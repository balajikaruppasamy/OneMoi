import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { LogoComponent } from '../../shared/logo.component';
import { AuthHeroComponent } from './auth-layout';
import { environment } from '../../../environments/environment';

type Mode = 'otp' | 'password' | 'operator';

/** Demo accounts come from environment.ts (development only) — the production build has none. */
type DemoLogin = { who: string; mode: Mode; login: string; secret: string; tenant?: string };
const DEMO = environment.demoLogins as DemoLogin[];

@Component({
  selector: 'mk-login',
  imports: [FormsModule, RouterLink, IconComponent, LogoComponent, AuthHeroComponent],
  template: `
  <div class="auth">
    <mk-auth-hero />
    <section class="auth-panel">
      <div class="auth-box stack">
        <div class="auth-logo"><mk-logo /></div>
        <div class="mt-4"><h1>Welcome back 👋</h1><p class="muted mt-1">Choose how you want to log in.</p></div>

        <div class="segmented">
          <button [class.active]="mode() === 'otp'" (click)="setMode('otp')"><mk-icon name="phone" [size]="17" />Mobile OTP</button>
          <button [class.active]="mode() === 'password'" (click)="setMode('password')"><mk-icon name="key" [size]="17" />Password</button>
          <button [class.active]="mode() === 'operator'" (click)="setMode('operator')"><mk-icon name="lock" [size]="17" />Operator</button>
        </div>

        @if (notice) { <div class="alert warn"><mk-icon name="info" /><div>{{ notice }}</div></div> }
        @if (error()) { <div class="alert err"><mk-icon name="alert" /><div>{{ error() }}</div></div> }

        @switch (mode()) {
          @case ('otp') {
            <form class="stack" (ngSubmit)="sendOtp()">
              <div class="field">
                <label class="label">Mobile number or e-mail</label>
                <input class="input" name="dest" [(ngModel)]="dest" placeholder="98765 43210" autocomplete="username" required />
              </div>
              <div class="field"><span class="label">I am logging in as</span>
                <div class="chips">
                  <button type="button" class="chip" [class.active]="loginAs() === 'individual'" (click)="loginAs.set('individual')">Individual / Function host</button>
                  <button type="button" class="chip" [class.active]="loginAs() === 'tenant'" (click)="loginAs.set('tenant')">Moi vendor staff</button>
                </div>
              </div>
              <button class="btn btn-primary btn-lg btn-block" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} Send OTP</button>
              <p class="xs muted center">First time? Logging in with OTP creates your free OneMoi account.</p>
            </form>
          }
          @case ('password') {
            <form class="stack" (ngSubmit)="passwordLogin()">
              <div class="field"><label class="label">E-mail or mobile</label><input class="input" name="login" [(ngModel)]="login" autocomplete="username" required /></div>
              <div class="field"><label class="label">Password</label><input class="input" type="password" name="pwd" [(ngModel)]="secret" autocomplete="current-password" required /></div>
              <button class="btn btn-primary btn-lg btn-block" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} Login</button>
              <p class="xs muted center">For OneMoi admin and Moi vendor staff. <a routerLink="/forgot-password">Forgot password?</a></p>
            </form>
          }
          @case ('operator') {
            <form class="stack" (ngSubmit)="operatorLogin()">
              <div class="alert"><mk-icon name="info" /><div class="small">Your vendor gives you the <b>vendor code</b>, <b>operator ID</b> and <b>PIN</b>. You can enter Moi only during your assigned function time.</div></div>
              <div class="grid grid-2" style="gap:12px">
                <div class="field"><label class="label">Vendor code</label><input class="input" name="tc" [(ngModel)]="tenantCode" placeholder="JDMOI" style="text-transform:uppercase" required /></div>
                <div class="field"><label class="label">Operator ID</label><input class="input" name="oc" [(ngModel)]="login" placeholder="OP-101" style="text-transform:uppercase" required /></div>
              </div>
              <div class="field"><label class="label">PIN</label><input class="input" type="password" inputmode="numeric" maxlength="6" name="pin" [(ngModel)]="secret" placeholder="••••" required /></div>
              <button class="btn btn-primary btn-lg btn-block" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} Start counter</button>
            </form>
          }
        }

        <p class="center small">New here? <a routerLink="/register">Create an account</a></p>

        @if (showDemo) {
        <details class="card card-pad">
          <summary style="cursor:pointer;font-weight:800">🧪 Demo logins (local testing)</summary>
          <div class="list mt-2">
            @for (d of demo; track d.who) {
              <button type="button" class="list-item link-btn" style="width:100%;text-align:left;padding:10px 4px;color:inherit" (click)="useDemo(d)">
                <div class="grow"><div class="small" style="font-weight:700">{{ d.who }}</div>
                  <div class="xs muted">{{ d.tenant ? d.tenant + ' · ' : '' }}{{ d.login }}{{ d.secret ? ' · ' + d.secret : ' · OTP shown on next screen' }}</div></div>
                <mk-icon name="chevron" [size]="16" />
              </button>
            }
          </div>
        </details>
        }
      </div>
    </section>
  </div>`
})
export class LoginPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  protected demo = DEMO;
  protected showDemo = DEMO.length > 0;
  protected mode = signal<Mode>('otp');
  protected loginAs = signal<'individual' | 'tenant'>('individual');
  protected busy = signal(false);
  protected error = signal('');
  dest = ''; login = ''; secret = ''; tenantCode = '';
  /** Why the last session ended (password changed, signed out everywhere, vendor suspended…) */
  protected notice = this.auth.takeNotice();

  setMode(m: Mode) { this.mode.set(m); this.error.set(''); }

  useDemo(d: (typeof DEMO)[number]) {
    this.setMode(d.mode);
    if (d.mode === 'otp') { this.dest = d.login; this.loginAs.set('individual'); this.sendOtp(); return; }
    this.login = d.login; this.secret = d.secret; this.tenantCode = d.tenant ?? '';
    d.mode === 'password' ? this.passwordLogin() : this.operatorLogin();
  }

  async sendOtp() {
    await this.run(async () => {
      const r = await this.auth.sendOtp(this.dest.trim(), 'login');
      sessionStorage.setItem('onemoi.otp', JSON.stringify({ destination: this.dest.trim(), purpose: 'login', loginAs: this.loginAs(), sent: r }));
      await this.router.navigateByUrl('/otp');
    });
  }
  async passwordLogin() {
    await this.run(async () => this.go(await this.auth.passwordLogin(this.login.trim(), this.secret)));
  }
  async operatorLogin() {
    await this.run(async () => this.go(await this.auth.operatorLogin(this.tenantCode.trim(), this.login.trim(), this.secret)));
  }

  private async go(p: { name: string; home: string }) {
    this.toast.ok(`Welcome, ${p.name}`);
    await this.router.navigateByUrl(p.home);
  }
  private async run(fn: () => Promise<void>) {
    this.error.set(''); this.busy.set(true);
    try { await fn(); } catch (e) { this.error.set(apiError(e).message); } finally { this.busy.set(false); }
  }
}
