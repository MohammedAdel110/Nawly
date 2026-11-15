import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';
import {
  ApplyStoreDto,
  AreaDto,
  DeliveryZoneDto,
  PaginationResponse,
  StoreCategoryDto,
  StoreDetailsDto,
  StoreSummaryDto,
} from '../models/api.models';

const FALLBACK_CATEGORIES: StoreCategoryDto[] = [
  {
    id: '33333333-3333-3333-3333-333333330001',
    name: 'Grocery & Supermarket',
    nameAr: 'سوبر ماركت وبقالة',
    iconRef: 'grocery',
    sortOrder: 1,
    isActive: true,
  },
  {
    id: '33333333-3333-3333-3333-333333330002',
    name: 'Restaurants & Grills',
    nameAr: 'مطاعم ومشويات',
    iconRef: 'restaurant',
    sortOrder: 2,
    isActive: true,
  },
  {
    id: '33333333-3333-3333-3333-333333330003',
    name: 'Bakery & Pastry',
    nameAr: 'أفران ومخبوزات',
    iconRef: 'bakery',
    sortOrder: 3,
    isActive: true,
  },
  {
    id: '33333333-3333-3333-3333-333333330004',
    name: 'Pharmacy',
    nameAr: 'صيدلية ومستلزمات طبية',
    iconRef: 'pharmacy',
    sortOrder: 4,
    isActive: true,
  },
  {
    id: '33333333-3333-3333-3333-333333330005',
    name: 'Vegetables, Fruit & Butcher',
    nameAr: 'خضار وفاكهة وجزارة',
    iconRef: 'produce',
    sortOrder: 5,
    isActive: true,
  },
];

const FALLBACK_ZONES: DeliveryZoneDto[] = [
  {
    id: '11111111-1111-1111-1111-111111110001',
    name: 'Nawa Village Center',
    nameAr: 'نوى — قلب القرية',
    baseFee: 10,
    perKmFee: 2,
    minOrderAmount: 30,
    freeDeliveryThreshold: null,
    estimatedTravelMinutes: 20,
    isActive: true,
  },
  {
    id: '11111111-1111-1111-1111-111111110002',
    name: 'Nawa Outskirts & Hamlets',
    nameAr: 'نوى — الأطراف والعزب',
    baseFee: 15,
    perKmFee: 2,
    minOrderAmount: 40,
    freeDeliveryThreshold: null,
    estimatedTravelMinutes: 30,
    isActive: true,
  },
];

const FALLBACK_AREAS: AreaDto[] = [
  {
    id: '22222222-2222-2222-2222-222222220001',
    zoneId: '11111111-1111-1111-1111-111111110001',
    zoneName: 'Nawa Village Center',
    zoneNameAr: 'نوى — قلب القرية',
    name: 'El-Mahatta & Market Street',
    nameAr: 'المحطة وشارع السوق',
    sortOrder: 1,
    isActive: true,
  },
  {
    id: '22222222-2222-2222-2222-222222220002',
    zoneId: '11111111-1111-1111-1111-111111110001',
    zoneName: 'Nawa Village Center',
    zoneNameAr: 'نوى — قلب القرية',
    name: 'El-Gamaa El-Kebir & Old Mosque',
    nameAr: 'الجامع الكبير والبلد القديم',
    sortOrder: 2,
    isActive: true,
  },
  {
    id: '22222222-2222-2222-2222-222222220003',
    zoneId: '11111111-1111-1111-1111-111111110002',
    zoneName: 'Nawa Outskirts & Hamlets',
    zoneNameAr: 'نوى — الأطراف والعزب',
    name: 'Ezbet El-Bahr & Agricultural Road',
    nameAr: 'عزبة البحر والطريق الزراعي',
    sortOrder: 3,
    isActive: true,
  },
];

@Injectable({ providedIn: 'root' })
export class DiscoveryService {
  private readonly http = inject(HttpClient);
  readonly usingOfflineReference = signal(false);

  getCategories(): Observable<StoreCategoryDto[]> {
    return this.http.get<StoreCategoryDto[]>('/api/v1/discovery/categories').pipe(
      tap(() => this.usingOfflineReference.set(false)),
      catchError((err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 0) {
          this.usingOfflineReference.set(true);
          return of(FALLBACK_CATEGORIES);
        }
        return throwError(() => err);
      }),
    );
  }

  getZones(): Observable<DeliveryZoneDto[]> {
    return this.http.get<DeliveryZoneDto[]>('/api/v1/discovery/zones').pipe(
      tap(() => this.usingOfflineReference.set(false)),
      catchError((err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 0) {
          this.usingOfflineReference.set(true);
          return of(FALLBACK_ZONES);
        }
        return throwError(() => err);
      }),
    );
  }

  getAreas(): Observable<AreaDto[]> {
    return this.http.get<AreaDto[]>('/api/v1/discovery/areas').pipe(
      tap(() => this.usingOfflineReference.set(false)),
      catchError((err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === 0) {
          this.usingOfflineReference.set(true);
          return of(FALLBACK_AREAS);
        }
        return throwError(() => err);
      }),
    );
  }

  getStores(filters?: {
    categoryId?: string | null;
    areaId?: string | null;
    search?: string | null;
    openNow?: boolean | null;
    pageSize?: number;
  }): Observable<StoreSummaryDto[]> {
    let params = new HttpParams().set('pageIndex', '1').set('pageSize', String(filters?.pageSize ?? 24));
    if (filters?.categoryId) {
      params = params.set('categoryId', filters.categoryId);
    }
    if (filters?.areaId) {
      params = params.set('areaId', filters.areaId);
    }
    if (filters?.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }
    if (filters?.openNow !== null && filters?.openNow !== undefined) {
      params = params.set('openNow', String(filters.openNow));
    }

    return this.http
      .get<PaginationResponse<StoreSummaryDto> | StoreSummaryDto[]>('/api/v1/discovery/stores', {
        params,
      })
      .pipe(
        tap(() => this.usingOfflineReference.set(false)),
        map((res) => (Array.isArray(res) ? res : (res?.data ?? []))),
        catchError((err: unknown) => {
          if (err instanceof HttpErrorResponse && err.status === 0) {
            this.usingOfflineReference.set(true);
            return of([]);
          }
          return throwError(() => err);
        }),
      );
  }

  applyStore(dto: ApplyStoreDto): Observable<StoreDetailsDto> {
    return this.http.post<StoreDetailsDto>('/api/v1/discovery/stores/apply', dto);
  }

  getStorePortalSettings(storeId?: string | null): Observable<StoreDetailsDto> {
    let params = new HttpParams();
    if (storeId) {
      params = params.set('storeId', storeId);
    }
    return this.http.get<StoreDetailsDto>('/api/v1/store-portal/settings', { params });
  }
}
