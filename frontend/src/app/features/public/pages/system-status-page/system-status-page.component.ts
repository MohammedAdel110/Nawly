import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';

export type SystemStatusKind = 'not-found' | 'forbidden' | 'error' | 'offline' | 'maintenance';

@Component({
  selector: 'nawly-system-status-page',
  standalone: true,
  imports: [RouterLink, NawlyAppShellPublicComponent],
  template: `
    <nawly-app-shell-public
      [brandIcon]="iconName()"
      [brandBadge]="i18n.isRtl() ? 'حالة النظام' : 'System Status'"
    >
      <div class="flex-1 flex items-center justify-center p-4 sm:p-6">
        <div class="max-w-lg w-full bg-white rounded-3xl border border-light-outline-variant/80 shadow-md p-8 sm:p-12 text-center space-y-6">
          <div
            class="w-24 h-24 mx-auto rounded-full flex items-center justify-center border"
            [class.bg-red-50]="kind() === 'forbidden' || kind() === 'error'"
            [class.text-error]="kind() === 'forbidden' || kind() === 'error'"
            [class.border-red-200]="kind() === 'forbidden' || kind() === 'error'"
            [class.bg-amber-50]="kind() === 'offline' || kind() === 'maintenance'"
            [class.text-warning]="kind() === 'offline' || kind() === 'maintenance'"
            [class.border-amber-200]="kind() === 'offline' || kind() === 'maintenance'"
            [class.bg-light-primary-container/50]="kind() === 'not-found'"
            [class.text-light-primary]="kind() === 'not-found'"
            [class.border-light-primary/20]="kind() === 'not-found'"
            aria-hidden="true"
          >
            <span class="material-symbols-outlined text-5xl">{{ iconName() }}</span>
          </div>

          <div class="space-y-2">
            <span class="font-code text-display-lg font-extrabold text-light-outline block ltr-num">
              {{ badgeCode() }}
            </span>
            <h1 class="text-headline-md font-headline font-bold text-light-on-surface">
              {{ headline() }}
            </h1>
            <p class="text-body-md text-light-on-surface-variant leading-relaxed">
              {{ description() }}
            </p>
          </div>

          @if (kind() === 'error' && traceId()) {
            <div class="p-4 rounded-xl bg-light-surface-variant/30 border border-light-outline-variant/60 flex flex-wrap items-center justify-between gap-2 text-start">
              <div class="min-w-0">
                <p class="text-label-sm font-label text-light-outline">{{ i18n.dict().errTraceLabel }}</p>
                <p class="text-body-sm font-code font-bold text-light-on-surface truncate ltr-num">
                  {{ traceId() }}
                </p>
              </div>
              <button
                type="button"
                class="min-h-[40px] px-3.5 py-1.5 rounded-lg border border-light-outline-variant bg-white hover:bg-light-surface-variant/40 text-label-sm font-label font-bold text-light-on-surface cursor-pointer"
                (click)="copyTraceId()"
              >
                {{ copied() ? i18n.dict().copied : i18n.dict().copy }}
              </button>
            </div>
          }

          <div class="pt-2 flex flex-col sm:flex-row gap-3 justify-center">
            @if (kind() === 'offline' || kind() === 'error') {
              <button
                type="button"
                class="min-h-[48px] px-6 py-3 bg-light-primary hover:bg-teal-900 text-white font-label font-bold rounded-xl shadow-sm inline-flex items-center justify-center gap-2 cursor-pointer transition-colors"
                (click)="retryNavigation()"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">refresh</span>
                <span>{{ i18n.dict().retry }}</span>
              </button>
            }

            <a
              routerLink="/"
              class="min-h-[48px] px-6 py-3 bg-light-primary hover:bg-teal-900 text-white font-label font-bold rounded-xl shadow-sm inline-flex items-center justify-center gap-2 transition-colors"
              [class.bg-white]="kind() === 'offline' || kind() === 'error'"
              [class.text-light-on-surface]="kind() === 'offline' || kind() === 'error'"
              [class.border]="kind() === 'offline' || kind() === 'error'"
              [class.border-light-outline-variant]="kind() === 'offline' || kind() === 'error'"
            >
              <span class="material-symbols-outlined text-lg" aria-hidden="true">home</span>
              <span>{{ i18n.isRtl() ? 'العودة لصفحة نوى الرئيسية' : 'Back to NAWA Home' }}</span>
            </a>

            <a
              routerLink="/help"
              class="min-h-[48px] px-6 py-3 border border-light-outline-variant hover:bg-light-surface-variant/40 text-light-on-surface font-label font-bold rounded-xl inline-flex items-center justify-center gap-2 transition-colors"
            >
              <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">support_agent</span>
              <span>{{ i18n.isRtl() ? 'تواصل مع الدعم الفني' : 'Contact Support' }}</span>
            </a>
          </div>
        </div>
      </div>
    </nawly-app-shell-public>
  `,
})
export class SystemStatusPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly i18n = inject(I18nService);

  protected readonly copied = signal(false);

  protected readonly kind = computed<SystemStatusKind>(
    () => (this.route.snapshot.data['kind'] as SystemStatusKind | undefined) ?? 'not-found',
  );

  protected readonly traceId = computed(() => {
    const queryTrace = this.route.snapshot.queryParamMap.get('traceId');
    const stateTrace = (globalThis.history?.state as { traceId?: string } | null)?.traceId;
    return queryTrace || stateTrace || null;
  });

  protected readonly iconName = computed(() => {
    switch (this.kind()) {
      case 'not-found':
        return 'wrong_location';
      case 'forbidden':
        return 'lock';
      case 'error':
        return 'dns';
      case 'offline':
        return 'wifi_off';
      case 'maintenance':
        return 'engineering';
    }
  });

  protected readonly badgeCode = computed(() => {
    switch (this.kind()) {
      case 'not-found':
        return '404';
      case 'forbidden':
        return '403';
      case 'error':
        return '500';
      case 'offline':
        return 'OFFLINE';
      case 'maintenance':
        return 'MAINT';
    }
  });

  protected readonly headline = computed(() => {
    const ar = this.i18n.isRtl();
    switch (this.kind()) {
      case 'not-found':
        return ar ? 'الصفحة التي تبحث عنها غير موجودة' : 'Page not found';
      case 'forbidden':
        return ar ? 'غير مصرح بالدخول لهذه الصفحة' : 'Access not permitted';
      case 'error':
        return ar ? 'حدث خطأ غير متوقع في الخادم' : 'Unexpected server error';
      case 'offline':
        return ar ? 'انقطع الاتصال بالإنترنت' : 'You are currently offline';
      case 'maintenance':
        return ar ? 'نوى في صيانة سريعة' : 'Scheduled maintenance in progress';
    }
  });

  protected readonly description = computed(() => {
    const ar = this.i18n.isRtl();
    switch (this.kind()) {
      case 'not-found':
        return ar
          ? 'يبدو أن الرابط قد تغير أو أن الصفحة لم تعد متوفرة. يمكنك العودة لتصفح متاجر قرية نوى بسهولة.'
          : 'The link you followed could not be found or may have moved. Head back home to browse Nawa stores.';
      case 'forbidden':
        return ar
          ? 'حسابك الحالي لا يملك صلاحية عرض هذه اللوحة. تأكد من تسجيل الدخول بالحساب الصحيح.'
          : 'Your current account role does not have permission to open this area.';
      case 'error':
        return ar
          ? 'واجهنا مشكلة مؤقتة أثناء تنفيذ طلبك. يمكنك المحاولة مرة أخرى أو مشاركة رقم المرجع مع الدعم.'
          : 'We hit a temporary problem completing your request. Try again or share the reference ID with support.';
      case 'offline':
        return ar
          ? 'تعذّر الوصول لشبكة الإنترنت الآن. تحقق من الواي فاي أو بيانات الموبايل ثم اضغط أعد المحاولة.'
          : 'No internet connection was detected. Check your mobile data or Wi-Fi and tap try again.';
      case 'maintenance':
        return ar
          ? 'نجري تحديثاً سريعاً لتحسين سرعة الخدمة لأهالي نوى. سنعود خلال دقائق قليلة.'
          : 'We are performing a quick system upgrade to keep Nawa ordering fast. Back in a few minutes.';
    }
  });

  protected copyTraceId(): void {
    const id = this.traceId();
    if (!id) return;
    void globalThis.navigator?.clipboard?.writeText(id).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  protected retryNavigation(): void {
    void this.router.navigateByUrl('/');
  }
}
