import { Injectable } from '@angular/core';

const ACCESS = 'mh_access_token';
const REFRESH = 'mh_refresh_token';
const NAME = 'mh_user_name';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  get accessToken(): string | null {
    return localStorage.getItem(ACCESS);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH);
  }

  get userName(): string | null {
    return localStorage.getItem(NAME);
  }

  setSession(accessToken: string, refreshToken: string, name: string): void {
    localStorage.setItem(ACCESS, accessToken);
    localStorage.setItem(REFRESH, refreshToken);
    localStorage.setItem(NAME, name);
  }

  clear(): void {
    localStorage.removeItem(ACCESS);
    localStorage.removeItem(REFRESH);
    localStorage.removeItem(NAME);
  }

  get isSignedIn(): boolean {
    return !!this.accessToken;
  }
}
