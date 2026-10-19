import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from './environment';
import { AuthResponse } from './models';

let refreshInFlight: Observable<AuthResponse> | null = null;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.accessToken;
  const authenticatedRequest = token && request.url.startsWith(environment.apiUrl)
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authenticatedRequest).pipe(catchError((error: unknown) => {
    const isAuthEndpoint = /\/auth\/(login|register|refresh|logout)(\?|$)/.test(request.url);
    if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !token || isAuthEndpoint) {
      return throwError(() => error);
    }

    if (!refreshInFlight) {
      refreshInFlight = auth.refresh().pipe(
        finalize(() => { refreshInFlight = null; }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return refreshInFlight.pipe(
      switchMap(session => next(request.clone({ setHeaders: { Authorization: `Bearer ${session.accessToken}` } }))),
      catchError(refreshError => {
        auth.clearSession();
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
        return throwError(() => refreshError);
      })
    );
  }));
};
