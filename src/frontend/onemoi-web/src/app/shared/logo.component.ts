import { Component, input } from '@angular/core';

let uid = 0;

/** OneMoi mark (envelope + ₹ seal) with optional wordmark. animated = loader animation. */
@Component({
  selector: 'mk-logo',
  template: `
    <span class="logo">
      <svg class="logo-mark" [class.mk-anim]="animated()" [style.width.px]="size()" [style.height.px]="size()" viewBox="0 0 120 120" role="img" aria-label="OneMoi">
        <defs>
          <linearGradient [attr.id]="id + 'b'" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#A8264F"/><stop offset="1" stop-color="#6A0F28"/></linearGradient>
          <linearGradient [attr.id]="id + 'c'" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFC94A"/><stop offset="1" stop-color="#E08E00"/></linearGradient>
        </defs>
        <rect width="120" height="120" rx="28" [attr.fill]="'url(#' + id + 'b)'"/>
        <g fill="#F2A516" opacity=".55"><circle cx="20" cy="20" r="2.4"/><circle cx="100" cy="20" r="2.4"/><circle cx="20" cy="100" r="2.4"/><circle cx="100" cy="100" r="2.4"/></g>
        <rect class="mk-env" x="22" y="34" width="76" height="56" rx="10" fill="#FFF9F0"/>
        <path class="mk-m" d="M34 80V48l26 20 26-20v32" fill="none" stroke="#8E1B3A" stroke-width="7.5" stroke-linecap="round" stroke-linejoin="round"/>
        <g class="mk-coin">
          <circle cx="60" cy="68" r="13" [attr.fill]="'url(#' + id + 'c)'" stroke="#FFF9F0" stroke-width="3.5"/>
          <path d="M55 62.5h10M55 66h10M55 62.5h3.2a3.6 3.6 0 0 1 0 7.2H55l7 6" fill="none" stroke="#6A0F28" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round"/>
        </g>
      </svg>
      @if (word()) {
        <span><div class="logo-word">One<b>Moi</b></div>@if (sub()) {<div class="logo-sub">ஒரே மொய் அடையாளம்</div>}</span>
      }
    </span>`
})
export class LogoComponent {
  size = input(40);
  word = input(true);
  sub = input(true);
  animated = input(false);
  protected id = 'om' + ++uid;
}
