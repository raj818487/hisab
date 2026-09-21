import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthTokenService } from './auth-token.service';
import { environment } from '../../environments/environment';

let refreshInFlight: Promise<boolean> | null = null;

async function refreshTokens(tokens: AuthTokenService): Promise<boolean> {
  const refresh = tokens.refreshToken;
  if (!refresh) return false;
  try {
    const res = await fetch(`${environment.apiBaseUrl}/account/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: refresh }),
    });
    if (!res.ok) {
      tokens.clear();
      return false;
    }
    const body = (await res.json()) as {
      accessToken: string;
      refreshToken: string;
      name: string;
    };
    tokens.setSession(body.accessToken, body.refreshToken, body.name);
    return true;
  } catch {
    tokens.clear();
    return false;
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(AuthTokenService);
  const access = tokens.accessToken;
  const authReq = access ? req.clone({ setHeaders: { Authorization: `Bearer ${access}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (
        err.status !== 401 ||
        req.url.includes('/account/login') ||
        req.url.includes('/account/register') ||
        req.url.includes('/account/refresh')
      ) {
        return throwError(() => err);
      }
      if (!tokens.refreshToken) {
        tokens.clear();
        return throwError(() => err);
      }
      if (!refreshInFlight) {
        refreshInFlight = refreshTokens(tokens).finally(() => {
          refreshInFlight = null;
        });
      }
      return from(refreshInFlight).pipe(
        switchMap((ok) => {
          if (!ok || !tokens.accessToken) return throwError(() => err);
          return next(req.clone({ setHeaders: { Authorization: `Bearer ${tokens.accessToken}` } }));
        }),
      );
    }),
  );
};
