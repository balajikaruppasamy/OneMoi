import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { ToastService } from './ui';

/**
 * Adds "Authorization: Bearer …", refreshes an expired token once, and reacts to security answers:
 *  401  → try refresh; if that fails, go to login with the reason
 *  403 TENANT_SUSPENDED → sign out (vendor suspended by OneMoi)
 *  429  → "too many attempts" toast
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  const isAuthCall = req.url.includes('/api/auth/') && !req.url.endsWith('/api/auth/me')
    && !req.url.endsWith('/api/auth/logout-all') && !req.url.endsWith('/api/auth/password/change');
  const withToken = (token: string | null) => (token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);

  return next(withToken(auth.accessToken)).pipe(
    catchError((err: HttpErrorResponse) => {
      const code = (err.error as { code?: string; message?: string } | null)?.code;
      const message = (err.error as { message?: string } | null)?.message;

      if (err.status === 403 && code === 'TENANT_SUSPENDED') {
        auth.logout(message);
        return throwError(() => err);
      }
      if (err.status === 429) toast.err(message ?? 'Too many attempts. Please wait a minute.');
      if (err.status !== 401 || isAuthCall || !auth.refreshToken) return throwError(() => err);

      // Token expired, password changed elsewhere, role changed… → try once to get a fresh session
      return from(auth.refresh()).pipe(
        switchMap(ok => {
          if (ok) return next(withToken(auth.accessToken));
          auth.logout(message ?? 'Your session has ended. Please login again.');
          return throwError(() => err);
        })
      );
    })
  );
};
