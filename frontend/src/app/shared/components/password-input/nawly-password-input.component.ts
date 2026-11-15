import { Component, forwardRef, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { I18nService } from '../../../core/i18n/i18n.service';

let nextPasswordId = 0;

@Component({
  selector: 'nawly-password-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => NawlyPasswordInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="w-full">
      <div class="flex items-center justify-between gap-2 mb-2">
        <label [attr.for]="inputId" class="text-label-md font-label text-light-on-surface">
          {{ label() }}
          @if (required()) {
            <span class="text-error ms-1" aria-hidden="true">*</span>
          }
        </label>
        <ng-content select="[slot=headerAction]" />
      </div>

      <div class="relative">
        <span
          class="material-symbols-outlined absolute start-4 top-1/2 -translate-y-1/2 text-light-outline text-xl pointer-events-none"
          aria-hidden="true"
        >
          lock
        </span>

        <input
          [id]="inputId"
          [type]="revealed() ? 'text' : 'password'"
          [autocomplete]="autocomplete()"
          minlength="8"
          maxlength="128"
          [value]="value()"
          [placeholder]="placeholder()"
          [disabled]="disabled()"
          [attr.aria-invalid]="activeError() ? 'true' : 'false'"
          [attr.aria-describedby]="describedBy()"
          class="w-full h-14 ps-12 pe-12 bg-white border rounded-xl text-body-md text-light-on-surface placeholder:text-light-outline/50 focus:outline-none focus:ring-2 transition-all"
          [class.border-error]="!!activeError()"
          [class.bg-red-50/40]="!!activeError()"
          [class.focus:ring-error/30]="!!activeError()"
          [class.border-light-outline]="!activeError()"
          [class.focus:ring-light-primary]="!activeError()"
          [class.focus:border-light-primary]="!activeError()"
          [class.opacity-60]="disabled()"
          (input)="onInput($event)"
          (focus)="focused.set(true)"
          (blur)="onBlur()"
        />

        <button
          type="button"
          class="absolute end-2 top-1/2 -translate-y-1/2 min-w-[44px] min-h-[44px] flex items-center justify-center rounded-lg text-light-outline hover:text-light-on-surface transition-colors cursor-pointer"
          [attr.aria-label]="revealed() ? i18n.dict().passwordHide : i18n.dict().passwordShow"
          [attr.aria-pressed]="revealed()"
          (click)="revealed.set(!revealed())"
        >
          <span class="material-symbols-outlined text-xl" aria-hidden="true">
            {{ revealed() ? 'visibility_off' : 'visibility' }}
          </span>
        </button>
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
        <p [id]="hintId" class="mt-1.5 text-label-sm text-light-on-surface-variant font-body">
          {{ hint() || i18n.dict().passwordHint }}
        </p>
      }
    </div>
  `,
})
export class NawlyPasswordInputComponent implements ControlValueAccessor {
  protected readonly i18n = inject(I18nService);

  readonly label = input.required<string>();
  readonly placeholder = input<string>('••••••••');
  readonly autocomplete = input<'current-password' | 'new-password'>('current-password');
  readonly required = input<boolean>(true);
  readonly showHint = input<boolean>(true);
  readonly hint = input<string>('');
  readonly externalError = input<string | null>(null);
  readonly validateMinLength = input<boolean>(true);

  protected readonly inputId = `nawly-pwd-${++nextPasswordId}`;
  protected readonly hintId = `${this.inputId}-hint`;
  protected readonly errorId = `${this.inputId}-error`;

  protected readonly value = signal('');
  protected readonly revealed = signal(false);
  protected readonly focused = signal(false);
  protected readonly touched = signal(false);
  protected readonly disabled = signal(false);

  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};

  protected activeError(): string | null {
    const ext = this.externalError();
    if (ext) return ext;
    if (!this.touched()) return null;

    const val = this.value();
    if (!val) {
      return this.required() ? this.i18n.dict().passwordErrorRequired : null;
    }
    if (this.validateMinLength() && (val.length < 8 || val.length > 128)) {
      return this.i18n.dict().passwordErrorLength;
    }
    return null;
  }

  protected describedBy(): string {
    return this.activeError() ? this.errorId : this.hintId;
  }

  writeValue(val: string | null | undefined): void {
    this.value.set(val ?? '');
  }

  registerOnChange(fn: (val: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.value.set(val);
    this.onChange(val);
  }

  protected onBlur(): void {
    this.focused.set(false);
    this.touched.set(true);
    this.onTouched();
  }
}
