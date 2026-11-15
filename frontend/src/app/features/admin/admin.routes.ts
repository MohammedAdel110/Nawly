import { Routes } from '@angular/router';
import { adminRoleGuard } from '../../core/guards/role.guard';
import {
  NawlyRoutePlaceholderComponent,
  RoutePlaceholderData,
} from '../../shared/components/route-placeholder/nawly-route-placeholder.component';

function adminPage(pageId: string, titleAr: string, titleEn: string): RoutePlaceholderData {
  return {
    pageId,
    titleAr,
    titleEn,
    areaAr: 'لوحة الإدارة والعمليات',
    areaEn: 'Admin & Operations Console',
    phase: 'Phase 5',
  };
}

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminFinance', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-01', 'نظرة عامة على التشغيل', 'Executive Operations Overview'),
  },
  {
    path: 'orders',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-02', 'سجل جميع الطلبات', 'Global Orders Console'),
  },
  {
    path: 'orders/:id',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-03', 'تفاصيل الطلب والتدخل الإداري', 'Admin Order Detail & Overrides'),
  },
  {
    path: 'dispatch',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-04', 'مراقبة التوصيل والموجات الحية', 'Live Dispatch & Wave Monitor'),
  },
  {
    path: 'stores',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-05', 'اعتماد وإدارة المتاجر', 'Store Approvals & Directory'),
  },
  {
    path: 'couriers',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-06', 'اعتماد وإدارة المناديب والحد النقدي', 'Courier Approvals & Cash Caps'),
  },
  {
    path: 'users',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-07', 'دليل المستخدمين والصلاحيات', 'Customer & Staff Directory'),
  },
  {
    path: 'categories',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-08', 'تصنيفات المحلات', 'Store Categories'),
  },
  {
    path: 'zones',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-09', 'نطاقات التوصيل ومناطق نوى', 'Delivery Zones & Village Areas'),
  },
  {
    path: 'coupons',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-10', 'الكوبونات والعروض الترويجية', 'Coupons & Promotions'),
  },
  {
    path: 'banners',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-11', 'إدارة البانرات الرئيسية', 'Home Banners'),
  },
  {
    path: 'payments',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminFinance'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-12', 'معاملات الدفع والمبالغ المستردة', 'Payment Transactions & Refunds'),
  },
  {
    path: 'finance/settlements',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminFinance'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-13', 'تسويات المتاجر', 'Store Settlements'),
  },
  {
    path: 'finance/courier-cash',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminFinance'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-14', 'مطابقة تحصيل المناديب النقدي', 'Courier COD Reconciliation'),
  },
  {
    path: 'finance/ledger',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminFinance'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-15', 'مستكشف القيود المالية', 'Ledger Explorer & Journal'),
  },
  {
    path: 'reviews',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-16', 'مراجعة وإدارة التقييمات', 'Review Moderation'),
  },
  {
    path: 'support',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations', 'AdminSupport'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-17', 'طابور تذاكر الدعم الفني', 'Support Ticket Queue'),
  },
  {
    path: 'broadcast',
    canActivate: [adminRoleGuard(['AdminSuper', 'AdminOperations'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-18', 'الإشعارات الجماعية والتنبيهات', 'Announcements & Push Broadcast'),
  },
  {
    path: 'settings',
    canActivate: [adminRoleGuard(['AdminSuper'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-19', 'إعدادات النظام والمميزات', 'System & Feature Settings'),
  },
  {
    path: 'audit',
    canActivate: [adminRoleGuard(['AdminSuper'])],
    component: NawlyRoutePlaceholderComponent,
    data: adminPage('M-20', 'سجل التدقيق الأمني', 'Audit Log Viewer'),
  },
];
