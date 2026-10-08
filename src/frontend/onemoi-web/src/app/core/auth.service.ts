import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { AuthResponse, SendOtpResponse, SessionProfile } from './models';

const KEY = 'onemoi.session';
const NOTICE = 'onemoi.notice';

interface StoredSession { accessToken: string; refreshToken: string; expiresAt: string; profile: SessionProfile; }

/**
 * Login state for every user type.
 *  - Super admin / vendor staff: password  (or OTP for vendor staff)
 *  - Individuals & function hosts: mobile OTP
 *  - Operators: vendor code + operator code + PIN
 * Permissions come from the server; use can('functions.manage') or the *mkCan directive.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);
  private session = signal<StoredSession | null>(read());

  readonly profile = computed(() => this.session()?.profile ?? null);
  readonly isLoggedIn = computed(() => !!this.session());
  private readonly perms = computed(() => new Set(this.profile()?.permissions ?? []));
  /** 'admin' | 'vendor' | 'operator' | 'individual' — which app area this login uses */
  readonly area = computed(() => {
    const p = this.profile();
    if (!p) return null;
    if (p.principalType === 'Operator') return 'operator';
    return p.userType === 'SuperAdmin' ? 'admin' : p.userType === 'TenantUser' ? 'vendor' : 'individual';
  });

  /** Does the logged-in user have this permission? */
  can(permission: string) { return this.perms().has(permission); }
  canAny(...permissions: string[]) { return permissions.some(p => this.perms().has(p)); }

  get accessToken() { return this.session()?.accessToken ?? null; }
  get refreshToken() { return this.session()?.refreshToken ?? null; }

  // ── login flows ──
  sendOtp(destination: string, purpose: 'login' | 'register' = 'login') {
    return this.api.post<SendOtpResponse>('/api/auth/otp/send', { destination, purpose });
  }
  async verifyOtp(destination: string, code: string, purpose: 'login' | 'register', loginAs: 'individual' | 'tenant') {
    return this.store(await this.api.post<AuthResponse>('/api/auth/otp/verify', { destination, code, purpose, loginAs }));
  }
  async passwordLogin(login: string, password: string) {
    return this.store(await this.api.post<AuthResponse>('/api/auth/login/password', { login, password }));
  }
  async operatorLogin(tenantCode: string, operatorCode: string, pin: string) {
    return this.store(await this.api.post<AuthResponse>('/api/auth/login/operator', { tenantCode, operatorCode, pin }));
  }
  registerIndividual(body: object) { return this.api.post<SendOtpResponse>('/api/auth/register/individual', body); }
  registerVendor(body: object) { return this.api.post<SendOtpResponse>('/api/auth/register/vendor', body); }

  // ── passwords ──
  forgotPassword(login: string) { return this.api.post<SendOtpResponse>('/api/auth/password/forgot', { login }); }
  async resetPassword(login: string, code: string, newPassword: string) {
    return this.store(await this.api.post<AuthResponse>('/api/auth/password/reset', { login, code, newPassword }));
  }
  /** Other devices are signed out; this one gets a fresh session. */
  async changePassword(currentPassword: string, newPassword: string) {
    return this.store(await this.api.post<AuthResponse>('/api/auth/password/change', { currentPassword, newPassword }));
  }

  /** Called by the interceptor when the access token expires. */
  async refresh(): Promise<boolean> {
    const rt = this.refreshToken;
    if (!rt) return false;
    try {
      this.store(await this.api.post<AuthResponse>('/api/auth/refresh', { refreshToken: rt }));
      return true;
    } catch {
      this.clear();
      return false;
    }
  }

  async reloadProfile() {
    const s = this.session();
    if (!s) return;
    const profile = await this.api.get<SessionProfile>('/api/auth/me');
    this.save({ ...s, profile });
  }

  async logout(notice?: string) {
    const rt = this.refreshToken;
    this.clear();
    if (rt) this.api.post('/api/auth/logout', { refreshToken: rt }).catch(() => {});
    if (notice) this.setNotice(notice);
    await this.router.navigateByUrl('/login');
  }

  /** Signs out on every phone / computer where this login is used. */
  async logoutAllDevices() {
    await this.api.post('/api/auth/logout-all');
    this.clear();
    this.setNotice('You have been signed out from all devices.');
    await this.router.navigateByUrl('/login');
  }

  /** One-time message shown on the login page (e.g. "your session ended because…"). */
  setNotice(text: string) { try { sessionStorage.setItem(NOTICE, text); } catch { /* ignore */ } }
  takeNotice() {
    try { const n = sessionStorage.getItem(NOTICE); sessionStorage.removeItem(NOTICE); return n; } catch { return null; }
  }

  clear() { this.save(null); }

  private store(r: AuthResponse) {
    this.save({ accessToken: r.accessToken, refreshToken: r.refreshToken, expiresAt: r.expiresAt, profile: r.profile });
    return r.profile;
  }
  private save(s: StoredSession | null) {
    this.session.set(s);
    try { s ? localStorage.setItem(KEY, JSON.stringify(s)) : localStorage.removeItem(KEY); } catch { /* storage blocked */ }
  }
}

function read(): StoredSession | null {
  try {
    const s = JSON.parse(localStorage.getItem(KEY) ?? 'null') as StoredSession | null;
    return s?.profile?.permissions ? s : null;     // sessions from before permissions existed → login again
  } catch { return null; }
}
