import { UiLocale } from '../models/api.models';

export interface TranslationDictionary {
  brandName: string;
  brandTagline: string;
  villageLocation: string;
  skipToContent: string;
  navBrowseStores: string;
  navHelp: string;
  navCustomerSignin: string;
  navStaffSignin: string;
  navJoinStore: string;
  navJoinCourier: string;
  navTerms: string;
  navPrivacy: string;
  navMyPortal: string;
  navSignOut: string;
  langSwitchTo: string;
  currencyUnit: string;
  loading: string;
  requiredBadge: string;
  optionalBadge: string;
  retry: string;
  back: string;
  next: string;
  submit: string;
  cancel: string;
  close: string;
  copy: string;
  copied: string;
  phoneLabel: string;
  phoneHint: string;
  phoneErrorRequired: string;
  phoneErrorInvalid: string;
  passwordShow: string;
  passwordHide: string;
  passwordHint: string;
  passwordErrorRequired: string;
  passwordErrorLength: string;
  otpResendIn: string;
  otpResendNow: string;
  uploadChooseOrCamera: string;
  uploadTakePhoto: string;
  uploadRetake: string;
  uploadRemove: string;
  uploadHintDefault: string;
  uploadErrorType: string;
  uploadErrorSize: string;
  uploadErrorCorrupt: string;
  footerRights: string;
  footerCommunityNote: string;
  footerPartnerHeading: string;
  footerLegalHeading: string;
  footerCustomerHeading: string;
  errGeneric: string;
  errNetwork: string;
  errUnauthorized: string;
  errForbidden: string;
  errNotFound: string;
  errConflict: string;
  errRateLimited: string;
  errServer: string;
  errTraceLabel: string;
  offlineCatalogBanner: string;
}

