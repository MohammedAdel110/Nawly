import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CanComponentDeactivate } from '../../../../core/guards/auth.guard';
import { I18nService } from '../../../../core/i18n/i18n.service';
import {
  AreaDto,
  DeliveryMode,
  DeliveryZoneDto,
  StoreCategoryDto,
  StoreDetailsDto,
} from '../../../../core/models/api.models';
import { DiscoveryService } from '../../../../core/services/discovery.service';
import { normalizeEgyptianMobile } from '../../../../core/utils/phone.util';
import { ParsedApiError, getFieldError, parseHttpError } from '../../../../core/utils/problem-details.util';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';
import { NawlyPasswordInputComponent } from '../../../../shared/components/password-input/nawly-password-input.component';
import {
  NawlyPhoneInputComponent,
  egyptianMobileValidator,
} from '../../../../shared/components/phone-input/nawly-phone-input.component';

const NAWA_DEFAULT_LAT = 30.3128;
const NAWA_DEFAULT_LNG = 31.3325;

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const pwd = group.get('ownerPassword')?.value as string | undefined;
  const confirm = group.get('confirmPassword')?.value as string | undefined;
  if (!confirm) return null;
  return pwd === confirm ? null : { passwordMismatch: true };
}

@Component({
  selector: 'nawly-join-store',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NawlyAppShellPublicComponent,
    NawlyPhoneInputComponent,
    NawlyPasswordInputComponent,
  ],
  template: `
    <nawly-app-shell-public
      brandIcon="storefront"
      [brandBadge]="i18n.isRtl() ? 'بوابة التجار' : 'Store Portal'"
    >
      <div class="max-w-5xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-10">
        @if (submittedStore(); as created) {
          <!-- Confirmation State -->
          <div class="max-w-2xl mx-auto bg-white rounded-3xl border border-light-outline-variant/80 shadow-xl p-8 sm:p-12 text-center">
            <div class="w-20 h-20 mx-auto rounded-full bg-emerald-50 border border-emerald-200 text-success flex items-center justify-center mb-6">
              <span class="material-symbols-outlined text-5xl" aria-hidden="true">check_circle</span>
            </div>

            <span class="inline-block px-3.5 py-1 rounded-full text-label-sm font-label bg-amber-50 text-accent border border-amber-200 mb-3">
              {{ created.status }}
            </span>

            <h1 class="text-headline-lg font-headline text-light-on-surface mb-2">
              {{
                i18n.isRtl()
                  ? 'تم استلام طلب انضمام متجرك بنجاح!'
                  : 'Your store application has been received!'
              }}
            </h1>

            <p class="text-body-md text-light-on-surface-variant max-w-lg mx-auto leading-relaxed">
              {{
                i18n.isRtl()
                  ? 'سيقوم فريق تشغيل نوى بمراجعة بيانات المتجر والتواصل معك خلال 24 ساعة لتفعيل المتجر ودخول لوحة التحكم.'
                  : 'Our NAWA operations team will review your shop details and contact you within 24 hours to activate your store dashboard.'
              }}
            </p>

            <div class="mt-6 p-4 rounded-2xl bg-light-surface-variant/30 border border-light-outline-variant/60 text-start max-w-md mx-auto">
              <div class="flex items-center justify-between gap-2 text-body-sm">
                <span class="text-light-on-surface-variant">
                  {{ i18n.isRtl() ? 'اسم المتجر:' : 'Store name:' }}
                </span>
                <strong class="font-headline text-light-on-surface">
                  {{ i18n.pick(created.nameAr, created.name) }}
                </strong>
              </div>
              <div class="flex items-center justify-between gap-2 text-body-sm mt-2">
                <span class="text-light-on-surface-variant">
                  {{ i18n.isRtl() ? 'رقم مرجع الطلب:' : 'Application ID:' }}
                </span>
                <strong class="font-code text-light-primary ltr-num">{{ created.id }}</strong>
              </div>
            </div>

            <div class="mt-8 flex flex-wrap items-center justify-center gap-3">
              <a
                routerLink="/staff/signin"
                class="min-h-[48px] px-6 py-3 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">login</span>
                <span>{{ i18n.isRtl() ? 'تسجيل دخول الشركاء' : 'Partner Sign in' }}</span>
              </a>
              <a
                routerLink="/"
                class="min-h-[48px] px-6 py-3 rounded-xl border border-light-outline-variant hover:bg-light-surface-variant/30 text-light-on-surface font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span>{{ i18n.isRtl() ? 'العودة للرئيسية' : 'Back to Home' }}</span>
              </a>
            </div>
          </div>
        } @else {
          <!-- Page Intro -->
          <div class="mb-6">
            <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-label-sm font-label font-bold bg-light-primary-container/60 text-light-on-primary-container">
              {{ i18n.isRtl() ? 'انضمام المتاجر والمحلات — قرية نوى' : 'Store Partner Onboarding — Nawa' }}
            </span>
            <h1 class="mt-3 text-headline-lg sm:text-headline-xl font-headline text-light-on-surface">
              {{ i18n.isRtl() ? 'سجّل محلك أو مطعمك على نوى' : 'Register your store on NAWA' }}
            </h1>
            <p class="mt-1 text-body-md text-light-on-surface-variant max-w-2xl">
              {{
                i18n.isRtl()
                  ? 'اعرض منتجاتك لأهالي نوى والعزب المجاورة واستقبل الطلبات كاش بسهولة. المراجعة مجانية خلال 24 ساعة.'
                  : 'Reach customers across Nawa village and nearby hamlets with cash-on-delivery orders. Reviewed free within 24 hours.'
              }}
            </p>
          </div>

          <!-- 4-Step Progress Tracker Bar -->
          <div class="bg-white rounded-2xl border border-light-outline-variant/70 p-4 sm:p-6 shadow-xs mb-8">
            <div class="flex items-center justify-between gap-2 sm:gap-3">
              @for (item of stepLabels(); track item.step; let last = $last) {
                <button
                  type="button"
                  class="flex items-center gap-2.5 text-start shrink-0 transition-opacity"
                  [class.cursor-pointer]="item.step <= maxReachedStep()"
                  [class.cursor-default]="item.step > maxReachedStep()"
                  (click)="jumpToStep(item.step)"
                >
                  <div
                    class="w-10 h-10 rounded-full flex items-center justify-center font-code font-bold text-sm transition-all shrink-0"
                    [class.bg-success]="item.step < currentStep()"
                    [class.text-white]="item.step <= currentStep()"
                    [class.bg-light-primary]="item.step === currentStep()"
                    [class.shadow-md]="item.step === currentStep()"
                    [class.bg-light-surface-variant]="item.step > currentStep()"
                    [class.text-light-outline]="item.step > currentStep()"
                  >
                    @if (item.step < currentStep()) {
                      <span class="material-symbols-outlined text-lg" aria-hidden="true">check</span>
                    } @else {
                      <span class="ltr-num">{{ item.step }}</span>
                    }
                  </div>
                  <div class="hidden sm:block">
                    <span class="text-label-md font-label font-bold block"
                      [class.text-light-primary]="item.step === currentStep()"
                      [class.text-success]="item.step < currentStep()"
                      [class.text-light-outline]="item.step > currentStep()"
                    >
                      {{ item.label }}
                    </span>
                  </div>
                </button>

                @if (!last) {
                  <div
                    class="flex-1 h-1 rounded-full transition-colors min-w-[16px]"
                    [class.bg-success]="item.step < currentStep()"
                    [class.bg-light-outline-variant/60]="item.step >= currentStep()"
                  ></div>
                }
              }
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

          @if (loadingCatalog()) {
            <div class="bg-white rounded-2xl border border-light-outline-variant/80 p-8 space-y-4">
              <div class="nawly-skeleton h-8 w-1/3"></div>
              <div class="nawly-skeleton h-14 w-full"></div>
              <div class="nawly-skeleton h-14 w-full"></div>
            </div>
          } @else {
            <form [formGroup]="form" (ngSubmit)="submitApplication()" novalidate>
              <!-- STEP 1: Owner Basic Information -->
              @if (currentStep() === 1) {
                <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
                  <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-2.5">
                    <span class="material-symbols-outlined text-light-primary" aria-hidden="true">person</span>
                    <h2 class="text-headline-sm font-headline text-light-on-surface">
                      {{
                        i18n.isRtl()
                          ? 'الخطوة 1: البيانات الأساسية لمالك المتجر'
                          : 'Step 1: Store Owner Account Details'
                      }}
                    </h2>
                  </div>

                  <div class="p-6 sm:p-8 space-y-6">
                    <div>
                      <label for="owner-full-name" class="block text-label-md font-label text-light-on-surface mb-2">
                        {{ i18n.isRtl() ? 'الاسم الكامل لمالك المتجر' : 'Owner full name' }}
                        <span class="text-error ms-1" aria-hidden="true">*</span>
                      </label>
                      <input
                        id="owner-full-name"
                        type="text"
                        formControlName="ownerFullName"
                        [placeholder]="i18n.isRtl() ? 'مثال: الحاج محمد إبراهيم الشرقاوي' : 'e.g. Mohamed Ibrahim El-Sharkawy'"
                        class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface placeholder:text-light-outline/50 focus:outline-none focus:ring-2 transition-all"
                        [class.border-error]="fieldInvalid('ownerFullName')"
                        [class.border-light-outline]="!fieldInvalid('ownerFullName')"
                        [class.focus:ring-light-primary]="!fieldInvalid('ownerFullName')"
                      />
                      @if (fieldInvalid('ownerFullName')) {
                        <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                          <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                          <span>{{ i18n.isRtl() ? 'يرجى إدخال اسم المالك بالكامل.' : 'Owner full name is required.' }}</span>
                        </p>
                      }
                    </div>

                    <nawly-phone-input
                      formControlName="ownerPhone"
                      [label]="i18n.isRtl() ? 'رقم موبايل المالك (للتواصل والدخول)' : 'Owner mobile number'"
                      [externalError]="fieldServerError('ownerPhone', 'OwnerPhone')"
                    />

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <nawly-password-input
                        formControlName="ownerPassword"
                        autocomplete="new-password"
                        [label]="i18n.isRtl() ? 'كلمة مرور لوحة المتجر' : 'Store dashboard password'"
                        [externalError]="fieldServerError('ownerPassword', 'OwnerPassword')"
                      />

                      <nawly-password-input
                        formControlName="confirmPassword"
                        autocomplete="new-password"
                        [validateMinLength]="false"
                        [showHint]="false"
                        [label]="i18n.isRtl() ? 'تأكيد كلمة المرور' : 'Confirm password'"
                        [externalError]="confirmPasswordMismatchError()"
                      />
                    </div>
                  </div>
                </section>
              }

              <!-- STEP 2: Store Details & Nawa Map Pin -->
              @if (currentStep() === 2) {
                <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
                  <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-2.5">
                    <span class="material-symbols-outlined text-light-primary" aria-hidden="true">store</span>
                    <h2 class="text-headline-sm font-headline text-light-on-surface">
                      {{
                        i18n.isRtl()
                          ? 'الخطوة 2: تفاصيل المتجر والموقع الجغرافي بنوى'
                          : 'Step 2: Store Details & Nawa Map Location'
                      }}
                    </h2>
                  </div>

                  <div class="p-6 sm:p-8 space-y-6">
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div>
                        <label for="store-name-ar" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'اسم المتجر بالعربية' : 'Store name (Arabic)' }}
                          <span class="text-error ms-1" aria-hidden="true">*</span>
                        </label>
                        <input
                          id="store-name-ar"
                          type="text"
                          formControlName="storeNameAr"
                          [placeholder]="i18n.isRtl() ? 'مثال: مطعم ومشويات النور' : 'e.g. El-Nour Grill'"
                          class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 transition-all"
                          [class.border-error]="fieldInvalid('storeNameAr')"
                          [class.border-light-outline]="!fieldInvalid('storeNameAr')"
                          [class.focus:ring-light-primary]="!fieldInvalid('storeNameAr')"
                        />
                        @if (fieldInvalid('storeNameAr')) {
                          <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                            <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                            <span>{{ i18n.isRtl() ? 'اسم المتجر بالعربية مطلوب.' : 'Arabic store name is required.' }}</span>
                          </p>
                        }
                      </div>

                      <div>
                        <label for="store-name-en" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'اسم المتجر بالإنجليزية (اختياري)' : 'Store name (English - optional)' }}
                        </label>
                        <input
                          id="store-name-en"
                          type="text"
                          dir="ltr"
                          formControlName="storeName"
                          placeholder="e.g. El-Nour Grill"
                          class="w-full h-14 px-4 bg-white border border-light-outline rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary transition-all"
                        />
                      </div>
                    </div>

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <div>
                        <label for="store-category" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'تصنيف النشاط التجاري' : 'Business category' }}
                          <span class="text-error ms-1" aria-hidden="true">*</span>
                        </label>
                        <select
                          id="store-category"
                          formControlName="categoryId"
                          class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 transition-all"
                          [class.border-error]="fieldInvalid('categoryId')"
                          [class.border-light-outline]="!fieldInvalid('categoryId')"
                          [class.focus:ring-light-primary]="!fieldInvalid('categoryId')"
                        >
                          <option value="">{{ i18n.isRtl() ? '— اختر التصنيف —' : '— Select category —' }}</option>
                          @for (cat of categories(); track cat.id) {
                            <option [value]="cat.id">{{ i18n.pick(cat.nameAr, cat.name) }}</option>
                          }
                        </select>
                        @if (fieldInvalid('categoryId')) {
                          <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                            <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                            <span>{{ i18n.isRtl() ? 'يرجى اختيار تصنيف النشاط.' : 'Please pick a category.' }}</span>
                          </p>
                        }
                      </div>

                      <div>
                        <label for="store-area" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'المنطقة / الحارة في نوى' : 'Area / Neighborhood in Nawa' }}
                          <span class="text-error ms-1" aria-hidden="true">*</span>
                        </label>
                        <select
                          id="store-area"
                          formControlName="areaId"
                          class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 transition-all"
                          [class.border-error]="fieldInvalid('areaId')"
                          [class.border-light-outline]="!fieldInvalid('areaId')"
                          [class.focus:ring-light-primary]="!fieldInvalid('areaId')"
                        >
                          <option value="">{{ i18n.isRtl() ? '— اختر المنطقة —' : '— Select area —' }}</option>
                          @for (area of areas(); track area.id) {
                            <option [value]="area.id">{{ i18n.pick(area.nameAr, area.name) }}</option>
                          }
                        </select>
                        @if (fieldInvalid('areaId')) {
                          <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                            <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                            <span>{{ i18n.isRtl() ? 'يرجى اختيار المنطقة.' : 'Please select an area.' }}</span>
                          </p>
                        }
                      </div>
                    </div>

                    <div>
                      <label for="store-address" class="block text-label-md font-label text-light-on-surface mb-2">
                        {{ i18n.isRtl() ? 'العنوان التفصيلي وعلامة مميزة' : 'Detailed street address & landmark' }}
                        <span class="text-error ms-1" aria-hidden="true">*</span>
                      </label>
                      <input
                        id="store-address"
                        type="text"
                        formControlName="addressText"
                        [placeholder]="i18n.isRtl() ? 'مثال: شارع المحطة الرئيسي — بجوار صيدلية الشفاء' : 'e.g. Station St — next to El-Shifaa Pharmacy'"
                        class="w-full h-14 px-4 bg-white border rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 transition-all"
                        [class.border-error]="fieldInvalid('addressText')"
                        [class.border-light-outline]="!fieldInvalid('addressText')"
                        [class.focus:ring-light-primary]="!fieldInvalid('addressText')"
                      />
                      @if (fieldInvalid('addressText')) {
                        <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                          <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                          <span>{{ i18n.isRtl() ? 'العنوان التفصيلي مطلوب.' : 'Street address is required.' }}</span>
                        </p>
                      }
                    </div>

                    <div>
                      <div class="flex flex-wrap items-center justify-between gap-2 mb-1">
                        <span class="text-label-md font-label text-light-on-surface">
                          {{ i18n.isRtl() ? 'رقم هاتف المتجر لاستقبال الطلبات' : 'Store contact phone' }}
                        </span>
                        <button
                          type="button"
                          class="text-label-sm font-label text-light-primary font-bold hover:underline cursor-pointer"
                          (click)="copyOwnerPhoneToStore()"
                        >
                          {{ i18n.isRtl() ? 'استخدام رقم المالك' : 'Copy owner mobile' }}
                        </button>
                      </div>
                      <nawly-phone-input
                        formControlName="storePhone"
                        [label]="''"
                        [showHint]="false"
                        [externalError]="fieldServerError('storePhone', 'StorePhone')"
                      />
                    </div>

                    <!-- Interactive Stylized Nawa Map Pin Picker -->
                    <div>
                      <label class="block text-label-md font-label text-light-on-surface mb-2">
                        {{ i18n.isRtl() ? 'حدد موقع المتجر بدقة على خريطة نوى' : 'Pin exact store location on Nawa map' }}
                      </label>
                      <div
                        class="relative h-64 rounded-xl border-2 border-light-outline/60 overflow-hidden bg-slate-100 cursor-crosshair select-none"
                        role="button"
                        tabindex="0"
                        [attr.aria-label]="i18n.isRtl() ? 'انقر لتحديد موقع المتجر على خريطة نوى' : 'Click to place store pin on Nawa map'"
                        (click)="onMapPadClick($event)"
                      >
                        <div
                          class="absolute inset-0 opacity-45"
                          style="background-image: radial-gradient(#006874 1px, transparent 1px); background-size: 20px 20px;"
                        ></div>
                        <svg class="absolute inset-0 w-full h-full pointer-events-none" viewBox="0 0 800 300" aria-hidden="true">
                          <path d="M0,150 Q250,120 500,160 T800,140" fill="none" stroke="#FFFFFF" stroke-width="14" />
                          <path d="M400,0 L420,300" fill="none" stroke="#FFFFFF" stroke-width="10" />
                          <path d="M180,0 L210,300" fill="none" stroke="#E2E8F0" stroke-width="6" />
                          <path d="M620,40 L580,300" fill="none" stroke="#97F0FF" stroke-opacity="0.6" stroke-width="8" />
                        </svg>

                        <!-- Dynamic Pin Marker -->
                        <div
                          class="absolute -translate-x-1/2 -translate-y-full flex flex-col items-center pointer-events-none transition-all duration-150"
                          [style.left.%]="pinXPercent()"
                          [style.top.%]="pinYPercent()"
                        >
                          <div class="bg-light-primary text-white text-xs font-headline font-bold px-3 py-1 rounded-full shadow-lg mb-1 whitespace-nowrap">
                            {{ i18n.isRtl() ? 'موقع المحل: قرية نوى' : 'Store Pin: Nawa' }}
                          </div>
                          <span class="material-symbols-outlined text-5xl text-error drop-shadow-md">location_on</span>
                        </div>

                        <!-- Current GPS Coordinate Badge -->
                        <div class="absolute bottom-3 start-3 bg-white/95 backdrop-blur-xs px-3 py-1.5 rounded-lg border border-light-outline-variant text-label-sm font-code text-light-on-surface ltr-num shadow-xs">
                          {{ displayCoords() }}
                        </div>

                        <!-- Use Browser GPS Button -->
                        <button
                          type="button"
                          class="absolute bottom-3 end-3 bg-white hover:bg-light-surface-variant text-light-primary border border-light-outline-variant px-3.5 py-2 rounded-lg text-label-md font-label shadow-sm flex items-center gap-1.5 cursor-pointer transition-colors"
                          (click)="useBrowserLocation(); $event.stopPropagation()"
                        >
                          <span class="material-symbols-outlined text-lg" aria-hidden="true">my_location</span>
                          <span>{{ i18n.isRtl() ? 'تحديد موقعي الحالي' : 'Use Current GPS' }}</span>
                        </button>
                      </div>
                    </div>

                    <!-- Storefront Photo Notice -->
                    <div class="bg-sky-50 border border-sky-200 rounded-xl p-4 flex items-start gap-3">
                      <span class="material-symbols-outlined text-sky-700 text-2xl shrink-0" aria-hidden="true">
                        photo_camera
                      </span>
                      <div class="text-body-sm text-sky-900">
                        <p class="font-bold mb-0.5">
                          {{
                            i18n.isRtl()
                              ? 'صور المحل والشعار وقائمة الأصناف:'
                              : 'Store Logo, Photos & Menu Items:'
                          }}
                        </p>
                        <p>
                          {{
                            i18n.isRtl()
                              ? 'بعد مراجعة وقبول طلبك، يمكنك رفع الشعار وصور واجهة المتجر وقائمة الأصناف مباشرة من لوحة تحكم المتجر.'
                              : 'After your application is reviewed and approved, you can upload your logo, storefront photos, and menu items directly from your Store Dashboard.'
                          }}
                        </p>
                      </div>
                    </div>
                  </div>
                </section>
              }

              <!-- STEP 3: Delivery & Order Settings -->
              @if (currentStep() === 3) {
                <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
                  <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-2.5">
                    <span class="material-symbols-outlined text-light-primary" aria-hidden="true">local_shipping</span>
                    <h2 class="text-headline-sm font-headline text-light-on-surface">
                      {{
                        i18n.isRtl()
                          ? 'الخطوة 3: إعدادات التوصيل وتجهيز الطلبات'
                          : 'Step 3: Delivery & Preparation Settings'
                      }}
                    </h2>
                  </div>

                  <div class="p-6 sm:p-8 space-y-8">
                    <div>
                      <label class="block text-label-md font-label text-light-on-surface mb-3">
                        {{ i18n.isRtl() ? 'نظام التوصيل المفضل لمتجرك' : 'Preferred delivery mode' }}
                      </label>
                      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                        @for (mode of deliveryModes(); track mode.value) {
                          <button
                            type="button"
                            class="text-start border-2 rounded-2xl p-5 flex flex-col justify-between gap-2 transition-all cursor-pointer"
                            [class.border-light-primary]="form.controls.deliveryMode.value === mode.value"
                            [class.bg-light-primary-container/15]="form.controls.deliveryMode.value === mode.value"
                            [class.border-light-outline-variant]="form.controls.deliveryMode.value !== mode.value"
                            [class.bg-white]="form.controls.deliveryMode.value !== mode.value"
                            (click)="selectDeliveryMode(mode.value)"
                          >
                            <div class="flex items-start justify-between gap-2 w-full">
                              <span
                                class="material-symbols-outlined text-3xl mb-1"
                                [class.text-light-primary]="form.controls.deliveryMode.value === mode.value"
                                [class.text-light-outline]="form.controls.deliveryMode.value !== mode.value"
                                aria-hidden="true"
                              >
                                {{ mode.icon }}
                              </span>
                              @if (mode.badge) {
                                <span class="text-label-sm font-label font-bold text-light-primary bg-light-primary-container/60 px-2 py-0.5 rounded-full">
                                  {{ mode.badge }}
                                </span>
                              }
                            </div>
                            <h3 class="font-headline font-bold text-light-on-surface">{{ mode.title }}</h3>
                            <p class="text-body-sm text-light-on-surface-variant">{{ mode.desc }}</p>
                          </button>
                        }
                      </div>
                    </div>

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 pt-2">
                      <div>
                        <label for="min-order-amount" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'الحد الأدنى للطلب' : 'Minimum order amount' }}
                        </label>
                        <div class="relative flex items-center" dir="ltr">
                          <input
                            id="min-order-amount"
                            type="number"
                            min="0"
                            formControlName="minOrderAmount"
                            class="w-full h-14 px-4 bg-white border border-r-0 border-light-outline rounded-l-xl font-code text-lg font-bold text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary ltr-num"
                          />
                          <div class="h-14 px-4 bg-light-surface-variant/60 border border-light-outline rounded-r-xl flex items-center font-label font-bold text-light-on-surface whitespace-nowrap">
                            {{ i18n.isRtl() ? 'ج.م (EGP)' : 'EGP' }}
                          </div>
                        </div>
                      </div>

                      <div>
                        <label for="prep-minutes" class="block text-label-md font-label text-light-on-surface mb-2">
                          {{ i18n.isRtl() ? 'متوسط وقت تحضير الطلب (بالدقائق)' : 'Default prep time (minutes)' }}
                        </label>
                        <div class="relative flex items-center" dir="ltr">
                          <input
                            id="prep-minutes"
                            type="number"
                            min="5"
                            max="180"
                            formControlName="defaultPrepMinutes"
                            class="w-full h-14 px-4 bg-white border border-r-0 border-light-outline rounded-l-xl font-code text-lg font-bold text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary ltr-num"
                          />
                          <div class="h-14 px-4 bg-light-surface-variant/60 border border-light-outline rounded-r-xl flex items-center font-label font-bold text-light-on-surface whitespace-nowrap">
                            {{ i18n.isRtl() ? 'دقيقة' : 'min' }}
                          </div>
                        </div>
                      </div>
                    </div>

                    <!-- Delivery Zones Checkboxes -->
                    <div>
                      <label class="block text-label-md font-label text-light-on-surface mb-3">
                        {{ i18n.isRtl() ? 'نطاقات التوصيل داخل قرية نوى' : 'Supported Nawa delivery zones' }}
                        <span class="text-error ms-1" aria-hidden="true">*</span>
                      </label>
                      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                        @for (zone of zones(); track zone.id) {
                          <label
                            class="flex items-center justify-between gap-2 p-3.5 rounded-xl border-2 cursor-pointer transition-all"
                            [class.border-light-primary]="isZoneSelected(zone.id)"
                            [class.bg-light-primary-container/15]="isZoneSelected(zone.id)"
                            [class.border-light-outline-variant]="!isZoneSelected(zone.id)"
                            [class.bg-white]="!isZoneSelected(zone.id)"
                          >
                            <div class="flex items-center gap-2.5 min-w-0">
                              <input
                                type="checkbox"
                                [checked]="isZoneSelected(zone.id)"
                                (change)="toggleZone(zone.id)"
                                class="w-5 h-5 rounded border-light-outline text-light-primary focus:ring-light-primary shrink-0"
                              />
                              <span class="text-body-sm font-bold text-light-on-surface truncate">
                                {{ i18n.pick(zone.nameAr, zone.name) }}
                              </span>
                            </div>
                            <span class="text-label-sm font-code font-bold text-light-primary shrink-0 ltr-num">
                              {{ zone.baseFee }} {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}
                            </span>
                          </label>
                        }
                      </div>
                      @if (zoneSelectionError()) {
                        <p role="alert" class="mt-2 flex items-center gap-1.5 text-body-sm font-semibold text-error">
                          <span class="material-symbols-outlined text-base" aria-hidden="true">error</span>
                          <span>{{ i18n.isRtl() ? 'يرجى اختيار منطقة توصيل واحدة على الأقل.' : 'Select at least one delivery zone.' }}</span>
                        </p>
                      }
                    </div>
                  </div>
                </section>
              }

              <!-- STEP 4: Final Review & Submission -->
              @if (currentStep() === 4) {
                <section class="bg-white rounded-2xl border border-light-outline-variant/80 shadow-md overflow-hidden">
                  <div class="bg-light-surface-variant/40 px-6 sm:px-8 py-4 border-b border-light-outline-variant/60 flex items-center gap-2.5">
                    <span class="material-symbols-outlined text-light-primary" aria-hidden="true">fact_check</span>
                    <h2 class="text-headline-sm font-headline text-light-on-surface">
                      {{ i18n.isRtl() ? 'الخطوة 4: مراجعة بيانات طلب الانضمام' : 'Step 4: Review & Submit Application' }}
                    </h2>
                  </div>

                  <div class="p-6 sm:p-8 space-y-6">
                    <!-- Store Preview Card -->
                    <div class="rounded-2xl border border-light-outline-variant bg-light-surface-variant/20 p-6">
                      <div class="flex flex-wrap items-center justify-between gap-3 mb-4 pb-4 border-b border-light-outline-variant/60">
                        <div class="flex items-center gap-3">
                          <div class="w-12 h-12 rounded-xl bg-light-primary text-white flex items-center justify-center shrink-0">
                            <span class="material-symbols-outlined text-2xl" aria-hidden="true">restaurant</span>
                          </div>
                          <div>
                            <h3 class="text-headline-sm font-headline font-bold text-light-on-surface">
                              {{ form.controls.storeNameAr.value }}
                            </h3>
                            <p class="text-body-sm text-light-on-surface-variant">
                              {{ selectedCategoryLabel() }}
                              @if (selectedAreaLabel()) {
                                • {{ selectedAreaLabel() }}
                              }
                            </p>
                          </div>
                        </div>

                        <button
                          type="button"
                          class="text-light-primary font-label font-semibold text-body-sm hover:underline cursor-pointer"
                          (click)="jumpToStep(2)"
                        >
                          {{ i18n.isRtl() ? 'تعديل المتجر' : 'Edit store' }}
                        </button>
                      </div>

                      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-body-sm">
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'اسم المالك:' : 'Owner:' }}</span>
                          <span class="font-bold text-light-on-surface">{{ form.controls.ownerFullName.value }}</span>
                        </div>
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'هاتف المتجر:' : 'Store phone:' }}</span>
                          <span class="font-code font-bold text-light-on-surface ltr-num">
                            {{ form.controls.storePhone.value }}
                          </span>
                        </div>
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'العنوان:' : 'Address:' }}</span>
                          <span class="font-bold text-light-on-surface">{{ form.controls.addressText.value }}</span>
                        </div>
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'نظام التوصيل:' : 'Delivery mode:' }}</span>
                          <span class="font-bold text-light-primary">{{ selectedDeliveryModeLabel() }}</span>
                        </div>
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'الحد الأدنى للطلب:' : 'Minimum order:' }}</span>
                          <span class="font-code font-bold text-light-on-surface ltr-num">
                            {{ form.controls.minOrderAmount.value }} {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}
                          </span>
                        </div>
                        <div>
                          <span class="text-light-outline block">{{ i18n.isRtl() ? 'مدة التحضير:' : 'Prep time:' }}</span>
                          <span class="font-bold text-light-on-surface ltr-num">
                            {{ form.controls.defaultPrepMinutes.value }} {{ i18n.isRtl() ? 'دقيقة' : 'min' }}
                          </span>
                        </div>
                      </div>
                    </div>

                    <!-- Partner Agreement Checkbox -->
                    <label
                      class="flex items-start gap-3 p-4 rounded-xl bg-light-surface-variant/30 cursor-pointer border"
                      [class.border-error]="stepSubmitAttempted() && form.controls.acceptTerms.invalid"
                      [class.border-transparent]="!(stepSubmitAttempted() && form.controls.acceptTerms.invalid)"
                    >
                      <input
                        type="checkbox"
                        formControlName="acceptTerms"
                        class="mt-1 w-5 h-5 rounded border-light-outline text-light-primary focus:ring-light-primary shrink-0"
                      />
                      <span class="text-body-sm text-light-on-surface leading-relaxed">
                        {{
                          i18n.isRtl()
                            ? 'أؤكد صحة جميع البيانات المدخلة أعلاه وأوافق على شروط انضمام التجار لمنصة نوى، مع الالتزام بأسعار المحل الرسمية وجودة التجهيز.'
                            : 'I confirm all store details are accurate and accept NAWA Merchant Terms, including honest pricing and quality preparation.'
                        }}
                      </span>
                    </label>
                  </div>
                </section>
              }

              <!-- Navigation Controls -->
              <div class="mt-8 flex flex-wrap items-center justify-between gap-4">
                @if (currentStep() > 1) {
                  <button
                    type="button"
                    class="min-h-[48px] px-6 py-3 rounded-xl border border-light-outline-variant bg-white hover:bg-light-surface-variant/30 text-light-on-surface font-label font-bold inline-flex items-center gap-2 cursor-pointer transition-colors"
                    [disabled]="submitting()"
                    (click)="prevStep()"
                  >
                    <span class="material-symbols-outlined text-lg" aria-hidden="true">
                      {{ i18n.isRtl() ? 'arrow_forward' : 'arrow_back' }}
                    </span>
                    <span>{{ i18n.isRtl() ? 'الخطوة السابقة' : 'Previous' }}</span>
                  </button>
                } @else {
                  <span></span>
                }

                @if (currentStep() < 4) {
                  <button
                    type="button"
                    class="min-h-[52px] px-8 py-3 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-base shadow-md shadow-light-primary/20 inline-flex items-center gap-2 cursor-pointer transition-all"
                    (click)="nextStep()"
                  >
                    <span>{{ i18n.isRtl() ? 'الخطوة التالية' : 'Next Step' }}</span>
                    <span class="material-symbols-outlined text-lg" aria-hidden="true">
                      {{ i18n.isRtl() ? 'arrow_back' : 'arrow_forward' }}
                    </span>
                  </button>
                } @else {
                  <button
                    type="submit"
                    class="min-h-[56px] px-8 py-3 rounded-xl bg-success hover:bg-emerald-800 text-white font-label font-bold text-lg shadow-md flex items-center justify-center gap-2.5 cursor-pointer transition-all disabled:opacity-55 disabled:cursor-not-allowed"
                    [disabled]="submitting()"
                  >
                    @if (submitting()) {
                      <span class="inline-block w-5 h-5 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
                    } @else {
                      <span class="material-symbols-outlined text-xl" aria-hidden="true">send</span>
                    }
                    <span>
                      {{ i18n.isRtl() ? 'تأكيد وإرسال طلب الانضمام الآن' : 'Confirm & Submit Store Application' }}
                    </span>
                  </button>
                }
              </div>
            </form>
          }
        }
      </div>
    </nawly-app-shell-public>
  `,
})
export class JoinStoreComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  protected readonly discovery = inject(DiscoveryService);
  protected readonly i18n = inject(I18nService);

  protected readonly currentStep = signal(1);
  protected readonly maxReachedStep = signal(1);
  protected readonly loadingCatalog = signal(true);
  protected readonly submitting = signal(false);
  protected readonly stepSubmitAttempted = signal(false);

  protected readonly categories = signal<StoreCategoryDto[]>([]);
  protected readonly zones = signal<DeliveryZoneDto[]>([]);
  protected readonly areas = signal<AreaDto[]>([]);
  protected readonly submittedStore = signal<StoreDetailsDto | null>(null);
  protected readonly apiError = signal<ParsedApiError | null>(null);

  protected readonly form = this.fb.nonNullable.group(
    {
      ownerFullName: ['', [Validators.required, Validators.maxLength(150)]],
      ownerPhone: ['', [Validators.required, egyptianMobileValidator]],
      ownerPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
      confirmPassword: ['', [Validators.required]],

      storeNameAr: ['', [Validators.required, Validators.maxLength(150)]],
      storeName: ['', [Validators.maxLength(150)]],
      descriptionAr: ['', [Validators.maxLength(1000)]],
      categoryId: ['', [Validators.required]],
      areaId: ['', [Validators.required]],
      addressText: ['', [Validators.required, Validators.maxLength(400)]],
      storePhone: ['', [Validators.required, egyptianMobileValidator]],
      latitude: [NAWA_DEFAULT_LAT as number | null],
      longitude: [NAWA_DEFAULT_LNG as number | null],

      deliveryMode: ['PlatformCourier' as DeliveryMode, [Validators.required]],
      minOrderAmount: [30, [Validators.required, Validators.min(0)]],
      defaultPrepMinutes: [25, [Validators.required, Validators.min(5), Validators.max(180)]],
      deliveryZoneIds: [[] as string[]],

      acceptTerms: [false, [Validators.requiredTrue]],
    },
    { validators: passwordMatchValidator },
  );

  protected readonly stepLabels = computed(() =>
    this.i18n.isRtl()
      ? [
          { step: 1, label: 'بيانات المالك' },
          { step: 2, label: 'تفاصيل المتجر والموقع' },
          { step: 3, label: 'التوصيل والطلبات' },
          { step: 4, label: 'المراجعة والإرسال' },
        ]
      : [
          { step: 1, label: 'Owner Info' },
          { step: 2, label: 'Store & Map' },
          { step: 3, label: 'Delivery' },
          { step: 4, label: 'Review' },
        ],
  );

  protected readonly deliveryModes = computed(() =>
    this.i18n.isRtl()
      ? [
          {
            value: 'PlatformCourier' as DeliveryMode,
            icon: 'two_wheeler',
            badge: 'موصى به',
            title: 'كباتن شبكة نوى',
            desc: 'تتولى شبكة نوى التوصيل السريع وتحصيل الكاش وتوريده.',
          },
          {
            value: 'SelfDelivery' as DeliveryMode,
            icon: 'electric_moped',
            badge: '',
            title: 'دليفري خاص بالمتجر',
            desc: 'لديك طيار أو عامل توصيل خاص بك وتتولى التوصيل بنفسك.',
          },
          {
            value: 'PickupOnly' as DeliveryMode,
            icon: 'shopping_bag',
            badge: '',
            title: 'استلام من المحل فقط',
            desc: 'يطلب العميل مسبقاً ويأتي لاستلام طلبه جاهزاً.',
          },
        ]
      : [
          {
            value: 'PlatformCourier' as DeliveryMode,
            icon: 'two_wheeler',
            badge: 'Recommended',
            title: 'NAWA Couriers',
            desc: 'Fulfilled by verified NAWA village couriers with COD handling.',
          },
          {
            value: 'SelfDelivery' as DeliveryMode,
            icon: 'electric_moped',
            badge: '',
            title: 'Store Self-Delivery',
            desc: 'Delivered directly by your own shop delivery staff.',
          },
          {
            value: 'PickupOnly' as DeliveryMode,
            icon: 'shopping_bag',
            badge: '',
            title: 'Pickup Only',
            desc: 'Customers collect pre-prepared orders at your counter.',
          },
        ],
  );

  protected readonly selectedDeliveryModeLabel = computed(() => {
    const mode = this.form.controls.deliveryMode.value;
    return this.deliveryModes().find((m) => m.value === mode)?.title ?? mode;
  });

  protected readonly selectedCategoryLabel = computed(() => {
    const id = this.form.controls.categoryId.value;
    const found = this.categories().find((c) => c.id === id);
    return found ? this.i18n.pick(found.nameAr, found.name) : '';
  });

  protected readonly selectedAreaLabel = computed(() => {
    const id = this.form.controls.areaId.value;
    const found = this.areas().find((a) => a.id === id);
    return found ? this.i18n.pick(found.nameAr, found.name) : '';
  });

  ngOnInit(): void {
    forkJoin({
      categories: this.discovery.getCategories(),
      zones: this.discovery.getZones(),
      areas: this.discovery.getAreas(),
    }).subscribe({
      next: ({ categories, zones, areas }) => {
        this.categories.set(categories.filter((c) => c.isActive));
        const activeZones = zones.filter((z) => z.isActive);
        this.zones.set(activeZones);
        this.areas.set(areas.filter((a) => a.isActive));
        if (activeZones.length > 0 && this.form.controls.deliveryZoneIds.value.length === 0) {
          this.form.controls.deliveryZoneIds.setValue([activeZones[0].id]);
        }
        this.loadingCatalog.set(false);
      },
      error: (err: unknown) => {
        this.apiError.set(parseHttpError(err, this.i18n.locale()));
        this.loadingCatalog.set(false);
      },
    });
  }

  canDeactivate(): boolean {
    if (this.submittedStore() || !this.form.dirty) {
      return true;
    }
    const promptText = this.i18n.isRtl()
      ? 'لديك بيانات غير مرسلة في نموذج تسجيل المحل. هل تريد مغادرة الصفحة؟'
      : 'You have unsaved store registration details. Leave this page anyway?';
    return globalThis.confirm?.(promptText) ?? true;
  }

  protected confirmPasswordMismatchError(): string | null {
    const ctrl = this.form.controls.confirmPassword;
    if (ctrl.touched || this.stepSubmitAttempted()) {
      if (!ctrl.value) {
        return this.i18n.isRtl() ? 'يرجى تأكيد كلمة المرور.' : 'Please confirm your password.';
      }
      if (this.form.hasError('passwordMismatch')) {
        return this.i18n.isRtl() ? 'كلمتا المرور غير متطابقتين.' : 'Passwords do not match.';
      }
    }
    return null;
  }

  protected fieldInvalid(name: keyof typeof this.form.controls): boolean {
    const ctrl = this.form.controls[name];
    return ctrl.invalid && (ctrl.touched || this.stepSubmitAttempted());
  }

  protected fieldServerError(...keys: string[]): string | null {
    return getFieldError(this.apiError(), ...keys);
  }

  protected zoneSelectionError(): boolean {
    return this.stepSubmitAttempted() && this.form.controls.deliveryZoneIds.value.length === 0;
  }

  protected isZoneSelected(zoneId: string): boolean {
    return this.form.controls.deliveryZoneIds.value.includes(zoneId);
  }

  protected toggleZone(zoneId: string): void {
    const current = this.form.controls.deliveryZoneIds.value;
    const next = current.includes(zoneId)
      ? current.filter((id) => id !== zoneId)
      : [...current, zoneId];
    this.form.controls.deliveryZoneIds.setValue(next);
    this.form.markAsDirty();
  }

  protected selectDeliveryMode(mode: DeliveryMode): void {
    this.form.controls.deliveryMode.setValue(mode);
    this.form.markAsDirty();
  }

  protected copyOwnerPhoneToStore(): void {
    const ownerPhone = this.form.controls.ownerPhone.value;
    if (ownerPhone) {
      this.form.controls.storePhone.setValue(ownerPhone);
      this.form.markAsDirty();
    }
  }

  protected displayCoords(): string {
    const lat = this.form.controls.latitude.value ?? NAWA_DEFAULT_LAT;
    const lng = this.form.controls.longitude.value ?? NAWA_DEFAULT_LNG;
    return `${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E`;
  }

  protected pinXPercent(): number {
    const lng = this.form.controls.longitude.value ?? NAWA_DEFAULT_LNG;
    const offset = ((lng - NAWA_DEFAULT_LNG) / 0.015) * 100;
    return Math.min(85, Math.max(15, 50 + offset));
  }

  protected pinYPercent(): number {
    const lat = this.form.controls.latitude.value ?? NAWA_DEFAULT_LAT;
    const offset = ((NAWA_DEFAULT_LAT - lat) / 0.015) * 100;
    return Math.min(85, Math.max(20, 50 + offset));
  }

  protected useBrowserLocation(): void {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      this.form.patchValue({ latitude: NAWA_DEFAULT_LAT, longitude: NAWA_DEFAULT_LNG });
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.form.patchValue({
          latitude: Number(pos.coords.latitude.toFixed(6)),
          longitude: Number(pos.coords.longitude.toFixed(6)),
        });
      },
      () => {
        this.form.patchValue({ latitude: NAWA_DEFAULT_LAT, longitude: NAWA_DEFAULT_LNG });
      },
      { timeout: 6000 },
    );
  }

  protected onMapPadClick(event: MouseEvent): void {
    const box = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const ratioX = (event.clientX - box.left) / Math.max(1, box.width) - 0.5;
    const ratioY = (event.clientY - box.top) / Math.max(1, box.height) - 0.5;
    this.form.patchValue({
      latitude: Number((NAWA_DEFAULT_LAT - ratioY * 0.015).toFixed(6)),
      longitude: Number((NAWA_DEFAULT_LNG + ratioX * 0.015).toFixed(6)),
    });
    this.form.markAsDirty();
  }

  protected jumpToStep(step: number): void {
    if (step <= this.maxReachedStep()) {
      this.stepSubmitAttempted.set(false);
      this.currentStep.set(step);
    }
  }

  protected prevStep(): void {
    this.stepSubmitAttempted.set(false);
    this.currentStep.update((s) => Math.max(1, s - 1));
  }

  protected nextStep(): void {
    this.stepSubmitAttempted.set(true);
    if (!this.isStepValid(this.currentStep())) {
      this.markCurrentStepTouched();
      return;
    }
    this.stepSubmitAttempted.set(false);
    const next = Math.min(4, this.currentStep() + 1);
    this.currentStep.set(next);
    this.maxReachedStep.update((m) => Math.max(m, next));
  }

  protected submitApplication(): void {
    this.stepSubmitAttempted.set(true);
    this.form.markAllAsTouched();
    this.apiError.set(null);

    if (this.form.invalid || this.form.controls.deliveryZoneIds.value.length === 0) {
      return;
    }

    const raw = this.form.getRawValue();
    const ownerPhone = normalizeEgyptianMobile(raw.ownerPhone);
    const storePhone = normalizeEgyptianMobile(raw.storePhone);
    if (!ownerPhone || !storePhone) return;

    this.submitting.set(true);
    this.discovery
      .applyStore({
        ownerFullName: raw.ownerFullName.trim(),
        ownerPhone,
        ownerPassword: raw.ownerPassword,
        storeNameAr: raw.storeNameAr.trim(),
        storeName: raw.storeName.trim() || raw.storeNameAr.trim(),
        description: null,
        descriptionAr: raw.descriptionAr.trim() || null,
        categoryId: raw.categoryId,
        areaId: raw.areaId,
        addressText: raw.addressText.trim(),
        storePhone,
        latitude: raw.latitude,
        longitude: raw.longitude,
        deliveryMode: raw.deliveryMode,
        minOrderAmount: Number(raw.minOrderAmount),
        defaultPrepMinutes: Number(raw.defaultPrepMinutes),
        deliveryZoneIds: raw.deliveryZoneIds,
      })
      .subscribe({
        next: (store) => {
          this.submitting.set(false);
          this.submittedStore.set(store);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.apiError.set(parseHttpError(err, this.i18n.locale()));
        },
      });
  }

  private isStepValid(step: number): boolean {
    const c = this.form.controls;
    if (step === 1) {
      return (
        c.ownerFullName.valid &&
        c.ownerPhone.valid &&
        c.ownerPassword.valid &&
        c.confirmPassword.valid &&
        !this.form.hasError('passwordMismatch')
      );
    }
    if (step === 2) {
      return (
        c.storeNameAr.valid &&
        c.categoryId.valid &&
        c.areaId.valid &&
        c.addressText.valid &&
        c.storePhone.valid
      );
    }
    if (step === 3) {
      return (
        c.deliveryMode.valid &&
        c.minOrderAmount.valid &&
        c.defaultPrepMinutes.valid &&
        c.deliveryZoneIds.value.length > 0
      );
    }
    return this.form.valid;
  }

  private markCurrentStepTouched(): void {
    const c = this.form.controls;
    const step = this.currentStep();
    if (step === 1) {
      c.ownerFullName.markAsTouched();
      c.ownerPhone.markAsTouched();
      c.ownerPassword.markAsTouched();
      c.confirmPassword.markAsTouched();
    } else if (step === 2) {
      c.storeNameAr.markAsTouched();
      c.categoryId.markAsTouched();
      c.areaId.markAsTouched();
      c.addressText.markAsTouched();
      c.storePhone.markAsTouched();
    }
  }
}
