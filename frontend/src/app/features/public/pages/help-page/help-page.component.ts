import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { NawlyAppShellPublicComponent } from '../../../../shared/components/app-shell-public/nawly-app-shell-public.component';

type FaqGroupKey = 'ordering' | 'cash' | 'zones' | 'cancellations';

interface FaqItem {
  id: string;
  group: FaqGroupKey;
  arQ: string;
  enQ: string;
  arA: string;
  enA: string;
}

interface FaqGroupMeta {
  key: FaqGroupKey;
  icon: string;
  arTitle: string;
  enTitle: string;
}

const FAQ_GROUPS: readonly FaqGroupMeta[] = [
  {
    key: 'ordering',
    icon: 'shopping_cart',
    arTitle: '1. الطلب واختيار المحلات',
    enTitle: '1. Ordering & Stores',
  },
  {
    key: 'cash',
    icon: 'payments',
    arTitle: '2. الدفع كاش والمبالغ المالية',
    enTitle: '2. Cash Payment & Pricing',
  },
  {
    key: 'zones',
    icon: 'moped',
    arTitle: '3. مناطق التوصيل والسرعة',
    enTitle: '3. Delivery Zones & Speed',
  },
  {
    key: 'cancellations',
    icon: 'cancel',
    arTitle: '4. الإلغاء، الشركاء، والخصوصية',
    enTitle: '4. Cancellations, Partners & Privacy',
  },
];

const FAQS: readonly FaqItem[] = [
  {
    id: 'how-to-order',
    group: 'ordering',
    arQ: 'كيف أطلب من مطعم أو سوبرماركت؟',
    enQ: 'How do I order from a local store?',
    arA: 'تصفح المحلات، أضف منتجاتك للسلة، حدد منطقتك داخل نوى، وسجّل برقم موبايلك لتأكيد الطلب برسالة OTP مجانية.',
    enA: 'Browse Nawa stores, add items to your cart, pick your village area, and sign in with your mobile OTP to confirm.',
  },
  {
    id: 'same-price',
    group: 'ordering',
    arQ: 'هل الأسعار نفس أسعار المحل؟',
    enQ: 'Are prices the same as in-shop prices?',
    arA: 'نعم، تلتزم المحلات المشاركة بعرض نفس أسعار البيع داخل المحل دون أي زيادات خفية على الأصناف.',
    enA: 'Yes, participating Nawa shops list their authentic counter prices with zero hidden item markups.',
  },
  {
    id: 'cod',
    group: 'cash',
    arQ: 'كيف أدفع ثمن الطلب؟',
    enQ: 'How do I pay for my order?',
    arA: 'الدفع نقدًا عند الاستلام (كاش) للكابتن بباب البيت بالجنيه المصري بعد التأكد من كود التسليم المكون من 4 أرقام.',
    enA: 'Pay Cash on Delivery in Egyptian Pounds (EGP) directly to the courier after confirming your 4-digit delivery code.',
  },
  {
    id: 'delivery-fee',
    group: 'cash',
    arQ: 'كم تبلغ تكلفة التوصيل داخل نوى؟',
    enQ: 'What is the delivery fee inside Nawa?',
    arA: 'تتراوح رسوم التوصيل حسب المسافة من 10 إلى 20 جنيه مصري وتظهر لك واضحة بالكامل قبل تأكيد الطلب.',
    enA: 'Village delivery fees typically range between 10 and 20 EGP and are clearly shown before order confirmation.',
  },
  {
    id: 'areas',
    group: 'zones',
    arQ: 'ما هي المناطق التي يغطيها نوى حالياً؟',
    enQ: 'Which Nawa areas and hamlets are covered?',
    arA: 'نغطي قرية نوى بالكامل (المحطة، شارع السوق، الجامع القبلي، داير الناحية، البلد القديم) والعزب المجاورة على الطريق الزراعي.',
    enA: 'All of Nawa village center (Station, Souq St, El-Gamea El-Qebly, Dayer El-Nahia, Old Village) plus nearby hamlets.',
  },
  {
    id: 'delivery-time',
    group: 'zones',
    arQ: 'كم يستغرق وصول الطلب داخل القرية؟',
    enQ: 'How fast does delivery arrive?',
    arA: 'يصل الطلب عادةً خلال 20 إلى 35 دقيقة حسب وقت تجهيز المتجر ومنطقتك، مع متابعة حية لحالة الطلب.',
    enA: 'Most local deliveries arrive within 20 to 35 minutes depending on preparation time, with live tracking.',
  },
  {
    id: 'cancel',
    group: 'cancellations',
    arQ: 'كيف ألغي طلباً أو أتواصل لحل مشكلة؟',
    enQ: 'How do cancellations and refunds work?',
    arA: 'يمكنك إلغاء الطلب مجاناً قبل بدء المتجر في التجهيز. وفي حال أي نقص أو تلف بالطلب، يتولى فريق الدعم تسويته فوراً.',
    enA: 'Orders can be cancelled free before prep begins. If anything arrives damaged or missing, Nawa Support resolves it fast.',
  },
  {
    id: 'join-kyc',
    group: 'cancellations',
    arQ: 'كيف أسجّل متجري أو أنضم ككابتن ومن يشاهد البطاقة؟',
    enQ: 'How do partners apply and who views National ID photos?',
    arA: 'استخدم «انضم كصاحب متجر» أو «انضم ككابتن». صورة بطاقة الرقم القومي مشفرة ومقصورة على مراجع التشغيل فقط ولا تظهر لأي متجر أو عميل.',
    enA: 'Use Join as a Store or Join as a Courier. Courier National ID images are encrypted and visible strictly to operations admins.',
  },
];

