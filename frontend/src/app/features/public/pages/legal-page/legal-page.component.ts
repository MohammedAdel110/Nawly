import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';

interface LegalSection {
  id: string;
  arTitle: string;
  enTitle: string;
  arBody: string;
  enBody: string;
}

const TERMS_SECTIONS: readonly LegalSection[] = [
  {
    id: 'scope',
    arTitle: '1. التعريف بالخدمة ونطاق العمل',
    enTitle: '1. Service Definition & Scope in Nawa',
    arBody:
      'تعمل منصة نوى (NAWA) كوسيط تقني يربط أهالي قرية نوى والعزب المجاورة (مركز شبين القناطر، محافظة القليوبية) بالمحلات التجارية والمطاعم والصيدليات ومناديب التوصيل المحليين. الأسعار والمنتجات وساعات العمل يحددها كل متجر مباشرة.',
    enBody:
      'NAWA (نوى) connects residents of Nawa village and surrounding hamlets (Shibin El Qanater, Qalyubia) with local groceries, restaurants, bakeries, pharmacies, and independent couriers. Product prices and stock availability are set directly by each participating store.',
  },
  {
    id: 'orders',
    arTitle: '2. تأكيد الطلبات والتجهيز',
    enTitle: '2. Order Confirmation & Preparation',
    arBody:
      'يُعد الطلب مؤكداً فور استلام إشعار قبول المتجر. في حال نفاد أحد الأصناف، يتواصل المتجر أو الدعم مع العميل لاقتراح بديل أو تعديل الفاتورة قبل خروج الكابتن.',
    enBody:
      'An order becomes active once accepted by the store. If an item goes out of stock, the store or support team contacts you to agree on a substitution or remove the item before courier dispatch.',
  },
  {
    id: 'cod',
    arTitle: '3. سياسة الدفع النقدي (كاش عند الاستلام)',
    enTitle: '3. Cash on Delivery (EGP) Policy',
    arBody:
      'تتم تسوية معظم الطلبات نقدًا بالجنيه المصري (EGP) مباشرة لكابتن التوصيل عند باب المنزل بعد مطابقة كود التسليم المكوّن من 4 أرقام. يلتزم العميل بدفع قيمة الفاتورة والرسوم المعلنة بالطلب.',
    enBody:
      'Orders are settled primarily in Cash on Delivery (EGP) directly with the courier at handoff after matching the 4-digit delivery verification code.',
  },
  {
    id: 'cancellations',
    arTitle: '4. الإلغاء والاسترجاع',
    enTitle: '4. Cancellations & Refunds',
    arBody:
      'يمكن إلغاء الطلب مجاناً قبل بدء المتجر في التجهيز. في حال وجود خطأ في الطلب أو تلف أثناء التوصيل، يحق للعميل التواصل مع الدعم خلال 24 ساعة للتعويض أو الاسترداد.',
    enBody:
      'Free cancellation is supported before preparation begins. Report damaged or incorrect deliveries within 24 hours for refund or replacement support.',
  },
  {
    id: 'kyc-privacy',
    arTitle: '5. حماية البيانات الشخصية والبطاقات (قانون 151 لسنة 2020)',
    enTitle: '5. Personal Data & National ID Privacy (Law 151/2020)',
    arBody:
      'تُحفظ أرقام الهواتف والعناوين وصور بطاقات الرقم القومي لكباتن التوصيل بتشفير كامل وفقاً لقانون حماية البيانات الشخصية المصري رقم 151 لسنة 2020. ولا يُسمح بأي حال بمشاركة بيانات السكان أو عرض صور البطاقات.',
    enBody:
      'In accordance with Egypt Personal Data Protection Law No. 151 of 2020, resident phone numbers, addresses, and courier National ID verification files are encrypted and strictly restricted to authorized onboarding reviewers.',
  },
  {
    id: 'disputes',
    arTitle: '6. النزاعات والدعم المحلي',
    enTitle: '6. Disputes & Local Resolution',
    arBody:
      'يلتزم فريق إدارة نوى (NAWA) بالتدخل العادل والفوري لحل أي خلاف حول جودة المنتج أو رسوم التوصيل خلال 24 ساعة عبر قنوات الدعم الرسمية.',
    enBody:
      'NAWA Operations mediates order quality or delivery disputes promptly within 24 hours through official support channels.',
  },
];

