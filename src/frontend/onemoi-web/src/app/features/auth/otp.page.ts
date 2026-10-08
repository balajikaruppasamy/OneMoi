import { Component, ElementRef, OnDestroy, OnInit, inject, signal, viewChildren } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { SendOtpResponse } from '../../core/models';
import { ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { LogoComponent } from '../../shared/logo.component';
import { AuthHeroComponent } from './auth-layout';

interface OtpState { destination: string; purpose: 'login' | 'register'; loginAs: 'individual' | 'tenant'; sent: SendOtpResponse; }

@Component({
  selector: 'mk-otp',
  imports: [RouterLink, IconComponent, LogoComponent, AuthHeroComponent],
  template: `
  <div class="auth">
    <mk-auth-hero />
    <section class="auth-panel">
      <div class="auth-box stack">
        <div class="row between"><a class="btn btn-ghost btn-icon" routerLink="/login"><mk-icon name="back" /></a><div class="auth-logo"><mk-logo [size]="34" /></div></div>
        <div class="icon-tile" style="width:64px;height:64px;border-radius:20px"><mk-icon name="phone" [size]="28" /></div>
        <div><h1>Enter verification code</h1>
          <p class="muted mt-1">We sent a 6-digit code to <b style="color:var(--text)">{{ state?.sent?.sentTo }}</b> <a routerLink="/login" class="small">Change</a></p></div>

        @if (state?.sent?.devOtp) {
          <div class="alert warn"><mk-icon name="info" /><div class="small"><b>Development mode:</b> your OTP is <b style="font-size:1.1rem;letter-spacing:.15em">{{ state!.sent.devOtp }}</b>.
            In production it is sent only by SMS / e-mail. <button class="link-btn" (click)="fill(state!.sent.devOtp!)">Fill it</button></div></div>
        }

        <div class="otp" [class.is-error]="!!error()">
          @for (i of slots; track i) {
            <input #box inputmode="numeric" maxlength="1" [attr.autocomplete]="i === 0 ? 'one-time-code' : 'off'" [attr.aria-label]="'Digit ' + (i + 1)"
                   (input)="onInput(i, $event)" (keydown)="onKey(i, $event)" (paste)="onPaste($event)" />
          }
        </div>
        @if (error()) { <div class="field-error">{{ error() }}</div> }

        <div class="row between small">
          @if (left() > 0) { <span class="muted">Resend code in <b class="num">0:{{ left().toString().padStart(2, '0') }}</b></span> }
          @else { <button class="btn btn-ghost btn-sm" (click)="resend()"><mk-icon name="refresh" [size]="16" />Resend OTP</button> }
        </div>
        <button class="btn btn-primary btn-lg btn-block" [disabled]="busy() || code().length < 6" (click)="verify()">@if (busy()) {<span class="spinner"></span>} Verify &amp; continue</button>
      </div>
    </section>
  </div>`
})
export class OtpPage implements OnInit, OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private boxes = viewChildren<ElementRef<HTMLInputElement>>('box');
  protected slots = [0, 1, 2, 3, 4, 5];
  protected state: OtpState | null = null;
  protected code = signal('');
  protected error = signal('');
  protected busy = signal(false);
  protected left = signal(30);
  private timer?: ReturnType<typeof setInterval>;

  ngOnInit() {
    try { this.state = JSON.parse(sessionStorage.getItem('onemoi.otp') ?? 'null'); } catch { /* ignore */ }
    if (!this.state) { this.router.navigateByUrl('/login'); return; }
    this.startTimer();
    setTimeout(() => this.boxes()[0]?.nativeElement.focus(), 50);
  }
  ngOnDestroy() { clearInterval(this.timer); }

  fill(code: string) { code.split('').forEach((d, i) => { const b = this.boxes()[i]; if (b) b.nativeElement.value = d; }); this.sync(); this.verify(); }

  onInput(i: number, e: Event) {
    const el = e.target as HTMLInputElement;
    const v = el.value.replace(/\D/g, '');
    if (v.length > 1) { this.fill(v.slice(0, 6)); return; }
    el.value = v;
    if (v && i < 5) this.boxes()[i + 1].nativeElement.focus();
    this.sync();
    if (this.code().length === 6) this.verify();
  }
  onKey(i: number, e: KeyboardEvent) {
    if (e.key === 'Backspace' && !(e.target as HTMLInputElement).value && i > 0) this.boxes()[i - 1].nativeElement.focus();
  }
  onPaste(e: ClipboardEvent) {
    e.preventDefault();
    this.fill((e.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6));
  }
  private sync() { this.code.set(this.boxes().map(b => b.nativeElement.value).join('')); this.error.set(''); }

  async verify() {
    if (!this.state || this.code().length < 6 || this.busy()) return;
    this.busy.set(true);
    try {
      const p = await this.auth.verifyOtp(this.state.destination, this.code(), this.state.purpose, this.state.loginAs);
      sessionStorage.removeItem('onemoi.otp');
      this.toast.ok(`Welcome, ${p.name}`);
      await this.router.navigateByUrl(p.home);
    } catch (e) {
      this.error.set(apiError(e).message);
      this.boxes().forEach(b => (b.nativeElement.value = ''));
      this.code.set('');
      this.boxes()[0]?.nativeElement.focus();
    } finally { this.busy.set(false); }
  }

  async resend() {
    if (!this.state) return;
    try {
      this.state.sent = await this.auth.sendOtp(this.state.destination, this.state.purpose);
      sessionStorage.setItem('onemoi.otp', JSON.stringify(this.state));
      this.toast.ok('A new code has been sent');
      this.startTimer();
    } catch (e) { this.error.set(apiError(e).message); }
  }
  private startTimer() {
    clearInterval(this.timer);
    this.left.set(this.state?.sent?.resendAfterSeconds ?? 30);
    this.timer = setInterval(() => { this.left.update(v => Math.max(0, v - 1)); if (this.left() === 0) clearInterval(this.timer); }, 1000);
  }
}
