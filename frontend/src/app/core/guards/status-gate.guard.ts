import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { CourierApplicationService } from '../services/courier-application.service';
import { DiscoveryService } from '../services/discovery.service';

export const storeStatusGateGuard: CanActivateFn = (_route, state) => {
  const discovery = inject(DiscoveryService);
  const router = inject(Router);

  return discovery.getStorePortalSettings().pipe(
    map((store) => {
      if (store.status === 'Active') {
        return true;
      }
      return router.createUrlTree(['/store/status'], {
        queryParams: { status: store.status },
      });
    }),
    catchError(() =>
      of(
        router.createUrlTree(['/store/status'], {
          queryParams: { from: state.url },
        }),
      ),
    ),
  );
};

export const courierStatusGateGuard: CanActivateFn = (_route, state) => {
  const courierService = inject(CourierApplicationService);
  const router = inject(Router);

  return courierService.getMyCourierProfile().pipe(
    map((profile) => {
      if (profile.status === 'Approved') {
        return true;
      }
      return router.createUrlTree(['/courier/status'], {
        queryParams: { status: profile.status },
      });
    }),
    catchError(() =>
      of(
        router.createUrlTree(['/courier/status'], {
          queryParams: { from: state.url },
        }),
      ),
    ),
  );
};
