import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Language, UiLocale } from '../models/api.models';
import { TRANSLATIONS, TranslationDictionary } from './translations';

const STORAGE_KEY = 'nawly.locale';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly document = inject(DOCUMENT);

  private readonly localeSignal = signal<UiLocale>(this.readInitialLocale());

  readonly locale = this.localeSignal.asReadonly();
  readonly isRtl = computed(() => this.localeSignal() === 'ar');
  readonly dir = computed<'rtl' | 'ltr'>(() => (this.localeSignal() === 'ar' ? 'rtl' : 'ltr'));
  readonly apiLanguage = computed<Language>(() => (this.localeSignal() === 'ar' ? 'Ar' : 'En'));
  readonly dict = computed<TranslationDictionary>(() => TRANSLATIONS[this.localeSignal()]);

  constructor() {
    effect(() => {
      const current = this.localeSignal();
      const htmlEl = this.document.documentElement;
      htmlEl.lang = current;
      htmlEl.dir = current === 'ar' ? 'rtl' : 'ltr';
    });
  }

  setLocale(locale: UiLocale): void {
    this.localeSignal.set(locale);
    this.persistLocale(locale);
  }

  setFromApiLanguage(language: Language | null | undefined): void {
    if (!language) return;
    this.setLocale(language === 'En' ? 'en' : 'ar');
  }

  toggleLocale(): void {
    this.setLocale(this.localeSignal() === 'ar' ? 'en' : 'ar');
  }

  pick(arText: string | null | undefined, enText?: string | null | undefined): string {
    if (this.localeSignal() === 'ar') {
      return (arText?.trim() || enText?.trim() || '');
    }
    return (enText?.trim() || arText?.trim() || '');
  }

  /**
   * Formats money cleanly in Western digits (0-9) per brief Section 2 & Section 4.
   * Examples: "25 ج.م" (Arabic), "EGP 25" (English), "42.50 ج.م" when fractional.
   */
  formatEgp(amount: number | null | undefined): string {
    const safeAmount = typeof amount === 'number' && Number.isFinite(amount) ? amount : 0;
    const hasFraction = Math.abs(safeAmount % 1) > 0.001;
    const formatted = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: hasFraction ? 2 : 0,
      maximumFractionDigits: 2,
    }).format(safeAmount);

    return this.localeSignal() === 'ar' ? `${formatted} ج.م` : `EGP ${formatted}`;
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }

  private readInitialLocale(): UiLocale {
    try {
      const saved = globalThis.localStorage?.getItem(STORAGE_KEY);
      if (saved === 'en' || saved === 'ar') {
        return saved;
      }
    } catch {
      // Storage blocked in private contexts; default to Arabic-first
    }
    return 'ar';
  }

  private persistLocale(locale: UiLocale): void {
    try {
      globalThis.localStorage?.setItem(STORAGE_KEY, locale);
    } catch {
      // Ignore storage quota / privacy mode errors
    }
  }
}
