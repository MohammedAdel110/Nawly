import {
  Component,
  DestroyRef,
  ElementRef,
  effect,
  forwardRef,
  inject,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { I18nService } from '../../../core/i18n/i18n.service';
import { convertArabicDigitsToAscii } from '../../../core/utils/phone.util';

@Component({
  selector: 'nawly-otp-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => NawlyOtpInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="w-full">
      <div class="flex flex-wrap items-center justify-between gap-2 mb-3">
        <span class="text-label-md font-label text-light-on-surface">
          {{
            label() ||
              (i18n.isRtl() ? 'أدخل الأرقام الـ 6 (من اليسار لليمين)' : 'Enter the 6 digits (left to right)')
          }}
        </span>
        <span class="text-label-sm text-light-outline font-label">
          {{ i18n.isRtl() ? 'تأكد من لوحة الأرقام الإنجليزية' : 'Use Latin 0-9 numerals' }}
        </span>
      </div>

      <div
        dir="ltr"
        class="grid grid-cols-6 gap-2 sm:gap-3"
        role="group"
        [attr.aria-label]="label() || (i18n.isRtl() ? 'كود التحقق المكون من 6 أرقام' : '6-digit verification code')"
      >
        @for (digit of digits(); track $index) {
          <input
            #otpBox
            type="text"
            inputmode="numeric"
            maxlength="1"
            [attr.autocomplete]="$index === 0 ? 'one-time-code' : 'off'"
            [attr.aria-label]="(i18n.isRtl() ? 'الرقم ' : 'Digit ') + ($index + 1)"
            [value]="digit"
            [disabled]="disabled()"
            class="w-full h-14 sm:h-16 text-center font-code text-2xl font-bold rounded-xl border-2 bg-white text-light-on-surface focus:outline-none transition-all ltr-num"
            [class.border-error]="!!errorText()"
            [class.bg-red-50/40]="!!errorText()"
            [class.border-light-primary]="!!digit && !errorText()"
            [class.border-light-outline]="!digit && !errorText()"
            [class.focus:border-light-primary]="!errorText()"
            [class.focus:ring-4]="true"
            [class.focus:ring-light-primary/15]="!errorText()"
            [class.focus:ring-error/20]="!!errorText()"
            (input)="onBoxInput($index, $event)"
            (keydown)="onBoxKeydown($index, $event)"
            (paste)="onPaste($event)"
            (focus)="onFocusBox($event)"
          />
        }
      </div>

      @if (errorText(); as err) {
        <p
          role="alert"
          class="mt-3 flex items-center justify-center gap-1.5 text-body-sm font-semibold text-error"
        >
          <span class="material-symbols-outlined text-base shrink-0" aria-hidden="true">error</span>
          <span>{{ err }}</span>
        </p>
      }

      @if (showResend()) {
        <div
          class="mt-4 bg-light-surface-variant/30 px-4 py-3 rounded-xl flex flex-wrap items-center justify-between gap-2 text-body-sm text-light-on-surface-variant"
          aria-live="polite"
        >
          @if (secondsLeft() > 0) {
            <span class="inline-flex items-center gap-1.5">
              <span class="material-symbols-outlined text-base text-light-primary" aria-hidden="true">timer</span>
              <span>{{ i18n.dict().otpResendIn }}</span>
              <span class="font-code font-bold text-light-on-surface ltr-num">
                {{ formattedCountdown() }}
              </span>
            </span>
          } @else {
            <button
              type="button"
              class="text-label-md font-label text-light-primary font-bold hover:underline cursor-pointer disabled:opacity-50"
              [disabled]="resendBusy() || disabled()"
              (click)="triggerResend()"
            >
              {{
                resendBusy()
                  ? i18n.dict().loading
                  : i18n.isRtl()
                    ? 'لم يصلك الرمز؟ إعادة الإرسال برسالة SMS مجانية'
                    : 'Didn’t receive it? Resend free SMS'
              }}
            </button>
          }

          <span class="inline-flex items-center gap-1 text-label-sm text-light-outline font-label">
            <span class="material-symbols-outlined text-base" aria-hidden="true">schedule</span>
            <span>{{ i18n.isRtl() ? 'صالح لمدة 5 دقائق' : 'Valid for 5 minutes' }}</span>
          </span>
        </div>
      }
    </div>
  `,
})
export class NawlyOtpInputComponent implements ControlValueAccessor {
  protected readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  readonly label = input<string>('');
  readonly errorText = input<string | null>(null);
  readonly cooldownSeconds = input<number>(60);
  readonly cooldownTrigger = input<number>(0);
  readonly showResend = input<boolean>(true);
  readonly resendBusy = input<boolean>(false);

  readonly completed = output<string>();
  readonly resendRequested = output<void>();

  private readonly boxes = viewChildren<ElementRef<HTMLInputElement>>('otpBox');

  protected readonly digits = signal<string[]>(['', '', '', '', '', '']);
  protected readonly disabled = signal(false);
  protected readonly secondsLeft = signal(0);

  private timerId: ReturnType<typeof setInterval> | null = null;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    effect(() => {
      const initialSeconds = this.cooldownSeconds();
      this.cooldownTrigger();
      this.startCountdown(initialSeconds);
    });

    this.destroyRef.onDestroy(() => this.clearTimer());
  }

  writeValue(value: string | null | undefined): void {
    const clean = convertArabicDigitsToAscii(value ?? '')
      .replace(/\D/g, '')
      .slice(0, 6);
    const next = ['', '', '', '', '', ''];
    for (let i = 0; i < clean.length; i++) {
      next[i] = clean[i] ?? '';
    }
    this.digits.set(next);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  focusFirst(): void {
    const first = this.boxes()[0]?.nativeElement;
    first?.focus();
  }

  protected formattedCountdown(): string {
    const total = Math.max(0, this.secondsLeft());
    const mins = Math.floor(total / 60);
    const secs = total % 60;
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
  }

  protected onFocusBox(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  protected onBoxInput(index: number, event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const raw = convertArabicDigitsToAscii(inputEl.value).replace(/\D/g, '');

    if (raw.length > 1) {
      this.applyMultiDigitString(raw, index);
      return;
    }

    const digit = raw.slice(-1);
    const next = [...this.digits()];
    next[index] = digit;
    this.digits.set(next);
    inputEl.value = digit;

    this.emitCode(next);

    if (digit && index < 5) {
      this.focusBox(index + 1);
    }
  }

  protected onBoxKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace') {
      const current = this.digits();
      if (!current[index] && index > 0) {
        const next = [...current];
        next[index - 1] = '';
        this.digits.set(next);
        this.emitCode(next);
        this.focusBox(index - 1);
        event.preventDefault();
      } else if (current[index]) {
        const next = [...current];
        next[index] = '';
        this.digits.set(next);
        this.emitCode(next);
      }
      return;
    }

    if (event.key === 'ArrowLeft' && index > 0) {
      this.focusBox(index - 1);
      event.preventDefault();
    } else if (event.key === 'ArrowRight' && index < 5) {
      this.focusBox(index + 1);
      event.preventDefault();
    }
  }

  protected onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasted = convertArabicDigitsToAscii(event.clipboardData?.getData('text') ?? '').replace(
      /\D/g,
      '',
    );
    if (!pasted) return;
    this.applyMultiDigitString(pasted, 0);
  }

  protected triggerResend(): void {
    if (this.secondsLeft() > 0 || this.resendBusy()) return;
    this.resendRequested.emit();
  }

  private applyMultiDigitString(rawDigits: string, startIndex: number): void {
    const next = [...this.digits()];
    let cursor = startIndex;
    for (const ch of rawDigits) {
      if (cursor > 5) break;
      next[cursor] = ch;
      cursor++;
    }
    this.digits.set(next);
    this.emitCode(next);
    this.focusBox(Math.min(cursor, 5));
  }

  private emitCode(arr: string[]): void {
    const joined = arr.join('');
    this.onChange(joined);
    this.onTouched();
    if (joined.length === 6) {
      this.completed.emit(joined);
    }
  }

  private focusBox(index: number): void {
    const el = this.boxes()[index]?.nativeElement;
    el?.focus();
    el?.select();
  }

  private startCountdown(seconds: number): void {
    this.clearTimer();
    const safeSeconds = Math.max(0, Math.floor(seconds));
    this.secondsLeft.set(safeSeconds);
    if (safeSeconds <= 0) return;

    this.timerId = setInterval(() => {
      const next = this.secondsLeft() - 1;
      if (next <= 0) {
        this.secondsLeft.set(0);
        this.clearTimer();
      } else {
        this.secondsLeft.set(next);
      }
    }, 1000);
  }

  private clearTimer(): void {
    if (this.timerId !== null) {
      clearInterval(this.timerId);
      this.timerId = null;
    }
  }
}
