import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MyMoiItem, MyMoiResponse } from '../../core/models';
import { InrPipe, PickPipe, ToastService, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';

/** The individual's lifetime Moi history across EVERY vendor — the heart of OneMoi. */
@Component({
  selector: 'mk-my-moi',
  imports: [FormsModule, DatePipe, InrPipe, PickPipe, IconComponent],
  template: `
  <div class="grid grid-main">
    <div class="stack">
      <div class="hero-card">
        <div class="small muted">Total Moi given · {{ year() ?? 'All time' }}</div>
        <div class="money" style="font-size:2.6rem">{{ (data()?.total ?? 0) | inr }}</div>
        <div class="grid grid-3 mt-4" style="gap:10px">
          <div style="background:rgba(255,249,240,.1);border-radius:14px;padding:10px"><b>{{ data()?.count ?? 0 }}</b><div class="xs muted">Functions</div></div>
          <div style="background:rgba(255,249,240,.1);border-radius:14px;padding:10px"><b>{{ data()?.vendorCount ?? 0 }}</b><div class="xs muted">Moi vendors</div></div>
          <div style="background:rgba(255,249,240,.1);border-radius:14px;padding:10px"><b>{{ avg() | inr }}</b><div class="xs muted">Average</div></div>
        </div>
      </div>

      <div class="chips scroll">
        @for (y of years(); track y) { <button class="chip" [class.active]="year() === y" (click)="setYear(y)">{{ y }}</button> }
        <button class="chip" [class.active]="year() === null" (click)="setYear(null)">All time</button>
      </div>
      <input class="input" [(ngModel)]="q" placeholder="Search function, vendor or place" />

      @for (it of filtered(); track it.entryId) {
        <div class="card card-pad row" style="align-items:flex-start">
          <div class="center" style="width:48px;flex:none"><b style="font-size:1.25rem">{{ dd(it.functionDate) | date: 'd' }}</b><div class="xs muted">{{ dd(it.functionDate) | date: 'MMM yy' }}</div></div>
          <div class="grow">
            <div style="font-weight:750">{{ it.functionName | pick: it.functionNameTa }}</div>
            <div class="small muted">{{ it.functionType | pick: it.functionTypeTa }} · {{ it.location }}</div>
            <div class="row wrap mt-1" style="gap:6px">
              <span class="badge brand">via {{ it.vendorName | pick: it.vendorNameTa }}</span><span class="badge">{{ it.paymentMode }}</span>
              @if (it.isHighlighted) { <span class="badge gold">★ {{ it.category | pick: it.categoryTa }}</span> }
              @for (g of it.gifts; track g) { <span class="badge">🎁 {{ g }}</span> }
            </div>
            <div class="xs muted mt-1">Recorded as "{{ it.nameAsWritten }}" <span class="ta">{{ it.nameTaAsWritten }}</span> · {{ it.receiptNo }}</div>
          </div>
          <div class="money" style="font-size:1.15rem">{{ it.amount | inr }}</div>
        </div>
      } @empty {
        <div class="card empty"><div class="empty-art"><mk-icon name="wallet" [size]="40" /></div><h3>No Moi found</h3>
          <p>Moi recorded with your mobile number {{ auth.profile()?.mobile }} by any vendor appears here automatically.</p></div>
      }
    </div>

    <div class="stack">
      <div class="card">
        <div class="card-head"><h3>By Moi vendor</h3></div>
        <div class="card-body stack-sm">
          @for (v of data()?.byVendor ?? []; track v.vendor) {
            <div><div class="row between small"><b>{{ v.vendor }}</b><b class="num">{{ v.amount | inr }}</b></div>
              <div class="progress mt-1"><span [style.width.%]="(v.amount / (data()!.total || 1)) * 100"></span></div>
              <div class="xs muted">{{ v.count }} function(s)</div></div>
          } @empty { <p class="small muted">No records</p> }
        </div>
      </div>
      <div class="alert"><mk-icon name="shield" /><div class="small">Vendors see only Moi given at <b>their own</b> functions. Only you can see this combined history.</div></div>
    </div>
  </div>`
})
export class MyMoiPage implements OnInit {
  protected auth = inject(AuthService);
  private api = inject(ApiService);
  private toast = inject(ToastService);
  protected data = signal<MyMoiResponse | null>(null);
  protected years = signal<number[]>([]);
  protected year = signal<number | null>(new Date().getFullYear());
  protected dd = dateOnly;
  q = '';
  protected avg = computed(() => (this.data()?.count ? Math.round(this.data()!.total / this.data()!.count) : 0));

  async ngOnInit() { await this.load(); if (!this.years().includes(this.year()!) && this.years().length) this.setYear(this.years()[0]); }
  setYear(y: number | null) { this.year.set(y); this.load(); }
  async load() {
    try {
      const r = await this.api.get<MyMoiResponse>('/api/me/moi', { year: this.year() ?? '' });
      this.data.set(r);
      if (this.year() === null || !this.years().length) this.years.set(r.years.length ? r.years : [new Date().getFullYear()]);
    } catch (e) { this.toast.err(apiError(e).message); }
  }
  filtered(): MyMoiItem[] {
    const q = this.q.trim().toLowerCase();
    const items = this.data()?.items ?? [];
    return q ? items.filter(i => `${i.functionName} ${i.functionNameTa} ${i.vendorName} ${i.location}`.toLowerCase().includes(q)) : items;
  }
}