export const TRANSLATIONS: Record<UiLocale, TranslationDictionary> = {
  ar: {
    brandName: 'نوى',
    brandTagline: 'كل طلباتك لحد عندك',
    villageLocation: 'كل طلباتك لحد عندك — قرية نوى، القليوبية',
    skipToContent: 'تخطي إلى المحتوى الرئيسي',
    navBrowseStores: 'تصفح المحلات',
    navHelp: 'المساعدة والدعم',
    navCustomerSignin: 'دخول العملاء',
    navStaffSignin: 'بوابة الشركاء والإدارة',
    navJoinStore: 'سجّل محلك',
    navJoinCourier: 'انضم كمندوب توصيل',
    navTerms: 'الشروط والأحكام',
    navPrivacy: 'سياسة الخصوصية',
    navMyPortal: 'لوحتي',
    navSignOut: 'تسجيل الخروج',
    langSwitchTo: 'English',
    currencyUnit: 'ج.م',
    loading: 'جاري التحميل...',
    requiredBadge: 'إجباري',
    optionalBadge: 'اختياري',
    retry: 'أعد المحاولة',
    back: 'السابق',
    next: 'التالي',
    submit: 'إرسال الطلب',
    cancel: 'إلغاء',
    close: 'إغلاق',
    copy: 'نسخ',
    copied: 'تم النسخ',
    phoneLabel: 'رقم الموبايل المصري',
    phoneHint: 'اكتب ١١ رقم يبدأ بـ 010 أو 011 أو 012 أو 015 (مثال: 01012345678)',
    phoneErrorRequired: 'يرجى إدخال رقم الموبايل.',
    phoneErrorInvalid: 'اكتب رقم موبايل مصري صحيح مكوّن من ١١ رقم يبدأ بـ 010 أو 011 أو 012 أو 015.',
    passwordShow: 'إظهار كلمة المرور',
    passwordHide: 'إخفاء كلمة المرور',
    passwordHint: 'من 8 إلى 128 حرف أو رقم',
    passwordErrorRequired: 'يرجى إدخال كلمة المرور.',
    passwordErrorLength: 'كلمة المرور يجب ألا تقل عن 8 أحرف.',
    otpResendIn: 'إعادة إرسال الكود بعد',
    otpResendNow: 'أرسل كود جديد',
    uploadChooseOrCamera: 'اختر صورة أو افتح الكاميرا',
    uploadTakePhoto: 'التقط صورة بالكاميرا',
    uploadRetake: 'تغيير الصورة',
    uploadRemove: 'حذف',
    uploadHintDefault: 'صور JPG أو PNG أو WebP بحد أقصى 5 ميجابايت',
    uploadErrorType: 'صيغة الملف غير مدعومة. اختر صورة JPG أو PNG أو WebP فقط.',
    uploadErrorSize: 'حجم الصورة يتجاوز 5 ميجابايت. اختر صورة أصغر أو صورها بجودة أقل.',
    uploadErrorCorrupt: 'تعذّر قراءة الصورة. تأكد أنها ملف صورة سليم وأعد المحاولة.',
    footerRights: 'نوى (NAWA) للتوصيل المحلي — كل طلباتك لحد عندك.',
    footerCommunityNote: 'مصمم لخدمة أهالي وتجار ومناديب قرية نوى بسهولة ووضوح.',
    footerPartnerHeading: 'شركاء نوى',
    footerLegalHeading: 'السياسات والدعم',
    footerCustomerHeading: 'للعملاء',
    errGeneric: 'حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.',
    errNetwork: 'تعذّر الاتصال بالخادم الآن. تأكد من اتصال الإنترنت وأعد المحاولة.',
    errUnauthorized: 'انتهت جلستك أو بيانات الدخول غير صحيحة.',
    errForbidden: 'ليس لديك صلاحية للوصول إلى هذه الصفحة.',
    errNotFound: 'البيانات أو الصفحة المطلوبة غير موجودة.',
    errConflict: 'البيانات المدخلة مسجلة مسبقاً.',
    errRateLimited: 'محاولات كثيرة متتالية. انتظر قليلاً ثم حاول مجدداً.',
    errServer: 'حدث عطل مؤقت في الخادم. نعمل على حله.',
    errTraceLabel: 'رقم المرجع للدعم',
    offlineCatalogBanner: 'تعذّر الوصول للخادم حالياً — يتم عرض بيانات نوى المرجعية للمعاينة.',
  },
  en: {
    brandName: 'NAWA',
    brandTagline: 'All your orders right to your door',
    villageLocation: 'All your orders to your door — Nawa Village',
    skipToContent: 'Skip to main content',
    navBrowseStores: 'Browse stores',
    navHelp: 'Help & Support',
    navCustomerSignin: 'Sign in',
    navStaffSignin: 'Partner & Staff Login',
    navJoinStore: 'Join as a Store',
    navJoinCourier: 'Join as a Courier',
    navTerms: 'Terms of Service',
    navPrivacy: 'Privacy Policy',
    navMyPortal: 'My Portal',
    navSignOut: 'Sign out',
    langSwitchTo: 'العربية',
    currencyUnit: 'EGP',
    loading: 'Loading...',
    requiredBadge: 'Required',
    optionalBadge: 'Optional',
    retry: 'Try again',
    back: 'Back',
    next: 'Next',
    submit: 'Submit application',
    cancel: 'Cancel',
    close: 'Close',
    copy: 'Copy',
    copied: 'Copied',
    phoneLabel: 'Egyptian mobile number',
    phoneHint: 'Enter 11 digits starting with 010, 011, 012, or 015 (e.g., 01012345678)',
    phoneErrorRequired: 'Please enter a mobile number.',
    phoneErrorInvalid: 'Enter a valid 11-digit Egyptian mobile starting with 010, 011, 012, or 015.',
    passwordShow: 'Show password',
    passwordHide: 'Hide password',
    passwordHint: '8 to 128 characters',
    passwordErrorRequired: 'Please enter a password.',
    passwordErrorLength: 'Password must be at least 8 characters.',
    otpResendIn: 'Resend code in',
    otpResendNow: 'Resend code',
    uploadChooseOrCamera: 'Choose photo or camera',
    uploadTakePhoto: 'Take photo',
    uploadRetake: 'Retake photo',
    uploadRemove: 'Remove',
    uploadHintDefault: 'JPG, PNG, or WebP up to 5 MB',
    uploadErrorType: 'Unsupported file format. Please choose a JPG, PNG, or WebP image.',
    uploadErrorSize: 'File exceeds 5 MB. Please upload a smaller image.',
    uploadErrorCorrupt: 'Could not verify the image file. Please select a valid photo.',
    footerRights: 'NAWA (نوى) local marketplace & delivery — all your orders to your door.',
    footerCommunityNote: 'Built for everyday clarity on any phone for Nawa neighbors, shops, and couriers.',
    footerPartnerHeading: 'Partner with NAWA',
    footerLegalHeading: 'Help & Policies',
    footerCustomerHeading: 'Customers',
    errGeneric: 'Something went wrong. Please try again.',
    errNetwork: 'Unable to reach the server right now. Check your connection and try again.',
    errUnauthorized: 'Your session expired or the credentials are incorrect.',
    errForbidden: 'You do not have permission to view this page.',
    errNotFound: 'The requested item or page was not found.',
    errConflict: 'An account with these details already exists.',
    errRateLimited: 'Too many attempts in a short time. Please wait before trying again.',
    errServer: 'A temporary server error occurred. Please try again soon.',
    errTraceLabel: 'Support reference ID',
    offlineCatalogBanner: 'Server unreachable — showing Nawa reference zones & categories for preview.',
  },
};
