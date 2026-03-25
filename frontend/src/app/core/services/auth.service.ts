import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from '../models/auth.models';

interface JwtPayload {
  sub: string;
  unique_name: string;
  role: string;
  exp: number;
}

const TOKEN_KEY = 'ca_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = '/api/auth';

  private readonly _currentUser$ = new BehaviorSubject<CurrentUser | null>(
    this.loadUserFromStorage()
  );

  readonly currentUser$ = this._currentUser$.asObservable();

  get currentUser() {
    return this._currentUser$.getValue();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiBase}/login`, request).pipe(
      tap(response => this.persistSession(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiBase}/register`, request).pipe(
      tap(response => this.persistSession(response))
    );
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBase}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBase}/reset-password`, { token, newPassword });
  }

  verifyEmail(token: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBase}/verify-email`, { token });
  }

  resendVerification(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiBase}/resend-verification`, {});
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._currentUser$.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const user = this._currentUser$.getValue();
    return user !== null && user.expiresAt > new Date();
  }

  hasRole(role: 'Participant' | 'Moderator' | 'Admin'): boolean {
    const user = this._currentUser$.getValue();
    if (!user) return false;
    const hierarchy = { Participant: 0, Moderator: 1, Admin: 2 };
    return hierarchy[user.role] >= hierarchy[role];
  }

  private persistSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    const user = this.decodeToken(response.token, response.expiresAt);
    this._currentUser$.next(user);
  }

  private loadUserFromStorage(): CurrentUser | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;
    try {
      const payload = this.parseJwtPayload(token);
      const expiresAt = new Date(payload.exp * 1000);
      if (expiresAt <= new Date()) {
        localStorage.removeItem(TOKEN_KEY);
        return null;
      }
      return {
        id: payload.sub,
        username: payload.unique_name,
        role: payload.role as CurrentUser['role'],
        expiresAt,
      };
    } catch {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }
  }

  private decodeToken(token: string, expiresAtIso: string): CurrentUser {
    const payload = this.parseJwtPayload(token);
    return {
      id: payload.sub,
      username: payload.unique_name,
      role: payload.role as CurrentUser['role'],
      expiresAt: new Date(expiresAtIso),
    };
  }

  private parseJwtPayload(token: string): JwtPayload {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(json) as JwtPayload;
  }
}
