import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { SendOtpResponse } from '../../core/models';
import { ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { LogoComponent } from '../../shared/logo.component';
import { AuthHeroComponent } from '../auth/auth-layout';

/** My account: change password + sign out from every device. Available to every logged-in user. */
@Component({
  selector: 'mk-account',
  imports: [FormsModule, IconComponent],
  template: `
  <div class="page-head"><div><h1>My account &amp; security</h1><p class="muted">{{ auth.profile()?.name }} · {{ role() }}</p></div></div>
  <div class="grid grid-2" style="align-items:start">
    @if (auth.profile()?.hasPassword) {
      <form class="card card-pad stack" (ngSubmit)="change()">
        <h3>Change password</h3>
        <p class="small muted">After changing, you stay logged in here but every other phone / computer is signed out.</p>
        @if (error()) { <div class="alert err"><mk-icon name="alert" /><div>{{ error() }}</div></div> }
        <div class="field"><label class="label">Current password</label><input class="input" type="password" name="cur" [(ngModel)]="cur" autocomplete="current-password" />
          @if (err('currentPassword')) {<span class="field-error">{{ err('currentPassword') }}</span>}</div>
        <div class="field"><label class="label">New password</label><input class="input" type="password" name="nw" [(ngModel)]="nw" autocomplete="new-password" />
          <span class="hint">At least 8 characters, with letters and numbers.</span>
          @if (err('newPassword')) {<span class="field-error">{{ err('newPassword') }}</span>}</div>
        <div class="field"><label class="label">Confirm new password</label><input class="input" type="password" name="cf" [(ngModel)]="cf" autocomplete="new-password" />
          @if (cf && cf !== nw) {<span class="field-error">Passwords do not match</span>}</div>
        <button class="btn btn-primary" [disabled]="busy() || !cur || !nw || cf !== nw"><mk-icon name="key" />Change password</button>
      </form>
    } @else {
      <div class="card card-pad stack"><h3>Login method</h3>
        <p class="muted">{{ auth.profile()?.principalType === 'Operator' ? 'You log in with your operator ID and PIN. Ask your vendor to reset the PIN if needed.' : 'You log in with a one-time code (OTP) sent to your mobile ' + (auth.profile()?.mobile ?? '') + '. No password needed.' }}</p>
      </div>
    }
    <div class="card card-pad stack">
      <h3>Sign out everywhere</h3>
      <p class="small muted">Lost a phone, or logged in on someone else's computer? This ends every session of your login, including this one.</p>
      <button class="btn btn-danger" (click)="logoutAll()"><mk-icon name="logout" />Sign out from all devices</button>
      <div class="section-title mt-4">What you can do</div>
      <div class="chips">@for (p of auth.profile()?.permissions ?? []; track p) {<span class="badge">{{ p }}</span>}</div>
    </div>
  </div>`
})
export class AccountPage {
  protected auth = inject(AuthService);
  private toast = inject(ToastService);
  protected busy = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  cur = ''; nw = ''; cf = '';

  role() {
    const p = this.auth.profile();
    return p?.principalType === 'Operator' ? 'Operator' : p?.role ? `${p.role} · ${p.tenantName}` : p?.userType ?? '';
  }
  err(k: string) { return this.errors()[k]?.[0] ?? ''; }

  async change() {
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try {
      await this.auth.changePassword(this.cur, this.nw);
      this.cur = this.nw = this.cf = '';
      this.toast.ok('Password changed. Other devices have been signed out.');
    } catch (e) { const a = apiError(e); this.errors.set(a.errors ?? {}); if (!a.errors) this.error.set(a.message); }
    finally { this.busy.set(false); }
  }
  async logoutAll() {
    if (!confirm('Sign out from ALL devices, including this one?')) return;
    try { await this.auth.logoutAllDevices(); } catch (e) { this.toast.err(apiError(e).message); }
  }
}

/** Forgot password (super admin & vendor staff): get a code on mobile / e-mail, set a new password. Also unlocks a locked account. */
@Component({
  selector: 'mk-forgot',
  imports: [FormsModule, RouterLink, IconComponent, LogoComponent, AuthHeroComponent],
  template: `
  <div class="auth">
    <mk-auth-hero />
    <section class="auth-panel">
      <div class="auth-box stack">
        <div class="row between"><a class="btn btn-ghost btn-icon" routerLink="/login"><mk-icon name="back" /></a><div class="auth-logo"><mk-logo [size]="34" /></div></div>
        <div><h1>Reset password</h1><p class="muted mt-1">For OneMoi admin and Moi vendor staff. Individuals log in with OTP and need no password.</p></div>
        @if (error()) { <div class="alert err"><mk-icon name="alert" /><div>{{ error() }}</div></div> }

        @if (!sent()) {
          <form class="stack" (ngSubmit)="send()">
            <div class="field"><label class="label">Your login e-mail or mobile</label><input class="input" name="login" [(ngModel)]="login" autocomplete="username" required /></div>
            <button class="btn btn-primary btn-lg btn-block" [disabled]="busy() || !login.trim()">Send reset code</button>
          </form>
        } @else {
          <div class="alert ok"><mk-icon name="check" /><div class="small">If an account exists for <b>{{ login }}</b>, a 6-digit code was sent to <b>{{ sent()!.sentTo }}</b>.</div></div>
          @if (sent()!.devOtp) { <div class="alert warn"><mk-icon name="info" /><div class="small"><b>Development mode:</b> code is <b>{{ sent()!.devOtp }}</b></div></div> }
          <form class="stack" (ngSubmit)="reset()">
            <div class="field"><label class="label">Code</label><input class="input input-xl center" name="code" inputmode="numeric" maxlength="6" [(ngModel)]="code" style="letter-spacing:.3em" /></div>
            <div class="field"><label class="label">New password</label><input class="input" type="password" name="pw" [(ngModel)]="pw" autocomplete="new-password" />
              <span class="hint">At least 8 characters, with letters and numbers.</span>
              @if (err('newPassword')) {<span class="field-error">{{ err('newPassword') }}</span>}</div>
            <button class="btn btn-primary btn-lg btn-block" [disabled]="busy() || code.length < 6 || !pw">Set new password &amp; login</button>
          </form>
        }
      </div>
    </section>
  </div>`
})
export class ForgotPasswordPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  protected sent = signal<SendOtpResponse | null>(null);
  protected busy = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  login = ''; code = ''; pw = '';

  err(k: string) { return this.errors()[k]?.[0] ?? ''; }
  async send() {
    this.busy.set(true); this.error.set('');
    try { this.sent.set(await this.auth.forgotPassword(this.login.trim())); } catch (e) { this.error.set(apiError(e).message); }
    finally { this.busy.set(false); }
  }
  async reset() {
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try {
      const p = await this.auth.resetPassword(this.login.trim(), this.code.trim(), this.pw);
      this.toast.ok('Password changed. Welcome back!');
      await this.router.navigateByUrl(p.home);
    } catch (e) { const a = apiError(e); this.errors.set(a.errors ?? {}); if (!a.errors) this.error.set(a.message); }
    finally { this.busy.set(false); }
  }
}

/** Shown when a route needs a permission the user does not have. */
@Component({
  selector: 'mk-forbidden',
  imports: [RouterLink, IconComponent],
  template: `
  <div class="card empty" style="max-width:520px;margin:40px auto">
    <div class="empty-art" style="background:var(--red-soft);color:var(--red)"><mk-icon name="lock" [size]="40" /></div>
    <h2>No access</h2>
    <p>Your role ({{ auth.profile()?.role ?? auth.profile()?.userType }}) does not allow this page. Ask your vendor owner if you need access.</p>
    <a class="btn btn-primary mt-2" [routerLink]="auth.profile()?.home ?? '/login'">Go to my home</a>
  </div>`
})
export class ForbiddenPage {
  protected auth = inject(AuthService);
}
