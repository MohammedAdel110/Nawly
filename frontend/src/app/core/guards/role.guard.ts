import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ADMIN_ROLES, RoleName, STORE_ROLES } from '../models/api.models';
import { AuthService } from '../services/auth.service';

function requireAnyRole(allowedRoles: readonly RoleName[], loginPath: string): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree([loginPath], {
        queryParams: { returnUrl: state.url },
      });
    }

    if (auth.hasAnyRole(allowedRoles)) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
}

export const customerGuard: CanActivateFn = requireAnyRole(['Customer'], '/signin');
export const storeRoleGuard: CanActivateFn = requireAnyRole(STORE_ROLES, '/staff/signin');
export const courierRoleGuard: CanActivateFn = requireAnyRole(['Courier'], '/staff/signin');

export const adminRoleGuard = (subRoles: readonly RoleName[] = ADMIN_ROLES): CanActivateFn =>
  requireAnyRole(subRoles, '/staff/signin');