@Component({
  selector: 'nawly-help-page',
  standalone: true,
  imports: [RouterLink, NawlyAppShellPublicComponent],
  template: `
    <nawly-app-shell-public
      brandIcon="help_center"
      [brandBadge]="i18n.isRtl() ? 'الدعم والمعلومات' : 'Help & Legal'"
    >
      <div class="max-w-5xl w-full mx-auto px-4 sm:px-6 py-8 sm:py-10 space-y-10">
        <!-- Teal Gradient Support Hero Card -->
        <div class="bg-gradient-to-br from-light-primary to-teal-900 rounded-3xl p-7 sm:p-12 text-white shadow-xl flex flex-col lg:flex-row items-start lg:items-center justify-between gap-8">
          <div class="space-y-4 max-w-xl">
            <span class="inline-flex items-center gap-2 bg-white/15 px-3.5 py-1.5 rounded-full text-label-sm font-label">
              <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
              <span>
                {{
                  i18n.isRtl()
                    ? 'فريق دعم قرية نوى متاح يوميًا من 8 صباحًا حتى 12 منتصف الليل'
                    : 'Nawa Village Support Desk open daily 8:00 AM – 12:00 AM'
                }}
              </span>
            </span>

            <h1 class="text-headline-lg sm:text-headline-xl font-headline font-bold leading-snug">
              {{ i18n.isRtl() ? 'أهلاً بك.. كيف نقدر نساعدك اليوم؟' : 'Hello — how can we help you today?' }}
            </h1>

            <p class="text-body-md text-teal-100 leading-relaxed">
              {{
                i18n.isRtl()
                  ? 'واجهت مشكلة في طلبك؟ تريد الاستفسار عن استعادة كلمة مرور شريك أو الانضمام كمتجر أو كابتن؟ نحن بجانبك خطوة بخطوة.'
                  : 'Need help with an order, partner password recovery, or joining NAWA? Our Nawa Village support team is ready.'
              }}
            </p>
          </div>

          <!-- Instant Local Contact Buttons -->
          <div class="flex flex-col gap-3 w-full lg:w-auto shrink-0">
            <a
              href="https://wa.me/201024567890"
              target="_blank"
              rel="noopener noreferrer"
              class="min-h-[56px] px-6 py-3 bg-success hover:bg-emerald-700 text-white font-label font-bold rounded-xl shadow-md flex items-center justify-center gap-2.5 transition-all"
            >
              <span class="material-symbols-outlined text-2xl" aria-hidden="true">chat</span>
              <span>{{ i18n.isRtl() ? 'محادثة واتساب فورية مع الدعم' : 'Instant WhatsApp Support' }}</span>
            </a>

            <a
              href="tel:+201024567890"
              class="min-h-[56px] px-6 py-3 bg-white hover:bg-light-surface-variant text-light-on-surface font-label font-bold rounded-xl shadow-md flex items-center justify-center gap-2.5 transition-all"
            >
              <span class="material-symbols-outlined text-2xl text-light-primary" aria-hidden="true">call</span>
              <span>{{ i18n.isRtl() ? 'اتصال هاتفي:' : 'Call:' }}</span>
              <span class="font-code font-bold ltr-num" dir="ltr">010 2456 7890</span>
            </a>
          </div>
        </div>

        <!-- FAQ Search Bar -->
        <div class="bg-white rounded-2xl border border-light-outline-variant/80 p-4 sm:p-5 shadow-xs">
          <label for="faq-search" class="sr-only">
            {{ i18n.isRtl() ? 'ابحث في الأسئلة الشائعة' : 'Search FAQs' }}
          </label>
          <div class="relative">
            <span
              class="material-symbols-outlined absolute start-4 top-1/2 -translate-y-1/2 text-light-outline text-xl pointer-events-none"
              aria-hidden="true"
            >
              search
            </span>
            <input
              id="faq-search"
              type="search"
              [value]="query()"
              (input)="onSearch($event)"
              [placeholder]="i18n.isRtl() ? 'ابحث عن سؤال (مثال: كاش، توصيل، بطاقة، إلغاء)...' : 'Search questions (e.g. cash, zones, cancel, ID)...'"
              class="w-full h-12 ps-12 pe-4 bg-light-surface border border-light-outline-variant rounded-xl text-body-md text-light-on-surface focus:outline-none focus:ring-2 focus:ring-light-primary transition-all"
            />
          </div>
        </div>

        <!-- 4 Categorized FAQ Grid -->
        <div class="space-y-6">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <h2 class="text-headline-md font-headline text-light-on-surface">
              {{ i18n.isRtl() ? 'الأسئلة الشائعة لأهالي نوى' : 'Frequently Asked Questions' }}
            </h2>
            @if (query().trim()) {
              <button
                type="button"
                class="text-label-md font-label text-light-primary hover:underline cursor-pointer"
                (click)="query.set('')"
              >
                {{ i18n.isRtl() ? 'عرض جميع الأسئلة' : 'Show all questions' }}
              </button>
            }
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            @for (group of visibleGroups(); track group.key) {
              <div class="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-4">
                <div class="flex items-center gap-2.5 pb-3 border-b border-slate-100">
                  <span class="material-symbols-outlined text-xl text-light-primary shrink-0" aria-hidden="true">{{ group.icon }}</span>
                  <h3 class="font-headline font-bold text-lg text-slate-900">
                    {{ i18n.isRtl() ? group.arTitle : group.enTitle }}
                  </h3>
                </div>

                <div class="space-y-2.5 text-body-sm">
                  @for (item of itemsForGroup(group.key); track item.id) {
                    <div class="rounded-xl border border-light-outline-variant/60 bg-light-surface-variant/15 overflow-hidden">
                      <button
                        type="button"
                        class="w-full p-3.5 text-start flex items-center justify-between gap-2 font-bold text-light-on-surface hover:bg-light-surface-variant/30 transition-colors cursor-pointer"
                        [attr.aria-expanded]="openFaqId() === item.id"
                        (click)="toggleFaq(item.id)"
                      >
                        <span>{{ i18n.isRtl() ? item.arQ : item.enQ }}</span>
                        <span class="material-symbols-outlined text-lg text-light-primary shrink-0" aria-hidden="true">
                          {{ openFaqId() === item.id ? 'expand_less' : 'expand_more' }}
                        </span>
                      </button>

                      @if (openFaqId() === item.id || query().trim().length > 0) {
                        <p class="px-3.5 pb-3.5 text-light-on-surface-variant leading-relaxed">
                          {{ i18n.isRtl() ? item.arA : item.enA }}
                        </p>
                      }
                    </div>
                  }
                </div>
              </div>
            }
          </div>

          @if (visibleGroups().length === 0) {
            <div class="bg-white rounded-2xl border border-light-outline-variant p-8 text-center">
              <p class="text-body-md text-light-on-surface-variant">
                {{
                  i18n.isRtl()
                    ? 'لم نعثر على أسئلة مطابقة لبحثك. يمكنك مراسلتنا مباشرة على واتساب.'
                    : 'No matching questions found. Message us directly on WhatsApp.'
                }}
              </p>
            </div>
          }
        </div>

        <!-- Bottom Cross Navigation -->
        <div class="flex flex-wrap items-center gap-3 pt-2">
          <a
            routerLink="/terms"
            class="min-h-[48px] px-5 py-2.5 rounded-xl border border-light-outline-variant bg-white hover:bg-light-surface-variant/30 font-label font-bold text-sm text-light-on-surface inline-flex items-center gap-2 transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">gavel</span>
            <span>{{ i18n.dict().navTerms }}</span>
          </a>
          <a
            routerLink="/privacy"
            class="min-h-[48px] px-5 py-2.5 rounded-xl border border-light-outline-variant bg-white hover:bg-light-surface-variant/30 font-label font-bold text-sm text-light-on-surface inline-flex items-center gap-2 transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-light-primary" aria-hidden="true">policy</span>
            <span>{{ i18n.dict().navPrivacy }}</span>
          </a>
          <a
            routerLink="/"
            class="min-h-[48px] px-5 py-2.5 rounded-xl text-light-primary hover:bg-light-primary-container/30 font-label font-bold text-sm inline-flex items-center gap-2 transition-colors"
          >
            <span class="material-symbols-outlined text-lg" aria-hidden="true">home</span>
            <span>{{ i18n.isRtl() ? 'العودة للرئيسية' : 'Back home' }}</span>
          </a>
        </div>
      </div>
    </nawly-app-shell-public>
  `,
})
export class HelpPageComponent {
  protected readonly i18n = inject(I18nService);

  protected readonly query = signal('');
  protected readonly openFaqId = signal<string | null>('how-to-order');

  protected readonly filteredFaqs = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return FAQS;
    return FAQS.filter((item) =>
      `${item.arQ} ${item.enQ} ${item.arA} ${item.enA}`.toLowerCase().includes(q),
    );
  });

  protected readonly visibleGroups = computed(() => {
    const items = this.filteredFaqs();
    return FAQ_GROUPS.filter((g) => items.some((i) => i.group === g.key));
  });

  protected itemsForGroup(key: FaqGroupKey): FaqItem[] {
    return this.filteredFaqs().filter((i) => i.group === key);
  }

  protected toggleFaq(id: string): void {
    this.openFaqId.update((curr) => (curr === id ? null : id));
  }

  protected onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }
}
