import { Component, forwardRef, inject, input, output, signal } from '@angular/core';
import {
  AbstractControl,
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';
import { I18nService } from '../../../core/i18n/i18n.service';
import {
  convertArabicDigitsToAscii,
  normalizeEgyptianMobile,
  stripCountryPrefixForInput,
} from '../../../core/utils/phone.util';

export const egyptianMobileValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = control.value as string | null | undefined;
  if (!value || !String(value).trim()) {
    return null;
  }
  return normalizeEgyptianMobile(String(value)) ? null : { egyptianMobile: true };
};

let nextPhoneId = 0;

@Component({
  selector: 'nawly-phone-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => NawlyPhoneInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="w-full">
      <label
        [attr.for]="inputId"
        class="block text-label-md font-label text-light-on-surface mb-2"
      >
        {{ label() || (i18n.isRtl() ? 'رقم الموبايل (11 رقم مصري)' : 'Mobile number (11 Egyptian digits)') }}
        @if (required()) {
          <span class="text-error ms-1" aria-hidden="true">*</span>
        }
      </label>

      <div class="relative flex items-center" dir="ltr">
        <div
          class="h-14 px-4 bg-light-surface-variant/60 border border-r-0 rounded-l-xl flex items-center gap-2 text-light-on-surface font-code font-bold select-none shrink-0 transition-colors"
          [class.border-error]="!!activeError()"
          [class.border-light-primary]="focused() && !activeError()"
          [class.border-light-outline]="!focused() && !activeError()"
          aria-hidden="true"
        >
          <span>🇪🇬</span>
          <span>+20</span>
        </div>

        <input
          [id]="inputId"
          type="tel"
          inputmode="numeric"
          autocomplete="tel-national"
          dir="ltr"
          class="w-full h-14 px-4 bg-white border rounded-r-xl font-code text-lg font-bold text-light-on-surface placeholder:text-light-outline/50 focus:outline-none focus:ring-2 transition-all ltr-num"
          [class.border-error]="!!activeError()"
          [class.bg-red-50/40]="!!activeError()"
          [class.focus:ring-error/30]="!!activeError()"
          [class.border-light-outline]="!activeError()"
          [class.focus:ring-light-primary]="!activeError()"
          [class.focus:border-light-primary]="!activeError()"
          [class.opacity-60]="disabled()"
          [value]="displayValue()"
          [placeholder]="placeholder()"
          [disabled]="disabled()"
          [attr.aria-invalid]="activeError() ? 'true' : 'false'"
          [attr.aria-describedby]="describedBy()"
          (input)="onRawInput($event)"
          (focus)="focused.set(true)"
          (blur)="handleBlur()"
        />
      </div>

      @if (activeError(); as err) {
        <p
          [id]="errorId"
          role="alert"
          class="mt-2 flex items-start gap-1.5 text-body-sm font-semibold text-error"
        >
          <span class="material-symbols-outlined text-base mt-0.5 shrink-0" aria-hidden="true">error</span>
          <span>{{ err }}</span>
        </p>
      } @else if (showHint()) {
        <p
          [id]="hintId"
          class="text-body-sm text-light-on-surface-variant mt-2 flex items-center gap-1.5"
        >
          <span class="material-symbols-outlined text-base text-light-primary shrink-0" aria-hidden="true">
            info
          </span>
          <span>
            {{
              hint() ||
                (i18n.isRtl()
                  ? 'يدعم شبكات فودافون، أورنج، اتصالات، ووي المصرية (11 رقم)'
                  : 'Supports Vodafone, Orange, Etisalat, and WE Egypt numbers (11 digits)')
            }}
          </span>
        </p>
      }
    </div>
  `,
})
export class NawlyPhoneInputComponent implements ControlValueAccessor {
  protected readonly i18n = inject(I18nService);

  readonly label = input<string>('');
  readonly hint = input<string>('');
  readonly showHint = input<boolean>(true);
  readonly placeholder = input<string>('010 1234 5678');
  readonly required = input<boolean>(true);
  readonly externalError = input<string | null>(null);

  readonly blurred = output<string | null>();

  protected readonly inputId = `nawly-phone-${++nextPhoneId}`;
  protected readonly hintId = `${this.inputId}-hint`;
  protected readonly errorId = `${this.inputId}-error`;

  protected readonly displayValue = signal('');
  protected readonly focused = signal(false);
  protected readonly touched = signal(false);
  protected readonly disabled = signal(false);

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  protected activeError(): string | null {
    const ext = this.externalError();
    if (ext) return ext;
    if (!this.touched()) return null;

    const raw = this.displayValue().trim();
    if (!raw) {
      return this.required() ? this.i18n.dict().phoneErrorRequired : null;
    }
    return normalizeEgyptianMobile(raw) ? null : this.i18n.dict().phoneErrorInvalid;
  }

  protected describedBy(): string {
    return this.activeError() ? this.errorId : this.hintId;
  }

  writeValue(value: string | null | undefined): void {
    const str = value ?? '';
    const stripped = stripCountryPrefixForInput(str);
    if (!stripped) {
      this.displayValue.set('');
      return;
    }
    if (str.startsWith('+20')) {
      this.displayValue.set(`0${stripped}`);
    } else {
      this.displayValue.set(stripped);
    }
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

  protected onRawInput(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const ascii = convertArabicDigitsToAscii(inputEl.value);
    const digitsOnly = ascii.replace(/[^\d+]/g, '');
    const stripped = digitsOnly.startsWith('+20')
      ? `0${digitsOnly.slice(3)}`
      : digitsOnly.replace(/\+/g, '');

    const clamped = stripped.slice(0, 11);
    inputEl.value = clamped;
    this.displayValue.set(clamped);

    const normalized = normalizeEgyptianMobile(clamped);
    this.onChange(normalized ?? clamped);
  }

  protected handleBlur(): void {
    this.focused.set(false);
    this.touched.set(true);
    this.onTouched();
    const normalized = normalizeEgyptianMobile(this.displayValue());
    this.blurred.emit(normalized);
  }
}
