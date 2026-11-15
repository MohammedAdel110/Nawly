import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export interface CanComponentDeactivate {
  canDeactivate: () => boolean | Promise<boolean>;
}

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  const isStaffTarget =
    state.url.startsWith('/store') ||
    state.url.startsWith('/courier') ||
    state.url.startsWith('/admin');

  return router.createUrlTree([isStaffTarget ? '/staff/signin' : '/signin'], {
    queryParams: { returnUrl: state.url },
  });
};

export const guestOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree([auth.resolveHomeRoute()]);
};

export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  if (typeof component?.canDeactivate !== 'function') {
    return true;
  }
  return component.canDeactivate();
};
