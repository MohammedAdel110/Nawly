import {
  Component,
  DestroyRef,
  ElementRef,
  forwardRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { I18nService } from '../../../core/i18n/i18n.service';

export interface UploadedImageSelection {
  file: File;
  previewUrl: string;
  storageRef: string;
}

const MAX_BYTES = 5 * 1024 * 1024;
const ALLOWED_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

@Component({
  selector: 'nawly-image-uploader',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => NawlyImageUploaderComponent),
      multi: true,
    },
  ],
  template: `
    <div class="w-full">
      <div class="flex flex-wrap items-center justify-between gap-2 mb-2">
        <span class="text-label-md font-label text-light-on-surface">
          {{ label() }}
          @if (required()) {
            <span class="text-error ms-1" aria-hidden="true">*</span>
          } @else {
            <span class="text-label-sm font-label text-light-outline ms-1.5">
              ({{ i18n.dict().optionalBadge }})
            </span>
          }
        </span>

        @if (sensitiveBadge()) {
          <span
            class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-label-sm font-label bg-emerald-50 text-success border border-emerald-200"
          >
            <span class="material-symbols-outlined text-sm" aria-hidden="true">lock</span>
            <span>{{ sensitiveBadge() }}</span>
          </span>
        }
      </div>

      <input
        #fileInput
        type="file"
        accept="image/jpeg,image/png,image/webp"
        class="sr-only"
        [disabled]="disabled()"
        (change)="onFileSelected($event)"
      />

      <input
        #cameraInput
        type="file"
        accept="image/jpeg,image/png,image/webp"
        capture="environment"
        class="sr-only"
        [disabled]="disabled()"
        (change)="onFileSelected($event)"
      />

      @if (!selection()) {
        <div
          class="border-2 border-dashed rounded-2xl p-6 text-center transition-all"
          [class.border-error]="!!displayedError()"
          [class.bg-red-50/40]="!!displayedError()"
          [class.border-light-outline/60]="!displayedError()"
          [class.hover:border-light-primary]="!displayedError()"
          [class.bg-light-surface-variant/20]="!displayedError()"
          [class.hover:bg-light-primary-container/10]="!displayedError()"
        >
          <div
            class="w-14 h-14 mx-auto rounded-full bg-white shadow-xs flex items-center justify-center text-light-primary mb-3"
            aria-hidden="true"
          >
            <span class="material-symbols-outlined text-3xl">{{ iconName() }}</span>
          </div>

          <p class="text-label-lg font-label font-bold text-light-on-surface mb-1">
            {{ titleText() || label() }}
          </p>

          <p class="text-body-sm text-light-on-surface-variant mb-4">
            {{
              hint() ||
                (i18n.isRtl()
                  ? 'اضغط لفتح الكاميرا أو اختيار صورة واضحة من الجهاز'
                  : 'Tap to open camera or choose a clear image from device')
            }}
          </p>

          <div class="flex flex-wrap items-center justify-center gap-2.5 mb-3">
            <button
              type="button"
              class="min-h-[48px] px-4 py-2 rounded-xl border border-light-outline bg-white hover:bg-light-surface-variant/40 text-label-md font-label font-bold text-light-on-surface inline-flex items-center gap-2 cursor-pointer transition-colors"
              [disabled]="disabled() || validating()"
              (click)="fileInput.click()"
            >
              <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">
                upload_file
              </span>
              <span>{{ validating() ? i18n.dict().loading : i18n.dict().uploadChooseOrCamera }}</span>
            </button>

            <button
              type="button"
              class="min-h-[48px] px-3.5 py-2 rounded-xl text-label-md font-label font-bold text-light-primary hover:bg-light-primary-container/30 sm:hidden inline-flex items-center gap-1.5 cursor-pointer"
              [disabled]="disabled() || validating()"
              (click)="cameraInput.click()"
            >
              <span class="material-symbols-outlined text-lg" aria-hidden="true">photo_camera</span>
              <span>{{ i18n.dict().uploadTakePhoto }}</span>
            </button>
          </div>

          <span
            class="inline-block bg-white px-3 py-1 rounded-full text-label-sm font-code text-light-outline border border-light-outline-variant/60"
          >
            JPG, PNG • {{ i18n.isRtl() ? 'بحد أقصى 5 ميجابايت' : 'Max 5 MB' }}
          </span>
        </div>
      } @else {
        <div class="rounded-2xl border border-light-outline-variant bg-white p-4 shadow-xs">
          @if (!hidePreview()) {
            <div
              class="rounded-xl overflow-hidden bg-light-surface-variant/30 border border-light-outline-variant/50 mb-3 max-h-60 flex items-center justify-center"
            >
              <img
                [src]="selection()!.previewUrl"
                [alt]="label()"
                class="max-h-60 w-auto object-contain"
              />
            </div>
          }

          <div class="flex flex-wrap items-center justify-between gap-3">
            <div class="flex items-center gap-3 min-w-0">
              <div
                class="w-10 h-10 rounded-xl bg-emerald-50 border border-emerald-200 text-success flex items-center justify-center shrink-0"
                aria-hidden="true"
              >
                <span class="material-symbols-outlined text-xl">verified</span>
              </div>
              <div class="min-w-0">
                <p class="text-label-md font-label font-bold text-light-on-surface truncate">
                  {{ selection()!.file.name }}
                </p>
                <p class="text-label-sm font-code text-success ltr-num">
                  {{ formatBytes(selection()!.file.size) }}
                </p>
              </div>
            </div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                class="min-h-[44px] px-3 py-1.5 rounded-xl border border-light-outline-variant text-label-md font-label text-light-on-surface hover:bg-light-surface-variant/40 cursor-pointer"
                [disabled]="disabled()"
                (click)="fileInput.click()"
              >
                {{ i18n.dict().uploadRetake }}
              </button>

              <button
                type="button"
                class="min-h-[44px] px-3 py-1.5 rounded-xl text-label-md font-label text-error hover:bg-red-50 cursor-pointer"
                [disabled]="disabled()"
                (click)="clearSelection()"
              >
                {{ i18n.dict().uploadRemove }}
              </button>
            </div>
          </div>
        </div>
      }

      @if (privacyNote()) {
        <p class="mt-2 text-body-sm text-light-on-surface-variant leading-relaxed">
          {{ privacyNote() }}
        </p>
      }

      @if (displayedError(); as err) {
        <p role="alert" class="mt-2 flex items-start gap-1.5 text-body-sm font-semibold text-error">
          <span class="material-symbols-outlined text-base mt-0.5 shrink-0" aria-hidden="true">error</span>
          <span>{{ err }}</span>
        </p>
      }
    </div>
  `,
})
export class NawlyImageUploaderComponent implements ControlValueAccessor {
  protected readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  readonly label = input.required<string>();
  readonly titleText = input<string>('');
  readonly iconName = input<string>('badge');
  readonly slotName = input<string>('upload');
  readonly required = input<boolean>(false);
  readonly hint = input<string>('');
  readonly sensitiveBadge = input<string>('');
  readonly privacyNote = input<string>('');
  readonly hidePreview = input<boolean>(false);
  readonly externalError = input<string | null>(null);

