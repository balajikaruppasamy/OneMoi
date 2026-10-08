import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { IconComponent } from '../../shared/icon.component';
import { LogoComponent } from '../../shared/logo.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';
import { AuthHeroComponent } from './auth-layout';

@Component({
  selector: 'mk-register',
  imports: [FormsModule, RouterLink, IconComponent, LogoComponent, TamilFieldComponent, AuthHeroComponent],
  template: `
  <div class="auth">
    <mk-auth-hero />
    <section class="auth-panel">
      <div class="auth-box stack">
        <div class="auth-logo"><mk-logo /></div>
        <div class="stepper mt-2"><div class="step active"><i>1</i>Details</div><div class="line"></div><div class="step"><i>2</i>Verify</div><div class="line"></div><div class="step"><i>3</i>Done</div></div>
        <div><h1>Create your account</h1><p class="muted mt-1">It takes less than a minute.</p></div>

        <div class="grid" style="gap:10px">
          <label class="choice"><input type="radio" name="type" [checked]="type() === 'individual'" (change)="type.set('individual')" />
            <div class="icon-tile"><mk-icon name="user" /></div>
            <div class="grow"><div style="font-weight:800">Individual</div><div class="small muted">Track the Moi I give · Free</div></div>
            <span class="tick"><mk-icon name="check" [size]="14" /></span></label>
          <label class="choice"><input type="radio" name="type" [checked]="type() === 'vendor'" (change)="type.set('vendor')" />
            <div class="icon-tile gold"><mk-icon name="store" /></div>
            <div class="grow"><div style="font-weight:800">Moi Vendor / Service</div><div class="small muted">I run Moi counters · approval needed</div></div>
            <span class="tick"><mk-icon name="check" [size]="14" /></span></label>
        </div>

        @if (error()) { <div class="alert err"><mk-icon name="alert" /><div>{{ error() }}</div></div> }

        <form class="stack" (ngSubmit)="submit()">
          @if (type() === 'vendor') {
            <mk-tamil-field label="Business name" [required]="true" [(en)]="f.businessName" [(ta)]="f.businessNameTa" placeholder="e.g. JD Moi Tech" [invalid]="!!err('businessName')" [errorText]="err('businessName')" />
            <div class="field"><label class="label">Owner name <span class="req">*</span></label><input class="input" name="owner" [(ngModel)]="f.ownerName" />
              @if (err('ownerName')) {<span class="field-error">{{ err('ownerName') }}</span>}</div>
          } @else {
            <mk-tamil-field label="Your name" [required]="true" [(en)]="f.name" [(ta)]="f.nameTa" placeholder="Ram" [invalid]="!!err('name')" [errorText]="err('name')" />
          }
          <div class="field"><label class="label">Mobile number <span class="req">*</span></label>
            <div class="input-group"><span class="addon">+91</span><input class="input" name="mobile" type="tel" inputmode="numeric" maxlength="10" [(ngModel)]="f.mobile" placeholder="98765 43210" /></div>
            @if (err('mobile')) {<span class="field-error">{{ err('mobile') }}</span>} @else {<span class="hint">{{ type() === 'vendor' ? 'Used to log in to your vendor dashboard.' : 'This becomes your permanent Moi identity.' }}</span>}</div>
          <div class="field"><label class="label">E-mail</label><input class="input" name="email" type="email" [(ngModel)]="f.email" placeholder="you@example.com" />
            @if (err('email')) {<span class="field-error">{{ err('email') }}</span>}</div>
          <div class="grid grid-2" style="gap:12px">
            <div class="field"><label class="label">City / Ooru</label><input class="input" name="city" [(ngModel)]="f.city" placeholder="Pollachi" /></div>
            @if (type() === 'individual') {
              <div class="field"><label class="label">Work</label><input class="input" name="work" [(ngModel)]="f.work" placeholder="Teacher" /></div>
            } @else {
              <div class="field"><label class="label">Password <span class="req">*</span></label><input class="input" name="pwd" type="password" [(ngModel)]="f.password" autocomplete="new-password" />
                @if (err('password')) {<span class="field-error">{{ err('password') }}</span>}</div>
            }
          </div>
          <label class="check"><input type="checkbox" name="consent" [(ngModel)]="f.consent" /><span>I agree to the Terms &amp; Privacy Policy and consent to OneMoi linking Moi records made with this mobile number to my account.</span></label>
          @if (err('consent')) {<span class="field-error">{{ err('consent') }}</span>}
          <button class="btn btn-primary btn-lg btn-block" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} Continue — send OTP</button>
        </form>
        <p class="center small">Already have an account? <a routerLink="/login">Login</a></p>
      </div>
    </section>
  </div>`
})
export class RegisterPage {
  private auth = inject(AuthService);
  private router = inject(Router);
  protected type = signal<'individual' | 'vendor'>('individual');
  protected busy = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  f = { name: '', nameTa: '', businessName: '', businessNameTa: '', ownerName: '', mobile: '', email: '', city: '', work: '', password: '', consent: false };

  err(k: string) { return this.errors()[k]?.[0] ?? ''; }

  async submit() {
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try {
      const vendor = this.type() === 'vendor';
      const sent = vendor ? await this.auth.registerVendor(this.f) : await this.auth.registerIndividual(this.f);
      sessionStorage.setItem('onemoi.otp', JSON.stringify({ destination: this.f.mobile, purpose: 'register', loginAs: vendor ? 'tenant' : 'individual', sent }));
      await this.router.navigateByUrl('/otp');
    } catch (e) {
      const a = apiError(e);
      this.error.set(a.message);
      this.errors.set(a.errors ?? {});
    } finally { this.busy.set(false); }
  }
}
