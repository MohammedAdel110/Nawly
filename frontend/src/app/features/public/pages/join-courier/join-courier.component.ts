import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { AreaDto, CourierProfileDto, VehicleType } from '../../../../core/models/api.models';
import { CourierApplicationService } from '../../../../core/services/courier-application.service';
import { DiscoveryService } from '../../../../core/services/discovery.service';
import { normalizeEgyptianMobile } from '../../../../core/utils/phone.util';
import { ParsedApiError, getFieldError, parseHttpError } from '../../../../core/utils/problem-details.util';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';
import { NawlyImageUploaderComponent } from '../../../../shared/components/image-uploader/nawly-image-uploader.component';
import { NawlyPasswordInputComponent } from '../../../../shared/components/password-input/nawly-password-input.component';
import {
  NawlyPhoneInputComponent,
  egyptianMobileValidator,
} from '../../../../shared/components/phone-input/nawly-phone-input.component';

interface VehicleOption {
  value: VehicleType;
  icon: string;
  arLabel: string;
  enLabel: string;
  arSub: string;
  enSub: string;
  needsPlate: boolean;
}

const VEHICLES: readonly VehicleOption[] = [
  {
    value: 'Motorcycle',
    icon: 'two_wheeler',
    arLabel: 'موتوسيكل',
    enLabel: 'Motorcycle',
    arSub: 'الأسرع والأكثر طلباً',
    enSub: 'Fastest & highest demand',
    needsPlate: true,
  },
  {
    value: 'Scooter',
    icon: 'electric_scooter',
    arLabel: 'سكوتر',
    enLabel: 'Scooter',
    arSub: 'خفيف وسهل التنقل',
    enSub: 'Agile inside alleys',
    needsPlate: true,
  },
  {
    value: 'Bicycle',
    icon: 'pedal_bike',
    arLabel: 'دراجة هوائية',
    enLabel: 'Bicycle',
    arSub: 'وسط قرية نوى',
    enSub: 'Central village zones',
    needsPlate: false,
  },
  {
    value: 'Car',
    icon: 'directions_car',
    arLabel: 'سيارة / فان',
    enLabel: 'Car / Van',
    arSub: 'للطلبات العائلية الكبيرة',
    enSub: 'Large family & bulk orders',
    needsPlate: true,
  },
  {
    value: 'OnFoot',
    icon: 'directions_walk',
    arLabel: 'سيراً على الأقدام',
    enLabel: 'On Foot',
    arSub: 'المسافات القريبة',
    enSub: 'Short walking distances',
    needsPlate: false,
  },
];

