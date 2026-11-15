import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { CourierProfileDto } from '../../../../core/models/api.models';
import { AuthService } from '../../../../core/services/auth.service';
import { CourierApplicationService } from '../../../../core/services/courier-application.service';
import { ParsedApiError, parseHttpError } from '../../../../core/utils/problem-details.util';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';

@Component({
  selector: 'nawly-courier-status-gate',
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
        } @else if (courier(); as c) {
          <div class="nawly-card p-6 sm:p-8">
            <div class="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-[#E7E2D9]">
              <span class="px-3 py-1 rounded-lg text-xs font-extrabold bg-[#F3EFEA] text-[#18181B]">
                {{ i18n.isRtl() ? 'حالة حساب الكابتن' : 'Courier Account Status' }}
              </span>

              @switch (c.status) {
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
                    {{ i18n.isRtl() ? 'موقوف' : 'Suspended' }}
                  </span>
                }
                @case ('Approved') {
                  <span class="px-3.5 py-1 rounded-full text-sm font-extrabold bg-[#F0FDF4] text-[#166534] border border-[#86EFAC]">
                    {{ i18n.isRtl() ? 'معتمد ومفعّل' : 'Approved & Active' }}
                  </span>
                }
              }
            </div>

            <h1 class="mt-5 text-2xl sm:text-3xl font-extrabold text-[#18181B]">
              {{ c.fullName }}
            </h1>

            @switch (c.status) {
              @case ('Pending') {
                <p class="mt-3 text-base text-[#52525B] leading-relaxed">
                  {{
                    i18n.isRtl()
                      ? 'طلب انضمامك كمندوب في نوى قيد المراجعة الآن. سنتواصل معك عبر هاتفك المسجل فور اعتماد الحساب. حفاظاً على خصوصيتك، لا يتم عرض صور المستندات المرفوعة هنا.'
                      : 'Your NAWA courier application is under review. Our team will reach out to your registered mobile number once verification is complete. For your privacy, ID images are never redisplayed here.'
                  }}
                </p>
              }
              @case ('Rejected') {
                <p class="mt-3 text-base text-[#991B1B] font-semibold leading-relaxed">
                  {{
                    c.rejectionReason ||
                      (i18n.isRtl()
                        ? 'لم يتم قبول الطلب. يمكنك التواصل مع الدعم لمعرفة التفاصيل.'
                        : 'Your application was not approved. Please contact support for more details.')
                  }}
                </p>
              }
              @case ('Suspended') {
                <p class="mt-3 text-base text-[#991B1B] font-semibold leading-relaxed">
                  {{
                    i18n.isRtl()
                      ? 'حساب المندوب موقوف مؤقتاً. تواصل مع إدارة التشغيل لحل المشكلة وإعادة التفعيل.'
                      : 'Your courier account is suspended. Please contact operations support to review and restore access.'
                  }}
                </p>
              }
              @case ('Approved') {
                <p class="mt-3 text-base text-[#166534] font-semibold">
                  {{
                    i18n.isRtl()
                      ? 'تم اعتماد حسابك! يمكنك الآن استقبال طلبات التوصيل في نوى.'
                      : 'You are approved! Open the courier duty board to go online.'
                  }}
                </p>
              }
            }

            <div class="mt-7 flex flex-wrap items-center gap-3">
              @if (c.status === 'Approved') {
                <button type="button" class="nawly-btn-primary" (click)="goToCourierHome()">
                  {{ i18n.isRtl() ? 'الدخول إلى لوحة المندوب' : 'Open Courier Board' }}
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
export class CourierStatusGateComponent implements OnInit {
  private readonly courierApp = inject(CourierApplicationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly i18n = inject(I18nService);

  protected readonly loading = signal(true);
  protected readonly courier = signal<CourierProfileDto | null>(null);
  protected readonly apiError = signal<ParsedApiError | null>(null);

  ngOnInit(): void {
    this.loadStatus();
  }

  protected loadStatus(): void {
    this.loading.set(true);
    this.apiError.set(null);

    this.courierApp.getMyCourierProfile().subscribe({
      next: (profile) => {
        this.courier.set(profile);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.apiError.set(parseHttpError(err, this.i18n.locale()));
        this.loading.set(false);
      },
    });
  }

  protected goToCourierHome(): void {
    void this.router.navigateByUrl('/courier');
  }

  protected signOut(): void {
    this.auth.logout('/staff/signin').subscribe();
  }
}
