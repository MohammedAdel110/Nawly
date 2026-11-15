import { Routes } from '@angular/router';
import { storeStatusGateGuard } from '../../core/guards/status-gate.guard';
import {
  NawlyRoutePlaceholderComponent,
  RoutePlaceholderData,
} from '../../shared/components/route-placeholder/nawly-route-placeholder.component';
import { StoreStatusGateComponent } from './pages/store-status-gate/store-status-gate.component';

function storePage(pageId: string, titleAr: string, titleEn: string): RoutePlaceholderData {
  return {
    pageId,
    titleAr,
    titleEn,
    areaAr: 'بوابة المتجر',
    areaEn: 'Store Portal',
    phase: 'Phase 4',
  };
}

export const STORE_ROUTES: Routes = [
  {
    path: 'status',
    component: StoreStatusGateComponent,
  },
  {
    path: '',
    canActivate: [storeStatusGateGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-02', 'لوحة متابعة المتجر', 'Store Dashboard'),
      },
      {
        path: 'orders',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-03', 'طابور الطلبات المباشر', 'Live Order Queue'),
      },
      {
        path: 'orders/history',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-05', 'سجل طلبات المتجر', 'Store Order History'),
      },
      {
        path: 'orders/:id',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-04', 'تفاصيل طلب المتجر', 'Store Order Detail'),
      },
      {
        path: 'menu',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-06', 'إدارة القائمة والمنتجات', 'Menu & Catalog Manager'),
      },
      {
        path: 'menu/products/new',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-07', 'إضافة منتج جديد', 'Create Product'),
      },
      {
        path: 'menu/products/:id/options',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-08', 'مجموعات إضافات المنتج', 'Product Option Groups'),
      },
      {
        path: 'menu/products/:id',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-07', 'تعديل المنتج', 'Edit Product'),
      },
      {
        path: 'hours',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-09', 'مواعيد عمل المتجر', 'Operating Hours'),
      },
      {
        path: 'settings',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-10', 'إعدادات المتجر والشعار والمناطق', 'Store Profile, Zones & Images'),
      },
      {
        path: 'staff',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-11', 'موظفو المتجر والصلاحيات', 'Store Staff & Access'),
      },
      {
        path: 'reviews',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-12', 'تقييمات العملاء للمتجر', 'Store Reviews'),
      },
      {
        path: 'finance',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-13', 'المالية وكشف الحساب', 'Store Finance & Ledger'),
      },
      {
        path: 'finance/settlements/:id',
        component: NawlyRoutePlaceholderComponent,
        data: storePage('S-14', 'تفاصيل التسوية المالية', 'Settlement Detail'),
      },
    ],
  },
];
