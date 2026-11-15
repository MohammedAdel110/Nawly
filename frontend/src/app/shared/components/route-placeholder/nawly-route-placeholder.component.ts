import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  forkJoin,
  map,
  merge,
  of,
  switchMap,
  tap,
} from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import {
  AreaDto,
  DeliveryZoneDto,
  StoreCategoryDto,
  StoreSummaryDto,
} from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { DiscoveryService } from '../../../core/services/discovery.service';
import { NawlyAppShellPublicComponent } from '../app-shell-public/nawly-app-shell-public.component';

export const SEARCH_DEBOUNCE_MS = 400;

interface StoreSearchCriteria {
  categoryId: string;
  areaId: string;
  search: string;
}

export interface RoutePlaceholderData {
  pageId: string;
  titleAr: string;
  titleEn: string;
  areaAr: string;
  areaEn: string;
  phase: string;
}

const DISCOVERY_PAGE_IDS = new Set(['C-01', 'C-02', 'C-03', 'C-04']);

@Component({
  selector: 'nawly-route-placeholder',
  standalone: true,
  imports: [RouterLink, NawlyAppShellPublicComponent],
  template: `
    <nawly-app-shell-public>
      @if (isDiscoveryView()) {
        <!-- Modern Editorial Nawa Village Marketplace Hero -->
        <section class="bg-white border-b border-slate-200">
          <div class="max-w-7xl mx-auto px-4 sm:px-6 py-10 sm:py-14 space-y-8">
            <div class="max-w-4xl space-y-5">
              <div class="inline-flex flex-wrap items-center gap-2.5 text-xs font-label font-bold text-slate-600">
                <span class="inline-flex items-center gap-2 text-emerald-700 bg-emerald-50 border border-emerald-200/80 px-3 py-1 rounded-full">
                  <span class="w-2 h-2 rounded-full bg-emerald-600 animate-pulse"></span>
                  <span>
                    {{
                      i18n.isRtl()
                        ? 'التوصيل متاح الآن في قرية نوى والعزب المجاورة'
                        : 'Live delivery across Nawa Village & hamlets'
                    }}
                  </span>
                </span>
                <span class="hidden sm:inline text-slate-300" aria-hidden="true">|</span>
                <span class="text-slate-600">
                  {{ i18n.isRtl() ? 'الدفع نقداً عند الاستلام' : 'Cash on delivery' }}
                </span>
              </div>

              <h1 class="text-3xl sm:text-5xl font-headline font-extrabold text-slate-900 tracking-tight leading-tight">
                {{ i18n.isRtl() ? 'نوى — كل طلباتك لحد عندك' : 'NAWA — All your orders right to your door' }}
                <span class="block text-teal-700 text-xl sm:text-2xl font-bold mt-2">
                  {{
                    i18n.isRtl()
                      ? 'اطلب من محلات قرية نوى بأسعار المحل الرسمية'
                      : 'Order from local Nawa Village shops at counter prices'
                  }}
                </span>
              </h1>

              <p class="text-base sm:text-lg text-slate-600 max-w-2xl leading-relaxed">
                {{
                  i18n.isRtl()
                    ? 'المطاعم، السوبر ماركت، الصيدليات، والمخابز المحلية في قرية نوى في مكان واحد — مع توصيل سريع لباب بيتك.'
                    : 'Restaurants, groceries, pharmacies, and bakeries across Nawa Village in one place — delivered fast to your doorstep.'
                }}
              </p>

              <!-- Unified Search & Area Command Bar -->
              <div class="bg-slate-50 border border-slate-300 rounded-2xl p-1.5 flex flex-col sm:flex-row items-stretch sm:items-center gap-1.5 focus-within:bg-white focus-within:border-slate-900 focus-within:shadow-md transition-all">
                <div class="relative sm:w-56 shrink-0">
                  <span
                    class="material-symbols-outlined absolute start-3.5 top-1/2 -translate-y-1/2 text-accent text-xl pointer-events-none"
                    aria-hidden="true"
                  >
                    location_on
                  </span>
                  <select
                    [value]="selectedAreaId()"
                    (change)="onAreaChange($event)"
                    [attr.aria-label]="i18n.isRtl() ? 'اختر منطقتك في نوى' : 'Choose area in Nawa'"
                    class="w-full h-11 ps-10 pe-3 bg-transparent border-0 rounded-xl text-sm font-label font-bold text-slate-800 focus:outline-none focus:ring-0 cursor-pointer"
                  >
                    <option value="">
                      {{ i18n.isRtl() ? 'كل مناطق قرية نوى' : 'All Nawa areas' }}
                    </option>
                    @for (area of areas(); track area.id) {
                      <option [value]="area.id">{{ i18n.pick(area.nameAr, area.name) }}</option>
                    }
                  </select>
                </div>

                <span class="hidden sm:block h-7 w-px bg-slate-200 shrink-0" aria-hidden="true"></span>

                <div class="relative flex-1">
                  <span
                    class="material-symbols-outlined absolute start-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-xl pointer-events-none"
                    aria-hidden="true"
                  >
                    search
                  </span>
                  <input
                    type="search"
                    [value]="searchQuery()"
                    (input)="onSearchChange($event)"
                    (keydown.enter)="onSearchEnter($event)"
                    [placeholder]="i18n.isRtl() ? 'ابحث عن محل، مطعم، صيدلية أو مخبز...' : 'Search Nawa shops, grills, pharmacies...'"
                    class="w-full h-11 ps-11 pe-10 bg-transparent border-0 rounded-xl text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-0"
                  />
                  @if (loadingStores()) {
                    <span
                      class="material-symbols-outlined animate-spin text-teal-700 text-lg absolute end-3.5 top-1/2 -translate-y-1/2 pointer-events-none"
                      aria-hidden="true"
                    >
                      progress_activity
                    </span>
                  }
                </div>

                <button
                  type="button"
                  [disabled]="loadingStores()"
                  class="h-11 px-6 rounded-xl bg-slate-900 hover:bg-slate-800 disabled:opacity-60 text-white font-label font-bold text-sm inline-flex items-center justify-center gap-2 shrink-0 cursor-pointer transition-colors"
                  (click)="triggerImmediateSearch()"
                >
                  <span>{{ i18n.isRtl() ? 'عرض المحلات' : 'Search Stores' }}</span>
                </button>
              </div>
              @if (searchQuery().trim().length === 1) {
                <p class="text-xs font-label text-slate-500 ps-2">
                  {{
                    i18n.isRtl()
                      ? 'اكتب حرفين على الأقل للبحث باسم المحل، أو امسح البحث لعرض جميع المحلات.'
                      : 'Type at least 2 characters to search, or clear the field to list all stores.'
                  }}
                </p>
              }
            </div>

            <!-- Architectural Trust Strip + Clean Customer Action -->
            <div class="pt-4 border-t border-slate-100 flex flex-col lg:flex-row lg:items-center justify-between gap-6">
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 flex-1 max-w-3xl">
                <div class="border-s-2 border-teal-600 ps-3.5">
                  <p class="font-headline font-extrabold text-base text-slate-900 ltr-num">
                    {{ i18n.isRtl() ? '20–35 دقيقة' : '20–35 mins' }}
                  </p>
                  <p class="text-xs text-slate-500 mt-0.5">
                    {{ i18n.isRtl() ? 'متوسط زمن التوصيل داخل القرية' : 'Average village delivery window' }}
                  </p>
                </div>

                <div class="border-s-2 border-teal-600 ps-3.5">
                  <p class="font-headline font-extrabold text-base text-slate-900 ltr-num">
                    {{ i18n.isRtl() ? '10–15 ج.م' : '10–15 EGP' }}
                  </p>
                  <p class="text-xs text-slate-500 mt-0.5">
                    {{ i18n.isRtl() ? 'أجرة توصيل ثابتة حسب المنطقة' : 'Flat zone-based delivery rate' }}
                  </p>
                </div>

                <div class="border-s-2 border-teal-600 ps-3.5">
                  <p class="font-headline font-extrabold text-base text-slate-900">
                    {{ i18n.isRtl() ? 'بسعر المحل' : 'Counter Prices' }}
                  </p>
                  <p class="text-xs text-slate-500 mt-0.5">
                    {{ i18n.isRtl() ? 'دفع كاش عند الاستلام بدون رسوم خفية' : 'Cash on delivery with zero hidden markup' }}
                  </p>
                </div>
              </div>

              @if (!auth.isAuthenticated()) {
                <div class="flex flex-wrap items-center gap-3 shrink-0">
                  <a
                    routerLink="/signin"
                    class="min-h-[44px] px-4 py-2 rounded-xl border border-slate-300 bg-white hover:bg-slate-50 text-slate-900 text-sm font-label font-bold inline-flex items-center gap-2 transition-colors"
                  >
                    <span class="material-symbols-outlined text-lg text-teal-700" aria-hidden="true">sms</span>
                    <span>{{ i18n.isRtl() ? 'دخول العملاء بالهاتف' : 'Customer SMS Sign in' }}</span>
                  </a>
                </div>
              }
            </div>
          </div>
        </section>

        <!-- Categories, Active Stores, Delivery Zones & Partner Callouts -->
        <div class="max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-10 space-y-12">
          <!-- Category Filter Bar -->
          <section aria-labelledby="categories-heading">
            <div class="flex flex-wrap items-center justify-between gap-3 mb-4">
              <h2 id="categories-heading" class="text-xl font-headline font-bold text-slate-900">
                {{ i18n.isRtl() ? 'تصفح حسب نوع المحل' : 'Browse by Store Category' }}
              </h2>
              @if (selectedCategoryId() || selectedAreaId() || searchQuery().trim()) {
                <button
                  type="button"
                  class="text-xs font-label text-teal-700 font-bold hover:underline cursor-pointer"
                  (click)="clearFilters()"
                >
                  {{ i18n.isRtl() ? 'إعادة ضبط الفلاتر' : 'Reset filters' }}
                </button>
              }
            </div>

            <div class="flex flex-wrap items-center gap-2.5">
              <button
                type="button"
                class="h-11 px-4 rounded-xl font-label font-bold text-sm inline-flex items-center gap-2 border transition-all cursor-pointer"
                [class.bg-slate-900]="!selectedCategoryId()"
                [class.text-white]="!selectedCategoryId()"
                [class.border-slate-900]="!selectedCategoryId()"
                [class.shadow-xs]="!selectedCategoryId()"
                [class.bg-white]="!!selectedCategoryId()"
                [class.text-slate-700]="!!selectedCategoryId()"
                [class.border-slate-200]="!!selectedCategoryId()"
                [class.hover:border-slate-400]="!!selectedCategoryId()"
                (click)="selectCategory('')"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">grid_view</span>
                <span>{{ i18n.isRtl() ? 'جميع المحلات' : 'All Categories' }}</span>
              </button>

              @for (cat of categories(); track cat.id) {
                <button
                  type="button"
                  class="h-11 px-4 rounded-xl font-label font-bold text-sm inline-flex items-center gap-2 border transition-all cursor-pointer"
                  [class.bg-slate-900]="selectedCategoryId() === cat.id"
                  [class.text-white]="selectedCategoryId() === cat.id"
                  [class.border-slate-900]="selectedCategoryId() === cat.id"
                  [class.shadow-xs]="selectedCategoryId() === cat.id"
                  [class.bg-white]="selectedCategoryId() !== cat.id"
                  [class.text-slate-700]="selectedCategoryId() !== cat.id"
                  [class.border-slate-200]="selectedCategoryId() !== cat.id"
                  [class.hover:border-slate-400]="selectedCategoryId() !== cat.id"
                  (click)="selectCategory(cat.id)"
                >
                  <span class="material-symbols-outlined text-lg" aria-hidden="true">
                    {{ categoryIcon(cat) }}
                  </span>
                  <span>{{ i18n.pick(cat.nameAr, cat.name) }}</span>
                </button>
              }
            </div>
          </section>

          <!-- Stores Grid -->
          <section aria-labelledby="stores-heading">
            <div class="flex flex-wrap items-center justify-between gap-2 mb-5">
              <h2 id="stores-heading" class="text-xl font-headline font-bold text-slate-900">
                {{ i18n.isRtl() ? 'المحلات المتاحة في نوى' : 'Participating Stores in Nawa' }}
              </h2>
              <span class="text-xs font-label font-bold text-slate-500 bg-slate-100 px-3 py-1 rounded-full ltr-num">
                {{ stores().length }} {{ i18n.isRtl() ? 'متجر' : 'stores' }}
              </span>
            </div>

            @if (loadingStores()) {
              <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                @for (n of [1, 2, 3]; track n) {
                  <div class="bg-white rounded-2xl border border-slate-200 overflow-hidden">
                    <div class="nawly-skeleton h-24 w-full rounded-none"></div>
                    <div class="p-5 space-y-3">
                      <div class="nawly-skeleton h-5 w-2/3"></div>
                      <div class="nawly-skeleton h-4 w-1/3"></div>
                      <div class="nawly-skeleton h-8 w-full mt-4"></div>
                    </div>
                  </div>
                }
              </div>
            } @else if (stores().length > 0) {
              <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                @for (store of stores(); track store.id) {
                  <a
                    [routerLink]="['/stores', store.slug]"
                    class="group bg-white rounded-2xl border border-slate-200 hover:border-slate-400 hover:shadow-lg transition-all flex flex-col overflow-hidden"
                  >
                    <!-- Dark Architectural Merchant Card Cover -->
                    <div class="h-24 px-5 pt-4 bg-gradient-to-l from-slate-900 to-slate-800 text-white relative flex items-start justify-between">
                      <span
                        class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-label font-bold"
                        [class.bg-emerald-500/20]="store.isOpenNow"
                        [class.text-emerald-200]="store.isOpenNow"
                        [class.border]="true"
                        [class.border-emerald-400/30]="store.isOpenNow"
                        [class.bg-white/10]="!store.isOpenNow"
                        [class.text-slate-300]="!store.isOpenNow"
                        [class.border-white/15]="!store.isOpenNow"
                      >
                        <span
                          class="w-1.5 h-1.5 rounded-full"
                          [class.bg-emerald-400]="store.isOpenNow"
                          [class.bg-slate-400]="!store.isOpenNow"
                        ></span>
                        <span>
                          {{
                            store.isOpenNow
                              ? (i18n.isRtl() ? 'مفتوح الآن' : 'Open')
                              : (i18n.isRtl() ? 'مغلق حالياً' : 'Closed')
                          }}
                        </span>
                      </span>

                      <span class="bg-white/15 backdrop-blur-xs text-white px-2.5 py-1 rounded-lg text-xs font-code font-bold ltr-num">
                        {{ store.estimatedMinutes }} {{ i18n.isRtl() ? 'دقيقة' : 'min' }}
                      </span>
                    </div>

                    <!-- Overlapping Monogram Avatar + Full Untruncated Store Info -->
                    <div class="px-5 pt-3 pb-5 flex-1 flex flex-col justify-between">
                      <div>
                        <div class="w-12 h-12 -mt-9 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center justify-center font-headline font-extrabold text-lg text-slate-900 overflow-hidden relative z-10">
                          {{ storeInitial(store) }}
                        </div>

                        <h3 class="font-headline font-bold text-lg text-slate-900 leading-snug mt-3 group-hover:text-teal-700 transition-colors">
                          {{ i18n.pick(store.nameAr, store.name) }}
                        </h3>

                        <span class="inline-block text-xs font-label font-bold text-teal-700 bg-teal-50 border border-teal-200/60 px-2.5 py-0.5 rounded-md mt-2">
                          {{ i18n.pick(store.categoryNameAr, store.categoryName) }}
                        </span>
                      </div>

                      <div class="pt-4 mt-5 border-t border-slate-100 flex items-center justify-between gap-2 text-xs text-slate-600">
                        <div class="flex items-center gap-3 ltr-num">
                          <span>
                            {{ i18n.isRtl() ? 'التوصيل:' : 'Delivery:' }}
                            <strong class="font-code text-slate-900">{{ store.deliveryFee }} {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}</strong>
                          </span>
                          <span class="text-slate-300" aria-hidden="true">•</span>
                          <span>
                            {{ i18n.isRtl() ? 'الحد الأدنى:' : 'Min:' }}
                            <strong class="font-code text-slate-900">{{ store.minOrderAmount }} {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}</strong>
                          </span>
                        </div>

                        <span class="font-label font-bold text-teal-700 inline-flex items-center gap-1 shrink-0">
                          <span>{{ i18n.isRtl() ? 'تصفح القائمة' : 'View Menu' }}</span>
                          <span class="material-symbols-outlined text-base" aria-hidden="true">
                            {{ i18n.isRtl() ? 'arrow_back' : 'arrow_forward' }}
                          </span>
                        </span>
                      </div>
                    </div>
                  </a>
                }
              </div>
            } @else {
              <!-- Clean Editorial Empty State -->
              <div class="bg-white rounded-2xl border border-slate-200 p-8 sm:p-10 text-center space-y-4">
                <div class="w-12 h-12 mx-auto rounded-xl bg-slate-100 text-slate-700 flex items-center justify-center">
                  <span class="material-symbols-outlined text-2xl" aria-hidden="true">store_mall_directory</span>
                </div>
                <h3 class="text-lg font-headline font-bold text-slate-900">
                  @if (selectedCategoryId() || selectedAreaId() || searchQuery().trim()) {
                    {{ i18n.isRtl() ? 'لا توجد متاجر مطابقة لهذا البحث حالياً' : 'No stores match your current filters' }}
                  } @else {
                    {{
                      i18n.isRtl()
                        ? 'نستقبل الآن طلبات انضمام المحلات والمطاعم في قرية نوى'
                        : 'Onboarding Nawa Village Stores & Restaurants Now'
                    }}
                  }
                </h3>
                <p class="text-sm text-slate-600 max-w-lg mx-auto">
                  @if (selectedCategoryId() || selectedAreaId() || searchQuery().trim()) {
                    {{
                      i18n.isRtl()
                        ? 'جرب اختيار تصنيف آخر أو عرض جميع مناطق قرية نوى.'
                        : 'Try clearing filters or switching to all village areas.'
                    }}
                  } @else {
                    {{
                      i18n.isRtl()
                        ? 'هل تملك مطعماً، سوبر ماركت، صيدلية أو مخبزاً في نوى؟ سجّل محلك مجاناً ليظهر هنا لآلاف السكان.'
                        : 'Own a shop, restaurant, bakery, or pharmacy in Nawa? Register free in 2 minutes to appear here.'
                    }}
                  }
                </p>
                <div class="flex flex-wrap items-center justify-center gap-3 pt-2">
                  @if (selectedCategoryId() || selectedAreaId() || searchQuery().trim()) {
                    <button
                      type="button"
                      class="h-11 px-6 rounded-xl bg-slate-900 hover:bg-slate-800 text-white font-label font-bold text-sm cursor-pointer"
                      (click)="clearFilters()"
                    >
                      {{ i18n.isRtl() ? 'عرض كل المحلات' : 'Show All Stores' }}
                    </button>
                  } @else {
                    <a
                      routerLink="/join/store"
                      class="h-11 px-6 rounded-xl bg-slate-900 hover:bg-slate-800 text-white font-label font-bold text-sm inline-flex items-center gap-2"
                    >
                      <span>{{ i18n.isRtl() ? 'سجّل محلك الآن' : 'Register Your Store' }}</span>
                    </a>
                    <a
                      routerLink="/signin"
                      class="h-11 px-6 rounded-xl border border-slate-300 bg-white hover:bg-slate-50 text-slate-900 font-label font-bold text-sm inline-flex items-center gap-2"
                    >
                      <span>{{ i18n.isRtl() ? 'تسجيل دخول العملاء' : 'Customer Sign in' }}</span>
                    </a>
                  }
                </div>
              </div>
            }
          </section>

          <!-- Transparent Nawa Village Delivery Zones -->
          @if (zones().length > 0) {
            <section aria-labelledby="zones-heading" class="bg-white rounded-2xl border border-slate-200 p-6 sm:p-8">
              <div class="flex flex-wrap items-center justify-between gap-2 mb-5 pb-4 border-b border-slate-100">
                <div class="flex items-center gap-2.5">
                  <span class="material-symbols-outlined text-teal-700 text-2xl" aria-hidden="true">map</span>
                  <h2 id="zones-heading" class="text-lg font-headline font-bold text-slate-900">
                    {{ i18n.isRtl() ? 'أسعار ومناطق التوصيل الرسمية داخل نوى' : 'Official Nawa Delivery Zones & Fees' }}
                  </h2>
                </div>
                <span class="text-xs font-label text-emerald-700 font-bold">
                  {{ i18n.isRtl() ? 'أسعار واضحة بدون أي عمولات خفية' : 'Transparent village delivery rates' }}
                </span>
              </div>

              <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                @for (zone of zones(); track zone.id) {
                  <div class="p-4 rounded-xl bg-slate-50 border border-slate-200/80 flex items-center justify-between gap-3">
                    <div>
                      <h3 class="font-headline font-bold text-slate-900 text-sm">
                        {{ i18n.pick(zone.nameAr, zone.name) }}
                      </h3>
                      <p class="text-xs text-slate-500 ltr-num mt-0.5">
                        {{
                          i18n.isRtl()
                            ? 'الوصول خلال ~' + zone.estimatedTravelMinutes + ' دقيقة'
                            : 'Est. ' + zone.estimatedTravelMinutes + ' mins'
                        }}
                      </p>
                    </div>
                    <div class="text-end shrink-0">
                      <span class="font-code font-extrabold text-base text-teal-700 ltr-num block">
                        {{ zone.baseFee }} {{ i18n.isRtl() ? 'ج.م' : 'EGP' }}
                      </span>
                      <span class="text-xs text-slate-500">
                        {{ i18n.isRtl() ? 'أجرة التوصيل' : 'Base fee' }}
                      </span>
                    </div>
                  </div>
                }
              </div>
            </section>
          }

          <!-- Refined Partner & Courier Editorial Callouts -->
          <section aria-labelledby="partners-heading" class="space-y-4">
            <h2 id="partners-heading" class="text-xl font-headline font-bold text-slate-900">
              {{ i18n.isRtl() ? 'انضم لشبكة شركاء نوى' : 'Partner with NAWA' }}
            </h2>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <a
                routerLink="/join/store"
                class="bg-white rounded-2xl border border-slate-200 hover:border-slate-400 hover:shadow-md p-6 flex items-center justify-between gap-4 transition-all"
              >
                <div class="space-y-1.5">
                  <span class="text-xs font-label font-bold text-accent">
                    {{ i18n.isRtl() ? 'لأصحاب المحلات والمطاعم' : 'For Shop & Restaurant Owners' }}
                  </span>
                  <h3 class="font-headline font-bold text-lg text-slate-900">
                    {{ i18n.isRtl() ? 'سجّل محلك أو مطعمك مجاناً' : 'Register Your Store Free' }}
                  </h3>
                  <p class="text-sm text-slate-600">
                    {{ i18n.isRtl() ? 'استقبل طلبات أهالي قرية نوى خلال 24 ساعة' : 'Start receiving Nawa village orders within 24 hours' }}
                  </p>
                </div>
                <span class="material-symbols-outlined text-2xl text-slate-400 shrink-0" aria-hidden="true">
                  {{ i18n.isRtl() ? 'arrow_back' : 'arrow_forward' }}
                </span>
              </a>

              <a
                routerLink="/join/courier"
                class="bg-white rounded-2xl border border-slate-200 hover:border-slate-400 hover:shadow-md p-6 flex items-center justify-between gap-4 transition-all"
              >
                <div class="space-y-1.5">
                  <span class="text-xs font-label font-bold text-teal-700">
                    {{ i18n.isRtl() ? 'لكباتن التوصيل داخل القرية' : 'For Local Village Couriers' }}
                  </span>
                  <h3 class="font-headline font-bold text-lg text-slate-900">
                    {{ i18n.isRtl() ? 'انضم ككابتن توصيل في نوى' : 'Join as a Local Courier' }}
                  </h3>
                  <p class="text-sm text-slate-600">
                    {{ i18n.isRtl() ? 'دخل يومي كاش ومواعيد عمل مرنة داخل قريتك' : 'Flexible hours and daily cash EGP settlements' }}
                  </p>
                </div>
                <span class="material-symbols-outlined text-2xl text-slate-400 shrink-0" aria-hidden="true">
                  {{ i18n.isRtl() ? 'arrow_back' : 'arrow_forward' }}
                </span>
              </a>
            </div>
          </section>
        </div>
      } @else {
        <!-- Clean Authenticated / Inner Section View (Zero Developer Codes or Spec Jargon) -->
        <div class="max-w-3xl w-full mx-auto px-4 sm:px-6 py-10 sm:py-16">
          <section class="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 sm:p-10 space-y-6">
            <div class="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-slate-100">
              <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-label font-bold bg-slate-100 text-slate-800 border border-slate-200">
                <span class="material-symbols-outlined text-base text-teal-700" aria-hidden="true">dashboard</span>
                <span>{{ i18n.pick(meta().areaAr, meta().areaEn) }}</span>
              </span>
            </div>

            <div class="space-y-2">
              <h1 class="text-headline-lg font-headline font-bold text-light-on-surface">
                {{ i18n.pick(meta().titleAr, meta().titleEn) }}
              </h1>
              <p class="text-body-md text-light-on-surface-variant leading-relaxed">
                {{
                  i18n.isRtl()
                    ? 'حسابك جاهز ومتصل بالنظام. يتم حالياً تجهيز العرض التفصيلي لهذه الصفحة وسيُتاح تلقائياً فور تفعيله.'
                    : 'Your account session is active and verified. The detailed view for this section is being prepared and will appear here automatically.'
                }}
              </p>
            </div>

            @if (auth.isAuthenticated()) {
              <div class="p-4 rounded-2xl bg-light-surface-variant/30 border border-light-outline-variant/60 flex flex-wrap items-center justify-between gap-3">
                <div class="text-body-sm">
                  <span class="text-light-on-surface-variant">
                    {{ i18n.isRtl() ? 'الحساب النشط:' : 'Active account:' }}
                  </span>
                  <strong class="font-headline text-light-on-surface ms-1">
                    {{ auth.session()?.fullName }}
                  </strong>
                </div>

                <button
                  type="button"
                  class="min-h-[44px] px-4 py-2 rounded-xl border border-light-outline-variant bg-white hover:bg-light-surface-variant/40 text-label-md font-label font-bold cursor-pointer"
                  (click)="signOut()"
                >
                  {{ i18n.dict().navSignOut }}
                </button>
              </div>
            }

            <div class="flex flex-wrap items-center gap-3 pt-2">
              <a
                routerLink="/"
                class="min-h-[48px] px-6 py-2.5 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">home</span>
                <span>{{ i18n.isRtl() ? 'العودة لسوق نوى الرئيسي' : 'Back to Nawa Marketplace' }}</span>
              </a>
              <a
                routerLink="/help"
                class="min-h-[48px] px-5 py-2.5 rounded-xl border border-light-outline-variant hover:bg-light-surface-variant/30 text-light-on-surface font-label font-bold inline-flex items-center gap-2 transition-colors"
              >
                <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">help</span>
                <span>{{ i18n.dict().navHelp }}</span>
              </a>
            </div>
          </section>
        </div>
      }
    </nawly-app-shell-public>
  `,
})
export class NawlyRoutePlaceholderComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly discovery = inject(DiscoveryService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly i18n = inject(I18nService);
  protected readonly auth = inject(AuthService);

  private readonly searchInput$ = new Subject<string>();
  private readonly immediateSearch$ = new Subject<void>();

  protected readonly meta = computed(() => this.route.snapshot.data as RoutePlaceholderData);
  protected readonly isDiscoveryView = computed(() =>
    DISCOVERY_PAGE_IDS.has(this.meta().pageId),
  );

  protected readonly categories = signal<StoreCategoryDto[]>([]);
  protected readonly zones = signal<DeliveryZoneDto[]>([]);
  protected readonly areas = signal<AreaDto[]>([]);
  protected readonly stores = signal<StoreSummaryDto[]>([]);
  protected readonly loadingStores = signal(true);

  protected readonly selectedCategoryId = signal('');
  protected readonly selectedAreaId = signal('');
  protected readonly searchQuery = signal('');

  ngOnInit(): void {
    if (!this.isDiscoveryView()) {
      this.loadingStores.set(false);
      return;
    }

    const debouncedTyping$ = this.searchInput$.pipe(
      map((query) => query?.trim() ?? ''),
      debounceTime(SEARCH_DEBOUNCE_MS),
      filter((query) => query.length === 0 || query.length >= 2),
      distinctUntilChanged(),
      map((search): StoreSearchCriteria => ({
        categoryId: this.selectedCategoryId(),
        areaId: this.selectedAreaId(),
        search,
      })),
    );

    const immediateTrigger$ = this.immediateSearch$.pipe(
      map((): StoreSearchCriteria => ({
        categoryId: this.selectedCategoryId(),
        areaId: this.selectedAreaId(),
        search: this.normalizedSearchQuery(),
      })),
    );

    merge(debouncedTyping$, immediateTrigger$)
      .pipe(
        distinctUntilChanged(
          (prev: StoreSearchCriteria, curr: StoreSearchCriteria) =>
            prev.categoryId === curr.categoryId &&
            prev.areaId === curr.areaId &&
            prev.search === curr.search,
        ),
        tap(() => this.loadingStores.set(true)),
        switchMap((criteria) =>
          this.discovery
            .getStores({
              categoryId: criteria.categoryId || null,
              areaId: criteria.areaId || null,
              search: criteria.search || null,
            })
            .pipe(catchError(() => of(this.stores()))),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((list) => {
        this.stores.set(list);
        this.loadingStores.set(false);
      });

    forkJoin({
      categories: this.discovery.getCategories(),
      zones: this.discovery.getZones(),
      areas: this.discovery.getAreas(),
      stores: this.discovery.getStores(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ categories, zones, areas, stores }) => {
          this.categories.set(categories.filter((c) => c.isActive));
          this.zones.set(zones.filter((z) => z.isActive));
          this.areas.set(areas.filter((a) => a.isActive));
          this.stores.set(stores);
          this.loadingStores.set(false);
        },
        error: () => {
          this.loadingStores.set(false);
        },
      });
  }

  protected categoryIcon(cat: StoreCategoryDto): string {
    const key = `${cat.iconRef ?? ''} ${cat.name} ${cat.nameAr}`.toLowerCase();
    if (key.includes('restaurant') || key.includes('مطاعم') || key.includes('مشوي')) {
      return 'restaurant';
    }
    if (key.includes('grocery') || key.includes('سوبر') || key.includes('بقال')) {
      return 'shopping_bag';
    }
    if (key.includes('pharmacy') || key.includes('صيدل')) {
      return 'medication';
    }
    if (key.includes('bakery') || key.includes('مخابز') || key.includes('أفران') || key.includes('حلو')) {
      return 'bakery_dining';
    }
    if (key.includes('produce') || key.includes('butcher') || key.includes('خضار') || key.includes('جزار')) {
      return 'nutrition';
    }
    return 'storefront';
  }

  protected storeInitial(store: StoreSummaryDto): string {
    const raw = this.i18n.pick(store.nameAr, store.name).trim();
    if (!raw) return 'ن';
    const cleaned = raw
      .replace(/^(مطعم وكشري|مطعم|محل|سوبر ماركت|صيدلية|مخبز|فرن)\s+/i, '')
      .trim();
    return (cleaned || raw).charAt(0).toUpperCase();
  }

  protected selectCategory(categoryId: string): void {
    this.selectedCategoryId.set(categoryId);
    this.immediateSearch$.next();
  }

  protected onAreaChange(event: Event): void {
    this.selectedAreaId.set((event.target as HTMLSelectElement).value);
    this.immediateSearch$.next();
  }

  protected onSearchChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchInput$.next(value);
  }

  protected onSearchEnter(event: Event): void {
    event.preventDefault();
    this.triggerImmediateSearch();
  }

  protected triggerImmediateSearch(): void {
    if ((this.searchQuery()?.trim() ?? '').length === 1) {
      return;
    }
    this.immediateSearch$.next();
  }

  protected clearFilters(): void {
    this.selectedCategoryId.set('');
    this.selectedAreaId.set('');
    this.searchQuery.set('');
    this.searchInput$.next('');
    this.immediateSearch$.next();
  }

  private normalizedSearchQuery(): string {
    const trimmed = this.searchQuery()?.trim() ?? '';
    return trimmed.length === 1 ? '' : trimmed;
  }

  protected signOut(): void {
    this.auth.logout('/').subscribe();
  }
}