  readonly fileSelected = output<UploadedImageSelection | null>();

  private readonly fileInputEl = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private readonly cameraInputEl = viewChild<ElementRef<HTMLInputElement>>('cameraInput');

  protected readonly selection = signal<UploadedImageSelection | null>(null);
  protected readonly localError = signal<string | null>(null);
  protected readonly validating = signal(false);
  protected readonly disabled = signal(false);

  private onChange: (storageRef: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    this.destroyRef.onDestroy(() => this.revokeCurrentPreview());
  }

  protected displayedError(): string | null {
    return this.localError() || this.externalError();
  }

  writeValue(val: string | null | undefined): void {
    if (!val) {
      this.revokeCurrentPreview();
      this.selection.set(null);
    }
  }

  registerOnChange(fn: (storageRef: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  clearSelection(): void {
    this.revokeCurrentPreview();
    this.selection.set(null);
    this.localError.set(null);
    if (this.fileInputEl()?.nativeElement) {
      this.fileInputEl()!.nativeElement.value = '';
    }
    if (this.cameraInputEl()?.nativeElement) {
      this.cameraInputEl()!.nativeElement.value = '';
    }
    this.onChange(null);
    this.onTouched();
    this.fileSelected.emit(null);
  }

  revokePreviewOnly(): void {
    this.revokeCurrentPreview();
    this.selection.set(null);
  }

  protected formatBytes(bytes: number): string {
    if (bytes < 1024 * 1024) {
      return `${Math.max(1, Math.round(bytes / 1024))} KB`;
    }
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  protected async onFileSelected(event: Event): Promise<void> {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (!file) return;

    this.localError.set(null);
    this.validating.set(true);
    this.onTouched();

    try {
      if (!ALLOWED_MIME_TYPES.has(file.type)) {
        this.localError.set(this.i18n.dict().uploadErrorType);
        inputEl.value = '';
        return;
      }

      if (file.size > MAX_BYTES) {
        this.localError.set(this.i18n.dict().uploadErrorSize);
        inputEl.value = '';
        return;
      }

      const validMagic = await this.hasValidImageMagicBytes(file);
      if (!validMagic) {
        this.localError.set(this.i18n.dict().uploadErrorCorrupt);
        inputEl.value = '';
        return;
      }

      this.revokeCurrentPreview();
      const previewUrl = URL.createObjectURL(file);
      const storageRef = this.buildCompactStorageRef(file.name, file.type);
      const result: UploadedImageSelection = { file, previewUrl, storageRef };
      this.selection.set(result);
      this.onChange(storageRef);
      this.fileSelected.emit(result);
    } catch {
      this.localError.set(this.i18n.dict().uploadErrorCorrupt);
    } finally {
      this.validating.set(false);
    }
  }

  private buildCompactStorageRef(filename: string, mimeType: string): string {
    const ext = mimeType === 'image/png' ? 'png' : mimeType === 'image/webp' ? 'webp' : 'jpg';
    const cleanStem =
      filename
        .replace(/\.[^/.]+$/, '')
        .toLowerCase()
        .replace(/[^a-z0-9_-]/g, '')
        .slice(0, 24) || 'doc';
    const token =
      globalThis.crypto?.randomUUID?.().slice(0, 12) ?? Math.random().toString(36).slice(2, 12);
    return `kyc://pending-upload/${this.slotName()}/${token}-${cleanStem}.${ext}`.slice(0, 180);
  }

  private revokeCurrentPreview(): void {
    const current = this.selection()?.previewUrl;
    if (current) {
      URL.revokeObjectURL(current);
    }
  }

  private async hasValidImageMagicBytes(file: File): Promise<boolean> {
    const buffer = await file.slice(0, 12).arrayBuffer();
    const bytes = new Uint8Array(buffer);
    if (bytes.length < 4) return false;

    const isJpeg = bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
    const isPng =
      bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47;
    const isWebp =
      bytes.length >= 12 &&
      bytes[0] === 0x52 &&
      bytes[1] === 0x49 &&
      bytes[2] === 0x46 &&
      bytes[3] === 0x46 &&
      bytes[8] === 0x57 &&
      bytes[9] === 0x45 &&
      bytes[10] === 0x42 &&
      bytes[11] === 0x50;

    return isJpeg || isPng || isWebp;
  }
}