const PRIVACY_SECTIONS: readonly LegalSection[] = [
  {
    id: 'collection',
    arTitle: '1. البيانات التي نجمعها',
    enTitle: '1. Data We Collect',
    arBody:
      'للعملاء: رقم الموبايل المصري، الاسم، وعناوين التوصيل داخل نوى لتسليم الطلبات. للمتاجر والكباتن: بيانات النشاط التجاري، ومستند بطاقة الرقم القومي للكابتن للتحقق الأمني فقط.',
    enBody:
      'For customers: Egyptian mobile number, full name, and Nawa delivery addresses. For partners and couriers: store contact details and courier National ID verification files for safety checks.',
  },
  {
    id: 'law151',
    arTitle: '2. الموافقة وحقوقك وفق قانون حماية البيانات (151 لسنة 2020)',
    enTitle: '2. Consent & Egypt Data Protection Law No. 151 of 2020',
    arBody:
      'باستخدامك نوى وطلب كود الدخول، فإنك توافق صراحةً على معالجة رقم هاتفك وعنوانك لتنفيذ طلباتك. طبقاً للقانون المصري رقم 151 لسنة 2020، يحق لك الاطلاع على بياناتك وتصحيحها أو طلب حذف حسابك.',
    enBody:
      'Under Egypt Personal Data Protection Law No. 151 of 2020, we process your mobile number and address strictly based on your explicit consent to authenticate your session and deliver orders.',
  },
  {
    id: 'kyc-confidentiality',
    arTitle: '3. سرية مستندات الكباتن (البطاقة الشخصية)',
    enTitle: '3. Courier National ID Confidentiality',
    arBody:
      'لا تظهر صورة البطاقة الشخصية لأي عميل أو صاحب محل أو في واجهات التطبيق بعد الرفع. يقتصر الاطلاع عليها على مسؤول العمليات المصرح له أثناء مراجعة طلب الانضمام فقط.',
    enBody:
      'Courier National ID photos are never displayed to customers, stores, or in browser screens after selection. Access is limited exclusively to authorized operations administrators.',
  },
  {
    id: 'retention',
    arTitle: '4. الأمان والاحتفاظ بالبيانات',
    enTitle: '4. Security & Data Retention',
    arBody:
      'نحمي جميع البيانات عبر اتصال مشفر وتدوير آمن لرموز الجلسة، ولا نبيع أو نشارك أرقام هواتف أهالي نوى مع أي جهات تسويقية خارجية.',
    enBody:
      'All communications use encrypted transport and secure token rotation. We never sell or rent customer phone numbers to third-party advertisers.',
  },
];

