import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { StoreDetailsDto } from '../../../../core/models/api.models';
import { AuthService } from '../../../../core/services/auth.service';
import { DiscoveryService } from '../../../../core/services/discovery.service';
import { ParsedApiError, parseHttpError } from '../../../../core/utils/problem-details.util';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';

@Component({
  selector: 'nawly-store-status-gate',
  standalone: true,
  imports: [RouterLink, NawlyAppShellPublicComponent],
  template: `
    <nawly-app-shell-public>
      <div class="max-w-2xl mx-auto px-4 sm:px-6 py-10 sm:py-14">
        @if (loading()) {
          <div class="nawly-card p-6 sm:p-8 space-y-4">
            <div class="nawly-skeleton h-8 w-44"></div>
            <div class="nawly-skeleton h-6 w-3/4"></div>
            <div class="nawly-skeleton h-24 w-full"></div>
          </div>
        } @else if (apiError(); as err) {
          <div class="nawly-card p-6 sm:p-8">
            <div class="nawly-banner-error mb-5">
              <span>{{ err.message }}</span>
            </div>
            <div class="flex flex-wrap gap-3">
              <button type="button" class="nawly-btn-primary" (click)="loadStatus()">
                {{ i18n.dict().retry }}
              </button>
              <a routerLink="/help" class="nawly-btn-outline">
                {{ i18n.dict().navHelp }}
              </a>
            </div>
          </div>
        } @else if (store(); as s) {
          <div class="nawly-card p-6 sm:p-8">
            <div class="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-[#E7E2D9]">
              <span class="px-3 py-1 rounded-lg text-xs font-extrabold bg-[#F3EFEA] text-[#18181B]">
                {{ i18n.isRtl() ? 'حالة حساب المتجر' : 'Store Account Status' }}
              </span>

              @switch (s.status) {
                @case ('Pending') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#FFFBEB] text-[#92400E] border border-[#FCD34D]">
                    {{ i18n.isRtl() ? 'قيد المراجعة' : 'Pending Review' }}
                  </span>
                }
                @case ('Rejected') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#FEF2F2] text-[#991B1B] border border-[#FCA5A5]">
                    {{ i18n.isRtl() ? 'مرفوض' : 'Rejected' }}
                  </span>
                }
                @case ('Suspended') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#FEF2F2] text-[#991B1B] border border-[#FCA5A5]">
                    {{ i18n.isRtl() ? 'موقوف مؤقتاً' : 'Suspended' }}
                  </span>
                }
                @case ('Closed') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#F3EFEA] text-[#52525B] border border-[#D6D0C4]">
                    {{ i18n.isRtl() ? 'مغلق' : 'Closed' }}
                  </span>
                }
                @case ('Active') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#F0FDF4] text-[#166534] border border-[#86EFAC]">
                    {{ i18n.isRtl() ? 'نشط ومعتمد' : 'Active & Approved' }}
                  </span>
                }
              }
            </div>

            <h1 class="mt-5 text-2xl sm:text-3xl font-extrabold text-[#18181B]">
              {{ i18n.pick(s.nameAr, s.name) }}
            </h1>

            @switch (s.status) {
              @case ('Pending') {
                <p class="mt-3 text-base text-[#52525B] leading-relaxed">
                  {{
                    i18n.isRtl()
                      ? 'طلب انضمام متجرك قيد المراجعة من فريق إدارة نوى. نقوم عادةً بالرد خلال 24 ساعة عبر الهاتف أو عند تحديث الحالة.'
                      : 'Your store application is currently under review by our NAWA operations team. Reviews are typically completed within 24 hours.'
                  }}
                </p>
              }
              @case ('Rejected') {
                <p class="mt-3 text-base text-[#991B1B] leading-relaxed font-semibold">
                  {{
                    s.rejectionReason ||
                      (i18n.isRtl()
                        ? 'لم تتم الموافقة على الطلب الحالي. تواصل مع الدعم لتوضيح المتطلبات أو إعادة التقديم.'
                        : 'This store application was not approved. Please contact support for details or submit a corrected application.')
                  }}
                </p>
              }
              @case ('Suspended') {
                <p class="mt-3 text-base text-[#991B1B] leading-relaxed font-semibold">
                  {{
                    i18n.isRtl()
                      ? 'تم إيقاف حساب المتجر مؤقتاً. يرجى التواصل مع فريق دعم نوى لمعرفة السبب وإعادة التفعيل.'
                      : 'Your store account is currently suspended. Please reach out to NAWA support to resolve the suspension.'
                  }}
                </p>
              }
              @case ('Closed') {
                <p class="mt-3 text-base text-[#52525B] leading-relaxed">
                  {{
                    i18n.isRtl()
                      ? 'هذا المتجر مغلق حالياً على المنصة. تواصل مع الإدارة إذا كنت ترغب في إعادة فتحه.'
                      : 'This store account is currently closed. Contact operations if you would like to reopen it.'
                  }}
                </p>
              }
              @case ('Active') {
                <p class="mt-3 text-base text-[#166534] font-semibold">
                  {{
                    i18n.isRtl()
                      ? 'متجرك مفعّل الآن! يمكنك الدخول مباشرة إلى لوحة إدارة الطلبات والمنيو.'
                      : 'Your store is active and ready for orders!'
                  }}
                </p>
              }
            }

            <div class="mt-7 flex flex-wrap items-center gap-3">
              @if (s.status === 'Active') {
                <button type="button" class="nawly-btn-primary" (click)="goToStoreDashboard()">
                  {{ i18n.isRtl() ? 'الدخول إلى لوحة المتجر' : 'Open Store Dashboard' }}
                </button>
              } @else {
                <button type="button" class="nawly-btn-primary" (click)="loadStatus()">
                  {{ i18n.isRtl() ? 'تحديث الحالة' : 'Refresh status' }}
                </button>
              }

              <a routerLink="/help" class="nawly-btn-outline">
                {{ i18n.dict().navHelp }}
              </a>

              <button type="button" class="nawly-btn-ghost text-[#DC2626]" (click)="signOut()">
                {{ i18n.dict().navSignOut }}
              </button>
            </div>
          </div>
        }
      </div>
    </nawly-app-shell-public>
  `,
})
export class StoreStatusGateComponent implements OnInit {
  private readonly discovery = inject(DiscoveryService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly i18n = inject(I18nService);

  protected readonly loading = signal(true);
  protected readonly store = signal<StoreDetailsDto | null>(null);
  protected readonly apiError = signal<ParsedApiError | null>(null);

  ngOnInit(): void {
    this.loadStatus();
  }

  protected loadStatus(): void {
    this.loading.set(true);
    this.apiError.set(null);

    this.discovery.getStorePortalSettings().subscribe({
      next: (details) => {
        this.store.set(details);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.apiError.set(parseHttpError(err, this.i18n.locale()));
        this.loading.set(false);
      },
    });
  }

  protected goToStoreDashboard(): void {
    void this.router.navigateByUrl('/store');
  }

  protected signOut(): void {
    this.auth.logout('/staff/signin').subscribe();
  }
}
