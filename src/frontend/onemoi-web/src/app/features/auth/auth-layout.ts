import { Component } from '@angular/core';
import { LogoComponent } from '../../shared/logo.component';

/** Left brand panel used by login / OTP / register (hidden on mobile). */
@Component({
  selector: 'mk-auth-hero',
  imports: [LogoComponent],
  template: `
    <section class="auth-hero">
      <div class="on-dark"><mk-logo [size]="44" /></div>
      <div class="stack">
        <div class="ta-line">ஒரே Mobile Number — உங்கள் முழு மொய் வரலாறு</div>
        <h1>Every Moi you give. Every vendor. One login.</h1>
        <p style="opacity:.8;max-width:46ch">Moi vendors record at the counter, in English and Tamil. Each person sees their complete Moi history with just their mobile number.</p>
        <div class="ledger-demo mt-6">
          <div class="row between"><div class="row"><div class="avatar">RK</div><div><div style="font-weight:800">N. Ram · ராம்</div><div class="xs" style="color:#7A686E">+91 98xxx xx210</div></div></div><span class="badge green"><span class="dot"></span>Verified</span></div>
          <div class="ld-item mt-3"><div class="grow"><div style="font-weight:700">Manikandan Keda Vettu</div><div class="xs" style="color:#7A686E">via <b>JD Moi Tech</b> · Cash</div></div><div class="money">₹1,000</div></div>
          <div class="ld-item mt-2"><div class="grow"><div style="font-weight:700">Arun weds Divya</div><div class="xs" style="color:#7A686E">via <b>Meenakshi Moi</b> · UPI</div></div><div class="money">₹5,000</div></div>
          <div class="ld-total"><span>Total Moi given</span><span class="money" style="font-size:1.3rem">₹6,000</span></div>
        </div>
      </div>
      <div class="small" style="opacity:.7">OTP secured · Your history is visible only to you</div>
    </section>`,
  styles: [':host{display:contents}']
})
export class AuthHeroComponent {}
