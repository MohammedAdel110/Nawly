import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { I18nService } from '../i18n/i18n.service';
import { AuthService } from '../services/auth.service';

const PUBLIC_AUTH_URLS = [
  '/api/v1/auth/otp/request',
  '/api/v1/auth/otp/verify',
  '/api/v1/auth/login/password',
  '/api/v1/auth/mfa/verify',
  '/api/v1/auth/token/refresh',
  '/api/v1/auth/logout',
];

function isPublicAuthRequest(url: string): boolean {
  return PUBLIC_AUTH_URLS.some((prefix) => url.includes(prefix));
}

function attachHeaders(
  req: HttpRequest<unknown>,
  token: string | null,
  lang: 'ar' | 'en',
): HttpRequest<unknown> {
  let headers = req.headers.set('Accept-Language', lang === 'ar' ? 'ar-EG,ar;q=0.9,en;q=0.8' : 'en-US,en;q=0.9');
  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }
  return req.clone({ headers });
}

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const i18n = inject(I18nService);
  const router = inject(Router);

  const isAuthEndpoint = isPublicAuthRequest(req.url);
  const initialToken = isAuthEndpoint ? null : auth.getAccessToken();
  const outgoing = attachHeaders(req, initialToken, i18n.locale());

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 401 && !isAuthEndpoint && auth.session()?.refreshToken) {
        return auth.refreshToken().pipe(
          switchMap((newTokens) => {
            const retried = attachHeaders(req, newTokens.accessToken, i18n.locale());
            return next(retried);
          }),
          catchError((refreshErr: unknown) => {
            auth.handleSessionExpired(router.url || '/');
            return throwError(() => refreshErr);
          }),
        );
      }

      if (error.status === 401 && !isAuthEndpoint && auth.isAuthenticated()) {
        auth.handleSessionExpired(router.url || '/');
      }

      return throwError(() => error);
    }),
  );
};