@Component({
  selector: 'nawly-legal-page',
  standalone: true,
  imports: [RouterLink, NawlyAppShellPublicComponent],
  template: `
    <nawly-app-shell-public
      brandIcon="gavel"
      [brandBadge]="i18n.isRtl() ? 'الوثائق القانونية' : 'Legal & Privacy'"
    >
      <div class="max-w-5xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-10">
        <!-- Header & Document Toggle -->
        <div class="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <span class="inline-flex items-center px-3 py-1 rounded-full text-label-sm font-label font-bold bg-light-primary-container/60 text-light-on-primary-container">
              {{ isPrivacy() ? (i18n.isRtl() ? 'سياسة الخصوصية وحماية البيانات' : 'Privacy Policy') : (i18n.isRtl() ? 'الشروط والأحكام الرسمية' : 'Terms & Conditions') }}
            </span>
            <h1 class="mt-3 text-headline-lg sm:text-headline-xl font-headline text-light-on-surface">
              {{
                isPrivacy()
                  ? (i18n.isRtl() ? 'سياسة الخصوصية وحماية البيانات' : 'Privacy & Data Protection Policy')
                  : (i18n.isRtl() ? 'الشروط والأحكام واتفاقية الاستخدام' : 'Terms & Conditions of Service')
              }}
            </h1>
            <p class="mt-1 text-body-sm text-light-outline">
              {{
                i18n.isRtl()
                  ? 'آخر تحديث: نوفمبر 2026 • النسخة الرسمية لقرية نوى، القليوبية'
                  : 'Last updated: November 2026 • Official Edition for Nawa Village, Qalyubia'
              }}
            </p>
          </div>

          <div class="flex items-center gap-2">
            <a
              routerLink="/terms"
              class="min-h-[44px] px-4 py-2 rounded-xl text-label-md font-label font-bold inline-flex items-center transition-colors"
              [class.bg-light-primary]="!isPrivacy()"
              [class.text-white]="!isPrivacy()"
              [class.bg-white]="isPrivacy()"
              [class.border]="isPrivacy()"
              [class.border-light-outline-variant]="isPrivacy()"
              [class.text-light-on-surface]="isPrivacy()"
            >
              {{ i18n.dict().navTerms }}
            </a>
            <a
              routerLink="/privacy"
              class="min-h-[44px] px-4 py-2 rounded-xl text-label-md font-label font-bold inline-flex items-center transition-colors"
              [class.bg-light-primary]="isPrivacy()"
              [class.text-white]="isPrivacy()"
              [class.bg-white]="!isPrivacy()"
              [class.border]="!isPrivacy()"
              [class.border-light-outline-variant]="!isPrivacy()"
              [class.text-light-on-surface]="!isPrivacy()"
            >
              {{ i18n.dict().navPrivacy }}
            </a>
          </div>
        </div>

        <!-- 12-Col Layout: Sticky TOC + White Article Card -->
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          <!-- Table of Contents -->
          <aside class="lg:col-span-4 bg-white rounded-2xl border border-light-outline-variant/80 p-6 lg:sticky lg:top-28 space-y-4">
            <h2 class="font-headline font-bold text-light-on-surface text-lg flex items-center gap-2">
              <span class="material-symbols-outlined text-light-primary" aria-hidden="true">menu_book</span>
              <span>{{ i18n.isRtl() ? 'فهرس البنود والوثيقة' : 'Document Index' }}</span>
            </h2>

            <nav class="space-y-1.5 text-body-sm" aria-label="Document sections">
              @for (sec of sections(); track sec.id) {
                <button
                  type="button"
                  class="w-full text-start p-2.5 rounded-lg font-medium transition-colors cursor-pointer"
                  [class.bg-light-primary-container/40]="activeSectionId() === sec.id"
                  [class.text-light-primary]="activeSectionId() === sec.id"
                  [class.font-bold]="activeSectionId() === sec.id"
                  [class.text-light-on-surface-variant]="activeSectionId() !== sec.id"
                  [class.hover:bg-light-surface-variant/30]="activeSectionId() !== sec.id"
                  (click)="scrollToSection(sec.id)"
                >
                  {{ i18n.pick(sec.arTitle, sec.enTitle) }}
                </button>
              }
            </nav>

            <div class="pt-4 border-t border-light-outline-variant/60 space-y-1.5">
              <span class="text-label-sm text-light-outline block">
                {{ i18n.isRtl() ? 'للاستفسارات القانونية والخصوصية:' : 'Legal & privacy inquiries:' }}
              </span>
              <span class="text-body-sm font-code font-bold text-light-primary ltr-num block">
                legal&#64;nawa.eg
              </span>
            </div>
          </aside>

          <!-- Document Article Body -->
          <article class="lg:col-span-8 bg-white rounded-2xl border border-light-outline-variant/80 p-6 sm:p-10 shadow-xs space-y-8">
            <div class="border-b border-light-outline-variant/60 pb-6">
              <h2 class="text-headline-md font-headline font-bold text-light-on-surface mb-2">
                {{
                  isPrivacy()
                    ? (i18n.isRtl() ? 'حماية بيانات أهالي وشركاء قرية نوى' : 'Protecting Nawa Residents & Partner Data')
                    : (i18n.isRtl() ? 'الشروط والأحكام العامة لاستخدام منصة نوى' : 'General Terms & Conditions for Using NAWA')
                }}
              </h2>
              <p class="text-body-sm text-light-outline">
                {{
                  i18n.isRtl()
                    ? 'إطار قانوني واضح يضمن الشفافية التامة لأهالي نوى وأصحاب المحلات والكباتن.'
                    : 'Clear legal standards ensuring transparency for Nawa residents, local shops, and couriers.'
                }}
              </p>
            </div>

            <div class="space-y-8 leading-relaxed">
              @for (sec of sections(); track sec.id) {
                <section [id]="'legal-' + sec.id" class="scroll-mt-28">
                  <h3 class="text-headline-sm font-headline font-bold text-light-primary mb-2">
                    {{ i18n.pick(sec.arTitle, sec.enTitle) }}
                  </h3>
                  <p class="text-body-md text-light-on-surface-variant leading-relaxed">
                    {{ i18n.pick(sec.arBody, sec.enBody) }}
                  </p>
                </section>
              }
            </div>

            <div class="pt-6 border-t border-light-outline-variant/60 flex flex-wrap items-center justify-between gap-4">
              <p class="text-body-sm text-light-on-surface-variant">
                {{
                  i18n.isRtl()
                    ? 'للاستفسارات أو ممارسة حقوق البيانات الشخصية يرجى التواصل عبر مركز المساعدة.'
                    : 'To ask questions or exercise your data rights, contact our Support Desk.'
                }}
              </p>
              <a
                routerLink="/help"
                class="min-h-[48px] px-5 py-2.5 rounded-xl bg-light-primary hover:bg-teal-900 text-white font-label font-bold text-sm inline-flex items-center gap-2 transition-colors"
              >
                <span class="material-symbols-outlined text-lg" aria-hidden="true">help</span>
                <span>{{ i18n.dict().navHelp }}</span>
              </a>
            </div>
          </article>
        </div>
      </div>
    </nawly-app-shell-public>
  `,
})
export class LegalPageComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly i18n = inject(I18nService);

  protected readonly isPrivacy = computed(
    () => (this.route.snapshot.data['legalType'] as string | undefined) === 'privacy',
  );

  protected readonly sections = computed(() =>
    this.isPrivacy() ? PRIVACY_SECTIONS : TERMS_SECTIONS,
  );

  protected readonly activeSectionId = signal<string>('');

  protected scrollToSection(id: string): void {
    this.activeSectionId.set(id);
    const el = document.getElementById(`legal-${id}`);
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