@Component({
  selector: 'nawly-join-courier',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NawlyAppShellPublicComponent,
    NawlyPhoneInputComponent,
    NawlyPasswordInputComponent,
    NawlyImageUploaderComponent,
  ],
  template: `
    <nawly-app-shell-public
      brandIcon="moped"
      [brandBadge]="i18n.isRtl() ? 'بوابة الكباتن' : 'Courier Portal'"
    >
      <div class="max-w-4xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-10">
        @if (submittedProfile(); as profile) {
          <!-- Confirmation State (Strictly ZERO ID image thumbnail rendered) -->
          <div class="max-w-2xl mx-auto bg-white rounded-3xl border border-light-outline-variant/80 shadow-xl p-8 sm:p-12 text-center">
            <div class="w-20 h-20 mx-auto rounded-full bg-emerald-50 border border-emerald-200 text-success flex items-center justify-center mb-6">
              <span class="material-symbols-outlined text-5xl" aria-hidden="true">sports_motorsports</span>
            </div>

            <span class="inline-block px-3.5 py-1 rounded-full text-label-sm font-label bg-amber-50 text-accent border border-amber-200 mb-3">
              {{ profile.status }}
            </span>

            <h1 class="text-headline-lg font-headline text-light-on-surface mb-2">
              {{ i18n.isRtl() ? 'أهلاً بك يا كابتن في نوى!' : 'Welcome to the NAWA Courier Team!' }}
            </h1>

            <p class="text-body-md text-light-on-surface-variant max-w-lg mx-auto leading-relaxed">
              {{
                i18n.isRtl()
                  ? 'تم استلام طلبك وملف بطاقة الرقم القومي بنجاح في نظام مشفر. سنتواصل معك خلال ساعات لإتمام التفعيل واستلام أولى طلبات قرية نوى.'
                  : 'Your courier application and encrypted National ID verification ref were submitted. Our Nawa operations team will contact you shortly for activation.'
              }}
            </p>

            <div class="mt-6 p-4 rounded-2xl bg-light-surface-variant/30 border border-light-outline-variant/60 text-start max-w-md mx-auto space-y-2">
              <div class="flex items-center justify-between gap-2 text-body-sm">
                <span class="text-light-on-surface-variant">{{ i18n.isRtl() ? 'الاسم:' : 'Name:' }}</span>
                <strong class="font-headline text-light-on-surface">{{ profile.fullName }}</strong>
              </div>
              <div class="flex items-center justify-between gap-2 text-body-sm">
                <span class="text-light-on-surface-variant">{{ i18n.isRtl() ? 'الوسيلة:' : 'Vehicle:' }}</span>
                <strong class="font-bold text-light-primary">{{ vehicleLabel(profile.vehicleType) }}</strong>
              </div>
              <div class="flex items-center justify-between gap-2 text-body-sm">
                <span class="text-light-on-surface-variant">{{ i18n.isRtl() ? 'رقم الطلب:' : 'Application ID:' }}</span>
                <strong class="font-code text-light-primary ltr-num">{{ profile.id }}</strong>
              </div>
            </div>

            <div class="mt-8 flex flex-wrap items-center justify-center gap-3">
              <a
                routerLink="/staff/signin"
                class="min-h-[48px] px-6 py-3 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">login</span>
                <span>{{ i18n.isRtl() ? 'دخول الكباتن' : 'Courier Sign in' }}</span>
              </a>
              <a
                routerLink="/"
                class="min-h-[48px] px-6 py-3 rounded-xl border border-light-outline-variant hover:bg-light-surface-variant/30 text-light-on-surface font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span>{{ i18n.isRtl() ? 'العودة للرئيسية' : 'Back Home' }}</span>
              </a>
            </div>
          </div>
        } @else {
          <!-- Teal Gradient Hero Banner -->
          <div class="bg-gradient-to-r from-light-primary to-teal-800 rounded-2xl p-6 sm:p-8 text-white shadow-lg mb-8 flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
            <div class="space-y-3 max-w-xl">
              <div class="inline-flex items-center gap-2 bg-white/15 backdrop-blur-xs px-3 py-1 rounded-full text-label-sm font-label">
                <span class="material-symbols-outlined text-sm text-emerald-300" aria-hidden="true">payments</span>
                <span>
                  {{
                    i18n.isRtl()
                      ? 'تحصيل يومي نقدًا لكامل أتعاب التوصيل'
                      : 'Daily cash settlement for every delivery'
                  }}
                </span>
              </div>
              <h1 class="text-headline-lg font-headline font-bold leading-snug">
                {{
                  i18n.isRtl()
                    ? 'انضم لأسطول كباتن نوى — كل طلباتك لحد عندك'
                    : 'Join the NAWA Courier Fleet in Nawa'
                }}
              </h1>
              <p class="text-body-md text-teal-100">
                {{
                  i18n.isRtl()
                    ? 'حقق دخلًا ثابتًا ومجزيًا من توصيل طلبات أهل قريتك بساعات عمل مرنة تناسب وقتك.'
                    : 'Earn steady local income delivering orders across Nawa village with flexible hours.'
                }}
              </p>
            </div>

            <div class="bg-white/10 backdrop-blur-md rounded-2xl p-5 border border-white/20 text-center shrink-0 w-full md:w-auto">
              <span class="text-label-sm text-teal-100 block mb-1">
                {{ i18n.isRtl() ? 'متوسط دخل الكابتن اليومي' : 'Avg. Daily Courier Earnings' }}
              </span>
              <span class="text-headline-md font-code font-bold text-white ltr-num block">
                250 - 400 {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}
              </span>
              <span class="text-label-sm text-emerald-200 font-bold">
                {{ i18n.isRtl() ? 'أتعاب توصيل يومية' : 'Daily EGP payout' }}
              </span>
            </div>
          </div>

          @if (apiError(); as err) {
            <div class="nawly-banner-error mb-6" role="alert">
              <span class="material-symbols-outlined text-error shrink-0 mt-0.5" aria-hidden="true">error</span>
              <div class="min-w-0">
                <p>{{ err.message }}</p>
                @if (err.traceId) {
                  <p class="mt-1 text-xs opacity-80 font-code ltr-num">
                    {{ i18n.dict().errTraceLabel }}: {{ err.traceId }}
                  </p>
                }
              </div>
            </div>
          }

          <form [formGroup]="form" (ngSubmit)="submitCourier()" novalidate class="space-y-8">
            <!-- Section 1: Personal Information -->
            <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
              <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-3">
                <span class="w-8 h-8 rounded-full bg-light-primary text-white font-code font-bold text-sm flex items-center justify-center ltr-num">
                  1
                </span>
                <h2 class="text-headline-sm font-headline text-light-on-surface">
                  {{ i18n.isRtl() ? 'البيانات الشخصية للكابتن' : 'Courier Personal Information' }}
                </h2>
              </div>

              <div class="p-6 sm:p-8 space-y-6">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label for="courier-fullname" class="block text-label-md font-label text-light-on-surface mb-2">
                      {{ i18n.isRtl() ? 'الاسم الرباعي بالكامل (كما بالبطاقة)' : 'Full legal name' }}
                      <span class="text-error ms-1" aria-hidden="true">*</span>
                    </label>
                    <input
                      id="courier-fullname"
                      type="text"
                      formControlName="fullName"
                      [placeholder]="i18n.isRtl() ? 'مثال: إسلام محمود عبد الفتاح سلامة' : 'e.g. Islam Mahmoud Salama'"
                      class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 transition-all"
                      [class.border-error]="fullNameInvalid()"
                      [class.border-light-outline]="!fullNameInvalid()"
                      [class.focus:ring-light-primary]="!fullNameInvalid()"
                    />
                    @if (fullNameInvalid()) {
                      <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                        <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                        <span>{{ i18n.isRtl() ? 'الاسم الكامل مطلوب.' : 'Full name is required.' }}</span>
                      </p>
                    }
                  </div>

                  <div>
                    <label for="courier-area" class="block text-label-md font-label text-light-on-surface mb-2">
                      {{ i18n.isRtl() ? 'منطقة السكن بقرية نوى (اختياري)' : 'Home area in Nawa (optional)' }}
                    </label>
                    <select
                      id="courier-area"
                      formControlName="areaId"
                      class="w-full h-14 px-4 bg-white border border-light-outline rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary transition-all"
                    >
                      <option value="">{{ i18n.isRtl() ? 'قرية نوى وما حولها' : 'Nawa & Surroundings' }}</option>
                      @for (area of areas(); track area.id) {
                        <option [value]="area.id">{{ i18n.pick(area.nameAr, area.name) }}</option>
                      }
                    </select>
                  </div>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <nawly-phone-input
                    formControlName="phoneNumber"
                    [label]="i18n.isRtl() ? 'رقم الموبايل الشخصي (يفضل واتساب)' : 'Mobile number (WhatsApp preferred)'"
                    [externalError]="fieldServerError('phoneNumber', 'PhoneNumber')"
                  />

                  <nawly-password-input
                    formControlName="password"
                    autocomplete="new-password"
                    [label]="i18n.isRtl() ? 'كلمة مرور حساب الكابتن' : 'Courier account password'"
                    [externalError]="fieldServerError('password', 'Password')"
                  />
                </div>
              </div>
            </section>

            <!-- Section 2: Vehicle Type Selection (5 Options) -->
            <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
              <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-3">
                <span class="w-8 h-8 rounded-full bg-light-primary text-white font-code font-bold text-sm flex items-center justify-center ltr-num">
                  2
                </span>
                <h2 class="text-headline-sm font-headline text-light-on-surface">
                  {{ i18n.isRtl() ? 'وسيلة التوصيل المتاحة لديك' : 'Select Your Delivery Vehicle' }}
                </h2>
              </div>

              <div class="p-6 sm:p-8 space-y-6">
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
                  @for (v of vehicles; track v.value) {
                    <button
                      type="button"
                      class="border-2 rounded-2xl p-4 text-center cursor-pointer transition-all flex flex-col items-center justify-center gap-1"
                      [class.border-light-primary]="form.controls.vehicleType.value === v.value"
                      [class.bg-light-primary-container/15]="form.controls.vehicleType.value === v.value"
                      [class.border-light-outline-variant]="form.controls.vehicleType.value !== v.value"
                      [class.hover:border-light-primary/50]="form.controls.vehicleType.value !== v.value"
                      (click)="form.controls.vehicleType.setValue(v.value)"
                    >
                      <span
                        class="material-symbols-outlined text-4xl mb-1"
                        [class.text-light-primary]="form.controls.vehicleType.value === v.value"
                        [class.text-light-outline]="form.controls.vehicleType.value !== v.value"
                        aria-hidden="true"
                      >
                        {{ v.icon }}
                      </span>
                      <span class="font-headline font-bold text-sm text-light-on-surface">
                        {{ i18n.isRtl() ? v.arLabel : v.enLabel }}
                      </span>
                      <span
                        class="text-xs font-label"
                        [class.text-light-primary]="form.controls.vehicleType.value === v.value"
                        [class.text-light-outline]="form.controls.vehicleType.value !== v.value"
                      >
                        {{ i18n.isRtl() ? v.arSub : v.enSub }}
                      </span>
                    </button>
                  }
                </div>

                @if (selectedVehicleNeedsPlate()) {
                  <div>
                    <label for="license-plate" class="block text-label-md font-label text-light-on-surface mb-2">
                      {{ i18n.isRtl() ? 'رقم لوحة المركبة (اختياري)' : 'Vehicle license plate (optional)' }}
                    </label>
                    <input
                      id="license-plate"
                      type="text"
                      dir="ltr"
                      formControlName="licensePlate"
                      placeholder="ق ب ج ٤٨٢١"
                      class="w-full max-w-xs h-12 px-4 bg-white border border-light-outline rounded-xl font-code text-body-md text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary ltr-num"
                    />
                  </div>
                }
              </div>
            </section>

            <!-- Section 3: Identity Documents & Encrypted National ID -->
            <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
              <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-3">
                <span class="w-8 h-8 rounded-full bg-light-primary text-white font-code font-bold text-sm flex items-center justify-center ltr-num">
                  3
                </span>
                <h2 class="text-headline-sm font-headline text-light-on-surface">
                  {{ i18n.isRtl() ? 'المستندات وإثبات الهوية' : 'Identity & Verification Documents' }}
                </h2>
              </div>

              <div class="p-6 sm:p-8 space-y-6">
                <!-- Privacy Assurance Banner -->
                <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-4 flex items-start gap-3">
                  <span class="material-symbols-outlined text-success text-2xl shrink-0" aria-hidden="true">lock</span>
                  <div class="text-body-sm text-emerald-950 leading-relaxed">
                    <p class="font-bold mb-0.5">
                      {{
                        i18n.isRtl()
                          ? 'حماية خصوصية بطاقة الرقم القومي:'
                          : 'National ID Confidentiality Guarantee:'
                      }}
                    </p>
                    <p>
                      {{
                        i18n.isRtl()
                          ? 'تُستخدم صورة البطاقة للتحقق الأمني فقط من قبل إدارة نوى، ولا يتم عرضها للعملاء أو المتاجر نهائياً.'
                          : 'Your National ID image is never shown publicly or stored in browser previews — visible strictly to authorized onboarding reviewers.'
                      }}
                    </p>
                  </div>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <nawly-image-uploader
                    #idUploader
                    formControlName="nationalIdImageRef"
                    slotName="national-id"
                    iconName="badge"
                    [required]="true"
                    [hidePreview]="true"
                    [label]="i18n.isRtl() ? 'صورة بطاقة الرقم القومي (الوجه الأمامي)' : 'National ID Front Photo'"
                    [titleText]="i18n.isRtl() ? 'صورة بطاقة الرقم القومي (الأمامية)' : 'National ID Front Side'"
                    [sensitiveBadge]="i18n.isRtl() ? 'سري ومشفر' : 'Encrypted KYC'"
                    [externalError]="idRequiredError()"
                  />

                  <nawly-image-uploader
                    formControlName="photoRef"
                    slotName="courier-photo"
                    iconName="account_circle"
                    [required]="false"
                    [label]="i18n.isRtl() ? 'صورة شخصية حديثة (اختيارية)' : 'Recent Personal Photo (Optional)'"
                    [titleText]="i18n.isRtl() ? 'صورة شخصية واضحة (اختياري)' : 'Clear Profile Selfie (Optional)'"
                  />
                </div>
              </div>
            </section>

            <!-- Bottom Resident Pledge & Submit Action -->
            <div class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
              <div class="space-y-1.5 flex-1">
                <label
                  class="flex items-start gap-3 cursor-pointer p-2 rounded-lg"
                  [class.bg-red-50/60]="attempted() && form.controls.pledgeAccepted.invalid"
                >
                  <input
                    type="checkbox"
                    formControlName="pledgeAccepted"
                    class="mt-1 w-5 h-5 rounded border-light-outline text-light-primary focus:ring-light-primary shrink-0"
                  />
                  <span class="text-body-sm text-light-on-surface leading-relaxed">
                    {{
                      i18n.isRtl()
                        ? 'أقر بأنني مقيم بقرية نوى أو ضواحيها، وبصحة بياناتي، والتزامي بأمانة توصيل طلبات أهالي القرية والتحصيل النقدي وتوريده في مواعيده.'
                        : 'I confirm I reside in Nawa Village or surrounding hamlets, my details are accurate, and I commit to honest doorstep delivery and COD settlement.'
                    }}
                  </span>
                </label>

                @if (attempted() && form.controls.pledgeAccepted.invalid) {
                  <p role="alert" class="ms-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                    <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                    <span>
                      {{ i18n.isRtl() ? 'يرجى تأكيد الإقرار قبل إرسال الطلب.' : 'Please accept the courier pledge first.' }}
                    </span>
                  </p>
                }
              </div>

              <button
                type="submit"
                class="w-full md:w-auto min-h-[56px] px-8 py-3 bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-lg rounded-xl shadow-md shadow-light-primary/20 flex items-center justify-center gap-2.5 shrink-0 cursor-pointer transition-all disabled:opacity-55 disabled:cursor-not-allowed"
                [disabled]="submitting()"
              >
                @if (submitting()) {
                  <span class="inline-block w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
                } @else {
                  <span class="material-symbols-outlined text-xl" aria-hidden="true">how_to_reg</span>
                }
                <span>{{ i18n.isRtl() ? 'تقديم طلب الانضمام ككابتن' : 'Submit Courier Application' }}</span>
              </button>
            </div>
          </form>
        }
      </div>
    </nawly-app-shell-public>
  `,
})
export class JoinCourierComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly courierApp = inject(CourierApplicationService);
  private readonly discovery = inject(DiscoveryService);
  protected readonly i18n = inject(I18nService);

  private readonly idUploader = viewChild<NawlyImageUploaderComponent>('idUploader');

  protected readonly vehicles = VEHICLES;
  protected readonly areas = signal<AreaDto[]>([]);
  protected readonly submitting = signal(false);
  protected readonly attempted = signal(false);
  protected readonly apiError = signal<ParsedApiError | null>(null);
  protected readonly submittedProfile = signal<CourierProfileDto | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    areaId: [''],
    phoneNumber: ['', [Validators.required, egyptianMobileValidator]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    vehicleType: ['Motorcycle' as VehicleType, [Validators.required]],
    licensePlate: [''],
    nationalIdImageRef: ['', [Validators.required, Validators.maxLength(300)]],
    photoRef: [null as string | null],
    pledgeAccepted: [false, [Validators.requiredTrue]],
  });

  ngOnInit(): void {
    this.discovery.getAreas().subscribe({
      next: (list) => this.areas.set(list.filter((a) => a.isActive)),
      error: () => {},
    });
  }

  protected readonly fullNameInvalid = computed(() => {
    const ctrl = this.form.controls.fullName;
    return ctrl.invalid && (ctrl.touched || this.attempted());
  });

  protected readonly idRequiredError = computed(() => {
    const serverErr = getFieldError(this.apiError(), 'nationalIdImageRef', 'NationalIdImageRef');
    if (serverErr) return serverErr;
    if (this.attempted() && this.form.controls.nationalIdImageRef.invalid) {
      return this.i18n.isRtl()
        ? 'يرجى رفع صورة واضحة لوجه البطاقة الشخصية.'
        : 'Please upload a clear photo of your National ID.';
    }
    return null;
  });

  protected selectedVehicleNeedsPlate(): boolean {
    const selected = this.form.controls.vehicleType.value;
    return VEHICLES.find((v) => v.value === selected)?.needsPlate ?? false;
  }

  protected fieldServerError(...keys: string[]): string | null {
    return getFieldError(this.apiError(), ...keys);
  }

  protected vehicleLabel(type: VehicleType): string {
    const found = VEHICLES.find((v) => v.value === type);
    if (!found) return type;
    return this.i18n.isRtl() ? found.arLabel : found.enLabel;
  }

  protected submitCourier(): void {
    this.attempted.set(true);
    this.form.markAllAsTouched();
    this.apiError.set(null);

    if (this.form.invalid) return;

    const raw = this.form.getRawValue();
    const canonicalPhone = normalizeEgyptianMobile(raw.phoneNumber);
    if (!canonicalPhone) return;

    this.submitting.set(true);
    this.courierApp
      .applyCourier({
        fullName: raw.fullName.trim(),
        phoneNumber: canonicalPhone,
        password: raw.password,
        vehicleType: raw.vehicleType,
        nationalIdImageRef: raw.nationalIdImageRef,
        photoRef: raw.photoRef || null,
      })
      .subscribe({
        next: (profile) => {
          this.submitting.set(false);
          this.idUploader()?.revokePreviewOnly();
          this.submittedProfile.set(profile);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.apiError.set(parseHttpError(err, this.i18n.locale()));
        },
      });
  }
}
