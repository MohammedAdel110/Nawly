import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../../core/i18n/i18n.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'nawly-app-shell-public',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex flex-col bg-light-surface text-light-on-surface font-body">
      <a href="#main-content" class="skip-link">
        {{ i18n.dict().skipToContent }}
      </a>

      <!-- Sticky Header -->
      <header class="bg-white/95 backdrop-blur-xs border-b border-slate-200 sticky top-0 z-40">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 h-20 flex items-center justify-between gap-3">
          <!-- Brand Mark & Clean Location Identifier -->
          <div class="flex items-center gap-3 min-w-0">
            <a
              routerLink="/"
              class="flex items-center min-h-[48px] rounded-xl focus-visible:outline-light-primary shrink-0"
            >
              <img
                src="icons/nawa-logo.png"
                [alt]="i18n.isRtl() ? 'نوى NAWA — كل طلباتك لحد عندك' : 'NAWA (نوى) — All your orders to your door'"
                class="h-11 sm:h-12 w-auto object-contain shrink-0 select-none"
              />
            </a>

            <span class="hidden sm:block h-6 w-px bg-slate-200 shrink-0" aria-hidden="true"></span>

            <span
              class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-100 text-slate-700 text-xs font-label font-bold whitespace-nowrap"
            >
              @if (brandBadge()) {
                <span>{{ brandBadge() }}</span>
              } @else {
                <span class="material-symbols-outlined text-base text-accent" aria-hidden="true">location_on</span>
                <span>{{ i18n.isRtl() ? 'قرية نوى، القليوبية' : 'Nawa, Qalyubia' }}</span>
              }
            </span>
          </div>

          <!-- Clean Typographic Navigation & Actions -->
          <nav class="flex items-center gap-1.5 sm:gap-3 shrink-0" aria-label="Main navigation">
            <a
              routerLink="/stores"
              class="hidden lg:inline-flex items-center text-sm font-label font-bold text-slate-600 hover:text-slate-900 transition-colors min-h-[44px] px-3 rounded-lg hover:bg-slate-50"
            >
              {{ i18n.isRtl() ? 'تصفح المحلات' : 'Browse Stores' }}
            </a>

            <a
              routerLink="/join/store"
              class="hidden xl:inline-flex items-center text-sm font-label font-bold text-slate-600 hover:text-slate-900 transition-colors min-h-[44px] px-3 rounded-lg hover:bg-slate-50"
            >
              {{ i18n.isRtl() ? 'سجّل متجرك' : 'For Stores' }}
            </a>

            <a
              routerLink="/join/courier"
              class="hidden xl:inline-flex items-center text-sm font-label font-bold text-slate-600 hover:text-slate-900 transition-colors min-h-[44px] px-3 rounded-lg hover:bg-slate-50"
            >
              {{ i18n.isRtl() ? 'انضم ككابتن' : 'For Couriers' }}
            </a>

            <a
              routerLink="/help"
              class="hidden md:inline-flex items-center text-sm font-label font-bold text-slate-600 hover:text-slate-900 transition-colors min-h-[44px] px-3 rounded-lg hover:bg-slate-50"
            >
              {{ i18n.isRtl() ? 'المساعدة' : 'Help' }}
            </a>

            <button
              type="button"
              class="inline-flex items-center gap-1.5 h-10 px-3 rounded-lg border border-slate-200 text-xs font-label font-bold text-slate-700 hover:bg-slate-100 transition-colors cursor-pointer"
              [attr.aria-label]="i18n.isRtl() ? 'Switch to English' : 'التبديل إلى العربية'"
              (click)="i18n.toggleLocale()"
            >
              <span class="material-symbols-outlined text-base text-slate-500" aria-hidden="true">language</span>
              <span>{{ i18n.isRtl() ? 'English' : 'العربية' }}</span>
            </button>

            @if (auth.isAuthenticated()) {
              <a
                [routerLink]="auth.resolveHomeRoute()"
                class="hidden sm:inline-flex items-center gap-1.5 h-10 px-4 rounded-xl bg-slate-900 hover:bg-slate-800 text-white text-xs font-label font-bold transition-colors"
              >
                <span class="material-symbols-outlined text-base" aria-hidden="true">dashboard</span>
                <span>{{ i18n.dict().navMyPortal }}</span>
              </a>
            } @else if (portalMode() === 'staff') {
              <a
                routerLink="/signin"
                class="hidden sm:inline-flex items-center gap-1.5 h-10 px-4 rounded-xl bg-slate-900 hover:bg-slate-800 text-white text-xs font-label font-bold transition-colors"
              >
                <span>{{ i18n.isRtl() ? 'دخول العملاء' : 'Customer Sign in' }}</span>
              </a>
            } @else if (!hideAuthButton() || showPartnerPortalCta()) {
              <a
                routerLink="/staff/signin"
                class="hidden sm:inline-flex items-center gap-1.5 h-10 px-4 rounded-xl bg-slate-900 hover:bg-slate-800 text-white text-xs font-label font-bold transition-colors"
              >
                <span>{{ i18n.isRtl() ? 'بوابة الشركاء' : 'Partner Portal' }}</span>
              </a>
            }
          </nav>
        </div>
      </header>

      <main id="main-content" class="flex-1 flex flex-col">
        <ng-content />
      </main>

      <!-- Minimal Village Footer -->
      <footer class="bg-white border-t border-light-outline-variant/50 py-8 px-6 mt-auto">
        <div
          class="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4 text-body-sm text-light-on-surface-variant"
        >
          <div class="flex items-center gap-3 text-center sm:text-start">
            <img
              src="icons/nawa-logo.png"
              [alt]="i18n.isRtl() ? 'نوى NAWA' : 'NAWA'"
              class="h-10 w-auto object-contain shrink-0 select-none"
            />
            <span>
              {{
                i18n.isRtl()
                  ? 'نوى (NAWA) © 2026 • كل طلباتك لحد عندك — قرية نوى، مركز شبين القناطر، القليوبية'
                  : 'NAWA (نوى) © 2026 • All your orders to your door — Nawa Village, Shibin El Qanater, Qalyubia'
              }}
            </span>
          </div>
          <div class="flex flex-wrap items-center justify-center gap-4 sm:gap-6 text-label-md">
            <a routerLink="/stores" class="hover:text-light-primary transition-colors">
              {{ i18n.isRtl() ? 'تصفح المحلات' : 'Browse Stores' }}
            </a>
            <a routerLink="/join/store" class="hover:text-light-primary transition-colors">
              {{ i18n.isRtl() ? 'انضم كصاحب متجر' : 'Join as Store' }}
            </a>
            <a routerLink="/join/courier" class="hover:text-light-primary transition-colors">
              {{ i18n.isRtl() ? 'انضم ككابتن توصيل' : 'Join as Courier' }}
            </a>
            <a routerLink="/terms" class="hover:text-light-primary transition-colors">
              {{ i18n.isRtl() ? 'الشروط والخصوصية' : 'Terms & Privacy' }}
            </a>
            <a routerLink="/help" class="hover:text-light-primary transition-colors">
              {{ i18n.isRtl() ? 'مركز المساعدة' : 'Help Center' }}
            </a>
          </div>
        </div>
      </footer>
    </div>
  `,
})
export class NawlyAppShellPublicComponent {
  protected readonly i18n = inject(I18nService);
  protected readonly auth = inject(AuthService);

  readonly hideAuthButton = input<boolean>(false);
  readonly showPartnerPortalCta = input<boolean>(true);
  readonly brandIcon = input<string>('local_shipping');
  readonly brandBadge = input<string>('');
  readonly portalMode = input<'customer' | 'staff'>('customer');

  protected readonly resolvedBadge = computed(() => {
    const custom = this.brandBadge();
    if (custom) return custom;
    return this.i18n.isRtl() ? 'نوى • القليوبية' : 'Nawa • Qalyubia';
  });
}
