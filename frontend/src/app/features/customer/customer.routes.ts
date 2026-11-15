import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { customerGuard } from '../../core/guards/role.guard';
import {
  NawlyRoutePlaceholderComponent,
  RoutePlaceholderData,
} from '../../shared/components/route-placeholder/nawly-route-placeholder.component';

function customerPage(
  pageId: string,
  titleAr: string,
  titleEn: string,
  phase = 'Phase 2–3',
): RoutePlaceholderData {
  return {
    pageId,
    titleAr,
    titleEn,
    areaAr: 'تطبيق العملاء',
    areaEn: 'Customer App',
    phase,
  };
}

export const CUSTOMER_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-01', 'الصفحة الرئيسية — سوق نوى', 'Home — Nawa Marketplace', 'Phase 2'),
  },
  {
    path: 'area',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-02', 'اختيار المنطقة والتوصيل في نوى', 'Delivery Area Selector', 'Phase 2'),
  },
  {
    path: 'stores',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-03', 'دليل المحلات والتصنيفات', 'Store Directory & Filters', 'Phase 2'),
  },
  {
    path: 'search',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-04', 'البحث الموحد في المحلات والمنتجات', 'Unified Store & Product Search', 'Phase 2'),
  },
  {
    path: 'stores/:slug',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-05', 'صفحة المتجر وقائمة المنتجات', 'Store Profile & Menu', 'Phase 2'),
  },
  {
    path: 'cart',
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-07', 'سلة المشتريات', 'Shopping Cart', 'Phase 2'),
  },
  {
    path: 'checkout',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-08', 'إتمام الطلب والدفع', 'Checkout', 'Phase 3'),
  },
  {
    path: 'orders',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-11', 'سجل طلباتي', 'Order History', 'Phase 3'),
  },
  {
    path: 'orders/:id/payment',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-09', 'الدفع الإلكتروني وإعادة المحاولة', 'Online Payment & Retry', 'Phase 3'),
  },
  {
    path: 'orders/:id/review',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-12', 'تقييم الطلب والمتجر', 'Rate Order & Review', 'Phase 3'),
  },
  {
    path: 'orders/:id',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-10', 'تأكيد الطلب والتتبع المباشر', 'Order Confirmation & Live Tracking', 'Phase 3'),
  },
  {
    path: 'account/addresses',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-13', 'العناوين المحفوظة', 'Saved Addresses', 'Phase 3'),
  },
  {
    path: 'account/favorites',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-14', 'المتاجر المفضلة', 'Favorite Stores', 'Phase 3'),
  },
  {
    path: 'account/profile',
    canActivate: [authGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-15', 'الحساب والملف الشخصي', 'Account & Profile', 'Phase 3'),
  },
  {
    path: 'account/security',
    canActivate: [authGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-16', 'الأمان والمصادقة الثنائية والجلسات', 'Security, MFA & Sessions', 'Phase 3'),
  },
  {
    path: 'notifications',
    canActivate: [authGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-17', 'مركز الإشعارات', 'Notifications Center', 'Phase 3'),
  },
  {
    path: 'support',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-18', 'تذاكر الدعم الفني', 'My Support Tickets', 'Phase 3'),
  },
  {
    path: 'support/new',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-19', 'فتح تذكرة دعم جديدة', 'New Support Ticket', 'Phase 3'),
  },
  {
    path: 'support/:id',
    canActivate: [customerGuard],
    component: NawlyRoutePlaceholderComponent,
    data: customerPage('C-20', 'محادثة تذكرة الدعم', 'Support Ticket Thread', 'Phase 3'),
  },
];
