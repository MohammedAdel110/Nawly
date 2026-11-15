import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { AuthTokenDto } from '../../../../core/models/api.models';
import { AuthService } from '../../../../core/services/auth.service';
import { convertArabicDigitsToAscii } from '../../../../core/utils/phone.util';
import { ParsedApiError, parseHttpError } from '../../../../core/utils/problem-details.util';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';
import { NawlyOtpInputComponent } from '../../../../shared/components/otp-input/nawly-otp-input.component';
import { NawlyPasswordInputComponent } from '../../../../shared/components/password-input/nawly-password-input.component';

@Component({
  selector: 'nawly-staff-signin',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NawlyAppShellPublicComponent,
    NawlyPasswordInputComponent,
    NawlyOtpInputComponent,
  ],
  template: `
    <nawly-app-shell-public
      [hideAuthButton]="true"
      portalMode="staff"
      brandIcon="shield_person"
      [brandBadge]="i18n.isRtl() ? 'بوابة الشركاء' : 'Partner Portal'"
    >
      <div class="flex-1 flex items-center justify-center p-4 sm:p-6 bg-light-surface">
        <div class="max-w-xl w-full mx-auto py-4 sm:py-8">
          <div class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-xl shadow-slate-900/5 overflow-hidden">
            <!-- Operations Portal Header Strip -->
            <div class="bg-slate-900/5 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex flex-wrap items-center justify-between gap-2">
              <div class="flex items-center gap-2 text-label-md font-label text-light-on-surface-variant font-bold">
                <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">
                  admin_panel_settings
                </span>
                <span>{{ i18n.isRtl() ? 'بوابة العمليات الموثوقة' : 'Trusted Operations Portal' }}</span>
              </div>

              <span class="text-label-sm font-label font-bold text-slate-800 bg-slate-100 border border-slate-200 px-3 py-1 rounded-lg">
                @if (mfaActive()) {
                  {{ i18n.isRtl() ? 'التحقق الثنائي (MFA)' : 'Two-Factor Verification' }}
                } @else {
                  {{ i18n.isRtl() ? 'بيانات الحساب' : 'Account Credentials' }}
                }
              </span>
            </div>

            <div class="p-6 sm:p-10">
              @if (rateLimitSecondsLeft() > 0) {
                <div class="nawly-banner-warning mb-6" role="alert">
                  <span class="material-symbols-outlined text-warning shrink-0 mt-0.5" aria-hidden="true">timer</span>
                  <div>
                    <p class="font-bold">
                      {{ i18n.isRtl() ? 'تم تقييد المحاولات مؤقتاً للحماية' : 'Sign-in temporarily rate limited' }}
                    </p>
                    <p class="text-sm mt-0.5">
                      {{ i18n.isRtl() ? 'أعد المحاولة بعد:' : 'Try again in:' }}
                      <span class="font-code font-extrabold ltr-num">{{ rateLimitSecondsLeft() }}s</span>
                    </p>
                  </div>
                </div>
              } @else if (bannerMessage(); as msg) {
                <div class="nawly-banner-error mb-6" role="alert">
                  <span class="material-symbols-outlined text-error shrink-0 mt-0.5" aria-hidden="true">error</span>
                  <div class="min-w-0">
                    <p>{{ msg }}</p>
                    @if (apiError()?.traceId) {
                      <p class="mt-1 text-xs opacity-80 font-code ltr-num">
                        {{ i18n.dict().errTraceLabel }}: {{ apiError()!.traceId }}
                      </p>
                    }
                  </div>
                </div>
              }

              @if (!mfaActive()) {
                <!-- Step 1: Credentials -->
                <form [formGroup]="loginForm" (ngSubmit)="submitCredentials()" novalidate class="space-y-6">
                  <div>
                    <h1 class="text-headline-md font-headline text-light-on-surface mb-1">
                      {{ i18n.isRtl() ? 'تسجيل دخول فريق العمل' : 'Partner & Staff Sign in' }}
                    </h1>
                    <p class="text-body-sm text-light-on-surface-variant">
                      {{
                        i18n.isRtl()
                          ? 'للتجار المعتمدين، كباتن التوصيل، ومسؤولي تشغيل نوى بقرية نوى'
                          : 'For approved store owners, couriers, and NAWA village operations admins'
                      }}
                    </p>
                  </div>

                  <div>
                    <label
                      for="staff-identifier"
                      class="block text-label-md font-label text-light-on-surface mb-2"
                    >
                      {{ i18n.isRtl() ? 'اسم المستخدم، البريد، أو رقم الهاتف' : 'Mobile number or email address' }}
                      <span class="text-error ms-1" aria-hidden="true">*</span>
                    </label>
                    <div class="relative">
                      <span
                        class="material-symbols-outlined absolute start-4 top-1/2 -translate-y-1/2 text-light-outline text-xl pointer-events-none"
                        aria-hidden="true"
                      >
                        person
                      </span>
                      <input
                        id="staff-identifier"
                        type="text"
                        maxlength="150"
                        autocomplete="username"
                        formControlName="identifier"
                        [placeholder]="i18n.isRtl() ? '010XXXXXXXX أو store@nawa.eg' : '010XXXXXXXX or store@nawa.eg'"
                        class="w-full h-14 ps-12 pe-4 bg-white border rounded-xl text-body-md text-light-on-surface placeholder:text-light-outline/50 focus:outline-none focus:ring-2 transition-all ltr-num"
                        [class.border-error]="identifierInvalid()"
                        [class.bg-red-50/40]="identifierInvalid()"
                        [class.focus:ring-error/30]="identifierInvalid()"
                        [class.border-light-outline]="!identifierInvalid()"
                        [class.focus:ring-light-primary]="!identifierInvalid()"
                        [class.focus:border-light-primary]="!identifierInvalid()"
                      />
                    </div>
                    @if (identifierInvalid()) {
                      <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                        <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                        <span>
                          {{
                            i18n.isRtl()
                              ? 'يرجى إدخال رقم الموبايل أو البريد الإلكتروني.'
                              : 'Please enter your mobile number or email.'
                          }}
                        </span>
                      </p>
                    }
                  </div>

                  <nawly-password-input
                    formControlName="password"
                    autocomplete="current-password"
                    [validateMinLength]="false"
                    [showHint]="false"
                    [label]="i18n.isRtl() ? 'كلمة المرور' : 'Password'"
                    [placeholder]="'••••••••••••'"
                  >
                    <button
                      slot="headerAction"
                      type="button"
                      class="text-label-sm font-label text-light-primary font-semibold hover:underline cursor-pointer"
                      (click)="forgotModalOpen.set(true)"
                    >
                      {{ i18n.isRtl() ? 'نسيت كلمة المرور؟' : 'Forgot password?' }}
                    </button>
                  </nawly-password-input>

                  <button
                    type="submit"
                    class="w-full h-14 bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-lg rounded-xl shadow-md shadow-light-primary/20 flex items-center justify-center gap-3 transition-all cursor-pointer disabled:opacity-55 disabled:cursor-not-allowed"
                    [disabled]="busy() || rateLimitSecondsLeft() > 0"
                  >
                    @if (busy()) {
                      <span class="inline-block w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
                    }
                    <span>
                      {{ i18n.isRtl() ? 'تسجيل الدخول ومتابعة التحقق' : 'Sign in & Continue' }}
                    </span>
                    <span class="material-symbols-outlined text-xl" aria-hidden="true">
                      {{ i18n.isRtl() ? 'arrow_back' : 'arrow_forward' }}
                    </span>
                  </button>

                  <!-- Partner & Customer Cross Links -->
                  <div class="pt-4 border-t border-light-outline-variant/60 space-y-4">
                    <a
                      routerLink="/signin"
                      class="flex items-center justify-center gap-2 text-body-sm text-light-on-surface-variant hover:text-light-primary font-medium transition-colors"
                    >
                      <span class="material-symbols-outlined text-base text-light-primary" aria-hidden="true">
                        sms
                      </span>
                      <span>
                        {{ i18n.isRtl() ? 'عميل وتريد الطلب من متاجر نوى؟' : 'Customer wanting to order?' }}
                      </span>
                      <span class="font-bold underline">
                        {{ i18n.isRtl() ? 'دخول العملاء بالرسائل القصيرة SMS' : 'Customer SMS Sign in' }}
                      </span>
                    </a>

                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      <a
                        routerLink="/join/store"
                        class="flex items-center gap-3 p-3.5 rounded-xl bg-light-surface-variant/30 hover:bg-light-primary-container/25 border border-light-outline-variant/50 transition-colors"
                      >
                        <span class="material-symbols-outlined text-light-primary text-2xl" aria-hidden="true">
                          storefront
                        </span>
                        <div class="text-body-sm">
                          <span class="font-bold text-light-on-surface block">
                            {{ i18n.isRtl() ? 'انضم كمتجر' : 'Join as Store' }}
                          </span>
                          <span class="text-light-outline text-label-sm">
                            {{ i18n.isRtl() ? 'سجّل نشاطك التجاري بنوى' : 'List your Nawa business' }}
                          </span>
                        </div>
                      </a>

                      <a
                        routerLink="/join/courier"
                        class="flex items-center gap-3 p-3.5 rounded-xl bg-light-surface-variant/30 hover:bg-light-primary-container/25 border border-light-outline-variant/50 transition-colors"
                      >
                        <span class="material-symbols-outlined text-accent text-2xl" aria-hidden="true">
                          delivery_dining
                        </span>
                        <div class="text-body-sm">
                          <span class="font-bold text-light-on-surface block">
                            {{ i18n.isRtl() ? 'انضم ككابتن' : 'Join as Courier' }}
                          </span>
                          <span class="text-light-outline text-label-sm">
                            {{ i18n.isRtl() ? 'توصيل يومي بدخل مجزٍ' : 'Daily cash delivery earnings' }}
                          </span>
                        </div>
                      </a>
                    </div>
                  </div>
                </form>
              } @else {
                <!-- Step 2: Two-Factor Authentication (MFA) -->
                <form [formGroup]="mfaForm" (ngSubmit)="submitMfa()" novalidate class="space-y-6">
                  <div class="flex items-center justify-between gap-2">
                    <div>
                      <h1 class="text-headline-md font-headline text-light-on-surface">
                        {{ i18n.isRtl() ? 'التحقق الثنائي (MFA)' : 'Two-factor Verification (MFA)' }}
                      </h1>
                      @if (mfaContext()?.displayTarget) {
                        <p class="text-body-sm text-light-on-surface-variant mt-1">
                          {{ i18n.isRtl() ? 'الحساب:' : 'Account:' }}
                          <strong class="font-code text-light-primary ltr-num ms-1">
                            {{ mfaContext()!.displayTarget }}
                          </strong>
                        </p>
                      }
                    </div>
                    <button
                      type="button"
                      class="text-label-md font-label text-light-outline hover:text-light-on-surface flex items-center gap-1 cursor-pointer min-h-[44px] px-2"
                      [disabled]="busy()"
                      (click)="cancelMfa()"
                    >
                      <span class="material-symbols-outlined text-base" aria-hidden="true">
                        {{ i18n.isRtl() ? 'arrow_forward' : 'arrow_back' }}
                      </span>
                      <span>{{ i18n.isRtl() ? 'الرجوع' : 'Back' }}</span>
                    </button>
                  </div>

                  <!-- Amber Security Callout -->
                  <div class="bg-amber-50 border border-amber-200 rounded-xl p-4 flex items-start gap-3">
                    <span class="material-symbols-outlined text-warning text-2xl shrink-0" aria-hidden="true">
                      verified_user
                    </span>
                    <div class="text-body-sm text-amber-900 leading-relaxed">
                      <p class="font-bold mb-0.5">
                        {{ i18n.isRtl() ? 'أمان حسابك أولوية' : 'Account security required' }}
                      </p>
                      <p>
                        {{
                          i18n.isRtl()
                            ? 'حسابات التجار والإدارة محمية بنظام المصادقة الثنائية لضمان أمان متحصلات النقدية والطلبات.'
                            : 'Merchant and operations accounts require two-factor verification to protect cash settlements and orders.'
                        }}
                      </p>
                    </div>
                  </div>

                  @if (!useRecoveryCode()) {
                    <div>
                      <nawly-otp-input
                        formControlName="totpCode"
                        [label]="
                          i18n.isRtl()
                            ? 'أدخل رمز المصادقة ذو الـ 6 أرقام من تطبيق الموبايل'
                            : 'Enter 6-digit authenticator app code'
                        "
                        [showResend]="false"
                        (completed)="submitMfa()"
                      />
                    </div>
                  } @else {
                    <div>
                      <label
                        for="staff-recovery-code"
                        class="block text-label-md font-label text-light-on-surface mb-2"
                      >
                        {{ i18n.isRtl() ? 'أدخل رمز الاسترداد الاحتياطي (8 خانات)' : 'Enter backup recovery code (8 characters)' }}
                      </label>
                      <input
                        id="staff-recovery-code"
                        type="text"
                        maxlength="24"
                        autocomplete="one-time-code"
                        formControlName="recoveryCode"
                        placeholder="XXXX-XXXX"
                        class="w-full h-14 px-4 bg-white border border-light-outline rounded-xl font-code text-lg font-bold tracking-widest uppercase text-center text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary focus:border-light-primary transition-all ltr-num"
                      />
                    </div>
                  }

                  <div class="flex flex-wrap items-center justify-between gap-2 text-body-sm">
                    <button
                      type="button"
                      class="text-light-primary font-label font-semibold hover:underline flex items-center gap-1.5 cursor-pointer min-h-[40px]"
                      (click)="toggleRecoveryMode()"
                    >
                      <span class="material-symbols-outlined text-base" aria-hidden="true">key</span>
                      <span>
                        {{
                          useRecoveryCode()
                            ? (i18n.isRtl() ? 'العودة لرمز تطبيق المصادقة (6 أرقام)' : 'Use 6-digit authenticator code instead')
                            : (i18n.isRtl() ? 'استخدام رمز الاسترداد الاحتياطي' : 'Use backup recovery code')
                        }}
                      </span>
                    </button>
                    <span class="text-light-outline text-label-sm">
                      {{ i18n.isRtl() ? 'يتغير الرمز كل 30 ثانية' : 'Refreshes every 30s' }}
                    </span>
                  </div>

                  <button
                    type="submit"
                    class="w-full h-14 bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-lg rounded-xl shadow-md shadow-light-primary/20 flex items-center justify-center gap-2 transition-all cursor-pointer disabled:opacity-55 disabled:cursor-not-allowed"
                    [disabled]="busy() || !mfaCanSubmit() || rateLimitSecondsLeft() > 0"
                  >
                    @if (busy()) {
                      <span class="inline-block w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
                    } @else {
                      <span class="material-symbols-outlined text-xl" aria-hidden="true">lock_open</span>
                    }
                    <span>
                      {{ i18n.isRtl() ? 'تأكيد الرمز والدخول إلى لوحة التحكم' : 'Verify Code & Open Dashboard' }}
                    </span>
                  </button>
                </form>
              }
            </div>
          </div>
        </div>
      </div>

      <!-- G-02 Support / WhatsApp Password Recovery Modal -->
      @if (forgotModalOpen()) {
        <div
          class="fixed inset-0 z-50 bg-light-on-surface/60 backdrop-blur-xs flex items-center justify-center p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="forgot-pwd-title"
          (click)="forgotModalOpen.set(false)"
        >
          <div
            class="bg-white rounded-2xl border border-light-outline-variant shadow-2xl max-w-md w-full p-6 sm:p-7"
            (click)="$event.stopPropagation()"
          >
            <div class="flex items-start gap-3 pb-4 mb-4 border-b border-slate-100">
              <span class="material-symbols-outlined text-2xl text-light-primary shrink-0 mt-0.5" aria-hidden="true">support_agent</span>
              <div>
                <h2 id="forgot-pwd-title" class="text-headline-sm font-headline text-light-on-surface">
                  {{ i18n.isRtl() ? 'استعادة كلمة مرور الشركاء والإدارة' : 'Reset Partner or Staff Password' }}
                </h2>
                <p class="text-label-sm font-label text-light-outline mt-0.5">
                  {{ i18n.isRtl() ? 'فريق عمليات قرية نوى' : 'Nawa Village Operations Support' }}
                </p>
              </div>
            </div>

            <p class="text-body-md text-light-on-surface-variant leading-relaxed">
              {{
                i18n.isRtl()
                  ? 'خدمة إعادة تعيين كلمة المرور ذاتياً غير مفعّلة حالياً لحماية الحسابات التجارية. لاستعادة دخولك بأمان، يرجى التواصل مع إدارة نوى لإصدار كلمة مرور مؤقتة بعد التحقق.'
                  : 'Self-service password reset is handled directly by Nawa Village Operations to safeguard merchant cash records. Please contact our support team to verify your identity and reset your password.'
              }}
            </p>

            <div class="mt-4 p-3.5 rounded-xl bg-light-surface-variant/30 text-body-sm text-light-on-surface flex items-center gap-2">
              <span class="material-symbols-outlined text-light-primary text-lg shrink-0" aria-hidden="true">sms</span>
              <span>
                {{
                  i18n.isRtl()
                    ? 'تنويه للعملاء: حسابات العملاء تستخدم كود SMS مباشرة بدون كلمة مرور.'
                    : 'Customer tip: Customers sign in with SMS OTP directly — no password needed.'
                }}
              </span>
            </div>

            <div class="mt-6 flex flex-wrap items-center justify-end gap-2.5">
              <a
                routerLink="/help"
                class="min-h-[48px] px-5 py-2.5 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-sm inline-flex items-center gap-2 transition-colors"
                (click)="forgotModalOpen.set(false)"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">chat</span>
                <span>{{ i18n.isRtl() ? 'التواصل مع الدعم' : 'Contact Support' }}</span>
              </a>
              <button
                type="button"
                class="min-h-[48px] px-5 py-2.5 rounded-xl border border-light-outline-variant hover:bg-light-surface-variant/30 text-light-on-surface font-label font-bold text-sm cursor-pointer transition-colors"
                (click)="forgotModalOpen.set(false)"
              >
                {{ i18n.dict().close }}
              </button>
            </div>
          </div>
        </div>
      }
    </nawly-app-shell-public>
  `,
})
export class StaffSigninComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly i18n = inject(I18nService);

  protected readonly busy = signal(false);
  protected readonly apiError = signal<ParsedApiError | null>(null);
  protected readonly forgotModalOpen = signal(false);
  protected readonly useRecoveryCode = signal(false);
  protected readonly rateLimitSecondsLeft = signal(0);

  protected readonly mfaContext = this.auth.pendingMfa;
  protected readonly mfaActive = computed(() => Boolean(this.mfaContext()?.challengeToken));

  private returnUrl: string | null = null;
  private rateLimitTimer: ReturnType<typeof setInterval> | null = null;

  protected readonly loginForm = this.fb.nonNullable.group({
    identifier: ['', [Validators.required, Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
  });

  protected readonly mfaForm = this.fb.nonNullable.group({
    totpCode: ['', [Validators.pattern(/^\d{6}$/)]],
    recoveryCode: [''],
  });

  protected readonly identifierInvalid = computed(() => {
    const ctrl = this.loginForm.controls.identifier;
    return ctrl.invalid && (ctrl.touched || ctrl.dirty);
  });

  protected readonly bannerMessage = computed(() => {
    const err = this.apiError();
    if (!err) return null;

    if (err.isInvalidCredentials && !this.mfaActive()) {
      return this.i18n.isRtl()
        ? 'بيانات الدخول غير صحيحة. تأكد من رقم الموبايل أو البريد الإلكتروني وكلمة المرور.'
        : 'Incorrect mobile/email or password. Please check your credentials and try again.';
    }
    return err.message;
  });

  ngOnInit(): void {
    const rawReturn = this.route.snapshot.queryParamMap.get('returnUrl');
    this.returnUrl = this.sanitizeReturnUrl(rawReturn);
    this.destroyRef.onDestroy(() => this.clearRateLimitTimer());
  }

  protected mfaCanSubmit(): boolean {
    if (this.useRecoveryCode()) {
      return this.mfaForm.controls.recoveryCode.value.trim().length >= 4;
    }
    return /^\d{6}$/.test(this.mfaForm.controls.totpCode.value);
  }

  protected submitCredentials(): void {
    this.loginForm.markAllAsTouched();
    this.apiError.set(null);
    if (this.loginForm.invalid) return;

    const identifier = convertArabicDigitsToAscii(
      this.loginForm.controls.identifier.value.trim(),
    );

    this.busy.set(true);
    this.auth
      .loginWithPassword({
        identifier,
        password: this.loginForm.controls.password.value,
        deviceName: 'NAWA Partner Web',
      })
      .subscribe({
        next: (res) => {
          this.busy.set(false);
          if (res.requiresMfa && res.mfaChallengeToken) {
            this.auth.setPendingMfa({
              challengeToken: res.mfaChallengeToken,
              source: 'staff',
              displayTarget: identifier,
              returnUrl: this.returnUrl,
            });
            this.mfaForm.reset({ totpCode: '', recoveryCode: '' });
            this.useRecoveryCode.set(false);
            return;
          }

          if (res.tokens) {
            this.navigatePostLogin(res.tokens);
          }
        },
        error: (err: unknown) => {
          this.busy.set(false);
          this.handleApiError(err);
        },
      });
  }

  protected submitMfa(): void {
    const ctx = this.mfaContext();
    if (!ctx || !this.mfaCanSubmit() || this.busy()) return;

    this.apiError.set(null);
    const isRecovery = this.useRecoveryCode();
    const code = isRecovery
      ? this.mfaForm.controls.recoveryCode.value.trim()
      : this.mfaForm.controls.totpCode.value.trim();

    this.busy.set(true);
    this.auth
      .verifyMfa({
        challengeToken: ctx.challengeToken,
        code,
        isRecoveryCode: isRecovery,
        deviceName: 'NAWA Partner Web',
      })
      .subscribe({
        next: (res) => {
          this.busy.set(false);
          if (res.tokens) {
            const targetOverride = ctx.returnUrl || this.returnUrl;
            this.navigatePostLogin(res.tokens, targetOverride);
          }
        },
        error: (err: unknown) => {
          this.busy.set(false);
          this.handleApiError(err);
        },
      });
  }

  protected toggleRecoveryMode(): void {
    this.apiError.set(null);
    this.useRecoveryCode.update((v) => !v);
  }

  protected cancelMfa(): void {
    this.apiError.set(null);
    const ctx = this.mfaContext();
    this.auth.clearPendingMfa();
    if (ctx?.source === 'customer') {
      void this.router.navigateByUrl('/signin');
    }
  }

  private navigatePostLogin(tokens: AuthTokenDto, preferredReturn?: string | null): void {
    const returnTarget = preferredReturn ?? this.returnUrl;
    if (returnTarget) {
      void this.router.navigateByUrl(returnTarget);
      return;
    }
    const home = this.auth.resolveHomeRoute(tokens.roles);
    void this.router.navigateByUrl(home);
  }

  private handleApiError(err: unknown): void {
    const parsed = parseHttpError(err, this.i18n.locale());
    this.apiError.set(parsed);
    if (parsed.status === 429) {
      this.startRateLimitCountdown(parsed.retryAfterSeconds ?? 60);
    }
  }

  private startRateLimitCountdown(seconds: number): void {
    this.clearRateLimitTimer();
    this.rateLimitSecondsLeft.set(Math.max(1, seconds));
    this.rateLimitTimer = setInterval(() => {
      const next = this.rateLimitSecondsLeft() - 1;
      if (next <= 0) {
        this.rateLimitSecondsLeft.set(0);
        this.clearRateLimitTimer();
      } else {
        this.rateLimitSecondsLeft.set(next);
      }
    }, 1000);
  }

  private clearRateLimitTimer(): void {
    if (this.rateLimitTimer !== null) {
      clearInterval(this.rateLimitTimer);
      this.rateLimitTimer = null;
    }
  }

  private sanitizeReturnUrl(raw: string | null): string | null {
    if (!raw || !raw.startsWith('/') || raw.startsWith('//')) {
      return null;
    }
    return raw;
  }
}
