import { Routes } from '@angular/router';
import { courierStatusGateGuard } from '../../core/guards/status-gate.guard';
import {
  NawlyRoutePlaceholderComponent,
  RoutePlaceholderData,
} from '../../shared/components/route-placeholder/nawly-route-placeholder.component';
import { CourierStatusGateComponent } from './pages/courier-status-gate/courier-status-gate.component';

function courierPage(pageId: string, titleAr: string, titleEn: string): RoutePlaceholderData {
  return {
    pageId,
    titleAr,
    titleEn,
    areaAr: 'بوابة المندوب',
    areaEn: 'Courier Portal',
    phase: 'Phase 4',
  };
}

export const COURIER_ROUTES: Routes = [
  {
    path: 'status',
    component: CourierStatusGateComponent,
  },
  {
    path: '',
    canActivate: [courierStatusGateGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        component: NawlyRoutePlaceholderComponent,
        data: courierPage('K-02', 'لوحة الكابتن واستقبال الطلبات', 'Courier Home & Duty Board'),
      },
      {
        path: 'deliveries/:id',
        component: NawlyRoutePlaceholderComponent,
        data: courierPage('K-04', 'خطوات تسليم الطلب', 'Active Delivery Stepper'),
      },
      {
        path: 'earnings',
        component: NawlyRoutePlaceholderComponent,
        data: courierPage('K-06', 'الأرباح والتحصيل النقدي والتسويات', 'Earnings, COD Cash & Settlements'),
      },
      {
        path: 'profile',
        component: NawlyRoutePlaceholderComponent,
        data: courierPage('K-07', 'ملف المندوب والمركبة', 'Courier Profile & Vehicle'),
      },
    ],
  },
];
