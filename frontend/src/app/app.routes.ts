import { Routes } from '@angular/router';
import { guestOnlyGuard, unsavedChangesGuard } from './core/guards/auth.guard';
import { adminRoleGuard, courierRoleGuard, storeRoleGuard } from './core/guards/role.guard';
import { CustomerSigninComponent } from './features/public/pages/customer-signin/customer-signin.component';
import { HelpPageComponent } from './features/public/pages/help-page/help-page.component';
import { JoinCourierComponent } from './features/public/pages/join-courier/join-courier.component';
import { JoinStoreComponent } from './features/public/pages/join-store/join-store.component';
import { LegalPageComponent } from './features/public/pages/legal-page/legal-page.component';
import { StaffSigninComponent } from './features/public/pages/staff-signin/staff-signin.component';
import { SystemStatusPageComponent } from './features/public/pages/system-status-page/system-status-page.component';

export const routes: Routes = [
  // A-01: Customer Sign-in / Register
  {
    path: 'signin',
    canActivate: [guestOnlyGuard],
    component: CustomerSigninComponent,
    title: 'دخول العملاء — نوى | NAWA Customer Sign in',
  },
  // A-02: Store, Courier & Admin Password + MFA Sign-in
  {
    path: 'staff/signin',
    canActivate: [guestOnlyGuard],
    component: StaffSigninComponent,
    title: 'بوابة الشركاء والإدارة — نوى | NAWA Partner Sign in',
  },
  // A-03: Join as a Store (4-step wizard)
  {
    path: 'join/store',
    component: JoinStoreComponent,
    canDeactivate: [unsavedChangesGuard],
    title: 'سجّل محلك — نوى | Join NAWA as a Store',
  },
  // A-04: Join as a Courier
  {
    path: 'join/courier',
    component: JoinCourierComponent,
    title: 'انضم كمندوب توصيل — نوى | Join NAWA as a Courier',
  },
  // A-05: Legal, Help & System Pages
  {
    path: 'terms',
    component: LegalPageComponent,
    data: { legalType: 'terms' },
    title: 'الشروط والأحكام — نوى | NAWA Terms of Service',
  },
  {
    path: 'privacy',
    component: LegalPageComponent,
    data: { legalType: 'privacy' },
    title: 'سياسة الخصوصية — نوى | NAWA Privacy Policy',
  },
  {
    path: 'help',
    component: HelpPageComponent,
    title: 'المساعدة والدعم — نوى | NAWA Help & Support',
  },
  {
    path: '404',
    component: SystemStatusPageComponent,
    data: { kind: 'not-found' },
    title: 'الصفحة غير موجودة — نوى | NAWA',
  },
  {
    path: 'forbidden',
    component: SystemStatusPageComponent,
    data: { kind: 'forbidden' },
    title: 'غير مصرح — نوى | NAWA',
  },
  {
    path: 'error',
    component: SystemStatusPageComponent,
    data: { kind: 'error' },
    title: 'خطأ في النظام — نوى | NAWA',
  },
  {
    path: 'offline',
    component: SystemStatusPageComponent,
    data: { kind: 'offline' },
    title: 'لا يوجد اتصال — نوى | NAWA',
  },
  {
    path: 'maintenance',
    component: SystemStatusPageComponent,
    data: { kind: 'maintenance' },
    title: 'صيانة النظام — نوى | NAWA',
  },

  // Lazy-loaded Role Portals (S-01..S-14, K-01..K-07, M-01..M-20)
  {
    path: 'store',
    canActivate: [storeRoleGuard],
    loadChildren: () => import('./features/store/store.routes').then((m) => m.STORE_ROUTES),
  },
  {
    path: 'courier',
    canActivate: [courierRoleGuard],
    loadChildren: () => import('./features/courier/courier.routes').then((m) => m.COURIER_ROUTES),
  },
  {
    path: 'admin',
    canActivate: [adminRoleGuard()],
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },

  // Lazy-loaded Customer App (C-01..C-20, root discovery + protected checkout/account)
  {
    path: '',
    loadChildren: () => import('./features/customer/customer.routes').then((m) => m.CUSTOMER_ROUTES),
  },

  // Catch-all -> 404
  {
    path: '**',
    component: SystemStatusPageComponent,
    data: { kind: 'not-found' },
  },
];
