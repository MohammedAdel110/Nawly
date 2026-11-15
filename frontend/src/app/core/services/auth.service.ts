import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import {
  ADMIN_ROLES,
  AuthTokenDto,
  LoginRequestDto,
  LoginResponseDto,
  MfaVerifyRequestDto,
  OtpChallengeResponseDto,
  ProfileDto,
  RequestOtpDto,
  RoleName,
  STORE_ROLES,
  VerifyOtpDto,
} from '../models/api.models';
import { I18nService } from '../i18n/i18n.service';

const AUTH_STORAGE_KEY = 'nawly.auth.session.v1';
const MFA_STATE_KEY = 'nawly.auth.mfa.pending.v1';

export interface PendingMfaContext {
  challengeToken: string;
  source: 'customer' | 'staff';
  displayTarget: string;
  returnUrl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);

  private readonly sessionSignal = signal<AuthTokenDto | null>(this.readStoredSession());
  private readonly pendingMfaSignal = signal<PendingMfaContext | null>(this.readPendingMfa());
  private refreshInFlight$: Observable<AuthTokenDto> | null = null;

  readonly session = this.sessionSignal.asReadonly();
  readonly pendingMfa = this.pendingMfaSignal.asReadonly();

  readonly isAuthenticated = computed(() => {
    const s = this.sessionSignal();
    return Boolean(s?.accessToken);
  });

  readonly roles = computed<RoleName[]>(() => this.sessionSignal()?.roles ?? []);

  readonly isCustomerOnly = computed(() => {
    const roles = this.roles();
    return roles.includes('Customer') && !this.hasAnyStaffRole(roles);
  });

  readonly isStoreUser = computed(() => this.hasAnyRole(STORE_ROLES));
  readonly isCourier = computed(() => this.roles().includes('Courier'));
  readonly isAdmin = computed(() => this.hasAnyRole(ADMIN_ROLES));

  requestOtp(dto: RequestOtpDto): Observable<OtpChallengeResponseDto> {
    return this.http.post<OtpChallengeResponseDto>('/api/v1/auth/otp/request', dto);
  }

  verifyOtp(dto: VerifyOtpDto): Observable<LoginResponseDto> {
    return this.http.post<LoginResponseDto>('/api/v1/auth/otp/verify', dto).pipe(
      tap((res) => {
        if (!res.requiresMfa && res.tokens) {
          this.setSession(res.tokens);
          this.clearPendingMfa();
        }
      }),
    );
  }

  loginWithPassword(dto: LoginRequestDto): Observable<LoginResponseDto> {
    return this.http.post<LoginResponseDto>('/api/v1/auth/login/password', dto).pipe(
      tap((res) => {
        if (!res.requiresMfa && res.tokens) {
          this.setSession(res.tokens);
          this.clearPendingMfa();
        }
      }),
    );
  }

  verifyMfa(dto: MfaVerifyRequestDto): Observable<LoginResponseDto> {
    return this.http.post<LoginResponseDto>('/api/v1/auth/mfa/verify', dto).pipe(
      tap((res) => {
        if (res.tokens) {
          this.setSession(res.tokens);
          this.clearPendingMfa();
        }
      }),
    );
  }

  refreshToken(): Observable<AuthTokenDto> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    const currentRefresh = this.sessionSignal()?.refreshToken ?? null;
    if (!currentRefresh) {
      return throwError(() => new Error('No refresh token available.'));
    }

    this.refreshInFlight$ = this.http
      .post<AuthTokenDto>('/api/v1/auth/token/refresh', { refreshToken: currentRefresh })
      .pipe(
        tap((tokens) => this.setSession(tokens)),
        finalize(() => {
          this.refreshInFlight$ = null;
        }),
        shareReplay(1),
      );

    return this.refreshInFlight$;
  }

  logout(redirectTo = '/'): Observable<void> {
    const refreshToken = this.sessionSignal()?.refreshToken ?? null;
    return this.http.post<void>('/api/v1/auth/logout', { refreshToken }).pipe(
      catchError(() => of(void 0)),
      tap(() => {
        this.clearSession();
        void this.router.navigateByUrl(redirectTo);
      }),
      map(() => void 0),
    );
  }

  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>('/api/v1/profile');
  }

  setSession(tokens: AuthTokenDto): void {
    this.sessionSignal.set(tokens);
    this.persistSession(tokens);
    if (tokens.preferredLanguage) {
      this.i18n.setFromApiLanguage(tokens.preferredLanguage);
    }
  }

  clearSession(): void {
    this.sessionSignal.set(null);
    this.clearPendingMfa();
    try {
      globalThis.localStorage?.removeItem(AUTH_STORAGE_KEY);
    } catch {
      // Ignore storage access errors
    }
  }

  setPendingMfa(ctx: PendingMfaContext): void {
    this.pendingMfaSignal.set(ctx);
    try {
      globalThis.sessionStorage?.setItem(MFA_STATE_KEY, JSON.stringify(ctx));
    } catch {
      // Ignore sessionStorage errors
    }
  }

  clearPendingMfa(): void {
    this.pendingMfaSignal.set(null);
    try {
      globalThis.sessionStorage?.removeItem(MFA_STATE_KEY);
    } catch {
      // Ignore sessionStorage errors
    }
  }

  hasRole(role: RoleName): boolean {
    return this.roles().includes(role);
  }

  hasAnyRole(allowed: readonly RoleName[]): boolean {
    const current = this.roles();
    return allowed.some((r) => current.includes(r));
  }

  resolveHomeRoute(roles: readonly RoleName[] = this.roles()): string {
    if (roles.some((r) => ADMIN_ROLES.includes(r))) {
      return '/admin';
    }
    if (roles.some((r) => STORE_ROLES.includes(r))) {
      return '/store';
    }
    if (roles.includes('Courier')) {
      return '/courier';
    }
    return '/';
  }

  handleSessionExpired(currentUrl: string): void {
    this.clearSession();
    const isStaffArea =
      currentUrl.startsWith('/store') ||
      currentUrl.startsWith('/courier') ||
      currentUrl.startsWith('/admin');
    const loginPath = isStaffArea ? '/staff/signin' : '/signin';
    const isAuthRoute = currentUrl.startsWith('/signin') || currentUrl.startsWith('/staff/signin');
    void this.router.navigate([loginPath], {
      queryParams: isAuthRoute ? undefined : { returnUrl: currentUrl },
    });
  }

  getAccessToken(): string | null {
    return this.sessionSignal()?.accessToken ?? null;
  }

  private hasAnyStaffRole(roles: readonly RoleName[]): boolean {
    return roles.some(
      (r) => STORE_ROLES.includes(r) || ADMIN_ROLES.includes(r) || r === 'Courier',
    );
  }

  private readStoredSession(): AuthTokenDto | null {
    try {
      const raw = globalThis.localStorage?.getItem(AUTH_STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw) as AuthTokenDto;
      if (!parsed?.accessToken) return null;
      return parsed;
    } catch {
      return null;
    }
  }

  private persistSession(tokens: AuthTokenDto): void {
    try {
      globalThis.localStorage?.setItem(AUTH_STORAGE_KEY, JSON.stringify(tokens));
    } catch {
      // Ignore storage quota errors
    }
  }

  private readPendingMfa(): PendingMfaContext | null {
    try {
      const raw = globalThis.sessionStorage?.getItem(MFA_STATE_KEY);
      if (!raw) return null;
      return JSON.parse(raw) as PendingMfaContext;
    } catch {
      return null;
    }
  }
}
