import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { FunctionReport } from '../../core/models';
import { InrPipe, ToastService, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';

/** Function Moi book — printable (PDF via browser) and downloadable (CSV/Excel) in English or Tamil. */
@Component({
  selector: 'mk-report',
  imports: [RouterLink, DatePipe, InrPipe, IconComponent],
  template: `
  <div class="page-head no-print">
    <div><a class="small" [routerLink]="back()">← Back</a><h1 class="mt-1">Moi report</h1></div>
    <div class="row wrap">
      <div class="segmented" style="width:auto"><button [class.active]="lang() === 'en'" (click)="lang.set('en')">English</button><button class="ta" [class.active]="lang() === 'ta'" (click)="lang.set('ta')">தமிழ்</button></div>
      @if (canExport) { <button class="btn btn-outline" (click)="csv()"><mk-icon name="download" />Excel (CSV)</button> }
      <button class="btn btn-primary" (click)="print()"><mk-icon name="printer" />Print / PDF</button>
    </div>
  </div>

  @if (r(); as r) {
    <div class="card card-pad">
      <div class="row" style="align-items:flex-start;gap:16px">
        @if (logo()) { <img class="tenant-logo lg" [src]="logo()" alt="" /> }
        <div class="grow">
          <h2>{{ t(r.header.vendorName, r.header.vendorNameTa) }}</h2>
          <div class="small muted">{{ r.header.vendorAddress }} · {{ r.header.vendorMobile }}</div>
          @if (r.header.receiptHeader) {<div class="small muted">{{ r.header.receiptHeader }}</div>}
        </div>
        <div style="text-align:right"><span class="badge brand num">{{ r.header.code }}</span></div>
      </div>
      <hr style="border:0;border-top:1px dashed var(--border);margin:16px 0">
      <h1 [class.ta]="lang() === 'ta'">{{ t(r.header.functionName, r.header.functionNameTa) }}</h1>
      <div class="row wrap mt-2 small" style="gap:18px">
        <span><b>{{ L('Type', 'வகை') }}:</b> {{ t(r.header.functionType, r.header.functionTypeTa) }}</span>
        <span><b>{{ L('Owner', 'விழா நடத்துபவர்') }}:</b> {{ t(r.header.ownerName, r.header.ownerNameTa) }}</span>
        <span><b>{{ L('Venue', 'இடம்') }}:</b> {{ t(r.header.location, r.header.locationTa) }}</span>
        <span><b>{{ L('Date', 'தேதி') }}:</b> {{ d(r.header.functionDate) | date: 'dd-MM-yyyy' }}</span>
      </div>

      <div class="grid grid-4 mt-4">
        <div class="stat"><div class="stat-label">{{ L('Total Moi', 'மொத்த மொய்') }}</div><div class="stat-value">{{ r.total | inr }}</div><div class="xs muted">{{ r.count }} {{ L('entries', 'பதிவுகள்') }}</div></div>
        <div class="stat"><div class="stat-label">{{ L('Cash', 'ரொக்கம்') }}</div><div class="stat-value">{{ r.cash | inr }}</div></div>
        <div class="stat"><div class="stat-label">{{ L('Expenses', 'செலவு') }}</div><div class="stat-value">{{ r.expenses | inr }}</div></div>
        <div class="stat"><div class="stat-label">{{ L('Net cash', 'கையிருப்பு') }}</div><div class="stat-value">{{ r.netCash | inr }}</div></div>
      </div>

      <div class="table-wrap mt-4"><table class="table">
        <thead><tr><th>{{ L('S.No', 'வ.எண்') }}</th><th>{{ L('Name', 'பெயர்') }}</th><th>{{ L('Spouse', 'துணைவர்') }}</th><th>{{ L('City', 'ஊர்') }}</th>
          <th>{{ L('Category', 'வகை') }}</th><th>{{ L('Gifts', 'பரிசு') }}</th><th>{{ L('Mode', 'முறை') }}</th><th class="r">{{ L('Amount', 'தொகை') }}</th></tr></thead>
        <tbody>
          @for (x of r.rows; track x.serialNo) {
            <tr [class.row-highlight]="x.isHighlighted">
              <td class="num">{{ x.serialNo }}</td>
              <td [class.ta]="lang() === 'ta'"><b>{{ x.initial }} {{ t(x.name, x.nameTa) }}</b>@if (x.work) {<div class="xs muted">{{ x.work }}</div>}</td>
              <td [class.ta]="lang() === 'ta'">{{ x.spouseInitial }} {{ t(x.spouseName, x.spouseNameTa) }}</td>
              <td [class.ta]="lang() === 'ta'">{{ t(x.city, x.cityTa) }}</td>
              <td>@if (x.category) { <span>{{ x.isHighlighted ? '★ ' : '' }}{{ t(x.category, x.categoryTa) }}</span> }</td>
              <td class="small">{{ x.gifts }}</td>
              <td>{{ x.paymentMode }}</td>
              <td class="r money">{{ x.amount | inr }}</td>
            </tr>
          }
        </tbody>
        <tfoot><tr><td colspan="7" class="r"><b>{{ L('Total', 'மொத்தம்') }}</b></td><td class="r money">{{ r.total | inr }}</td></tr></tfoot>
      </table></div>
      @if (r.header.receiptFooter) { <p class="center muted mt-4">{{ r.header.receiptFooter }}</p> }
    </div>
  }`
})
export class ReportPage implements OnInit {
  id = input.required<string>();
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  protected r = signal<FunctionReport | null>(null);
  protected lang = signal<'en' | 'ta'>('en');
  protected d = dateOnly;
  /** Function hosts may always download their own report; vendor staff need reports.export */
  protected canExport = this.auth.area() === 'individual' || this.auth.can('reports.export');
  protected logo = () => this.api.file(this.r()?.header.vendorLogo);
  protected back = () => (this.auth.area() === 'individual' ? '/me/functions' : `/vendor/functions/${this.id()}`);

  async ngOnInit() {
    try { this.r.set(await this.api.get<FunctionReport>(`/api/reports/functions/${this.id()}`)); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
  t(en?: string | null, ta?: string | null) { return this.lang() === 'ta' && ta ? ta : (en ?? ''); }
  L(en: string, ta: string) { return this.lang() === 'ta' ? ta : en; }
  print() { window.print(); }
  csv() { this.api.download(`/api/reports/functions/${this.id()}/csv`, { lang: this.lang() }).catch(e => this.toast.err(apiError(e).message)); }
}
