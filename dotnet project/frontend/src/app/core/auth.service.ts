import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, finalize, of, tap, throwError } from 'rxjs';
import { environment } from './environment';
import { AuthResponse, UserRole } from './models';

const SESSION_KEY = 'foodsupply.session.v1';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessionSubject = new BehaviorSubject<AuthResponse | null>(this.readSession());
  readonly session$ = this.sessionSubject.asObservable();

  get session(): AuthResponse | null { return this.sessionSubject.value; }
  get accessToken(): string | null { return this.session?.accessToken ?? null; }
  get role(): UserRole | null { return this.session?.role ?? null; }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, {
      email, password, deviceName: this.deviceName()
    }).pipe(tap(session => this.setSession(session)));
  }

  register(body: object): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, body)
      .pipe(tap(session => this.setSession(session)));
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.session?.refreshToken;
    if (!refreshToken) return throwError(() => new Error('No refresh token is available.'));
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(tap(session => this.setSession(session)));
  }

  logout(): Observable<void> {
    const refreshToken = this.session?.refreshToken;
    if (!refreshToken) {
      this.clearSession();
      return of(undefined);
    }
    return this.http.post<void>(`${environment.apiUrl}/auth/logout`, { refreshToken })
      .pipe(catchError(() => of(undefined)),
        finalize(() => this.clearSession()));
  }

  clearSession(): void {
    sessionStorage.removeItem(SESSION_KEY);
    this.sessionSubject.next(null);
  }

  private setSession(session: AuthResponse): void {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    this.sessionSubject.next(session);
  }

  private readSession(): AuthResponse | null {
    try {
      const stored = sessionStorage.getItem(SESSION_KEY);
      return stored ? JSON.parse(stored) as AuthResponse : null;
    } catch {
      sessionStorage.removeItem(SESSION_KEY);
      return null;
    }
  }

  private deviceName(): string {
    const agent = navigator.userAgent;
    const browser = agent.includes('Edg/') ? 'Edge' : agent.includes('Firefox/') ? 'Firefox' : agent.includes('Chrome/') ? 'Chrome' : 'Browser';
    return `${browser} on ${navigator.platform || 'this device'}`.slice(0, 100);
  }
}
