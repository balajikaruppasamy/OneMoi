import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Expense, FunctionDetail, FunctionSummary, MoiEntry } from '../../core/models';
import { InrPipe, PickPipe, TimePipe, ToastService, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { ExpensePanelComponent } from './expense-panel.component';
import { CanDirective } from '../../core/guards';
import { P } from '../../core/permissions';

@Component({
  selector: 'mk-function-detail',
  imports: [FormsModule, RouterLink, DatePipe, InrPipe, PickPipe, TimePipe, IconComponent, ExpensePanelComponent, CanDirective],
  template: `
  @if (f(); as f) {
    <div class="page-head">
      <div><a class="small" routerLink="/vendor/functions">← Functions</a>
        <h1 class="mt-1">{{ f.name | pick: f.nameTa }}</h1>
        <p class="muted ta">{{ f.nameTa }}</p>
        <div class="row wrap mt-2" style="gap:6px"><span class="badge" [class.green]="f.status === 'Live'" [class.blue]="f.status === 'Scheduled'">{{ f.status }}</span>
          <span class="badge">{{ f.typeName | pick: f.typeNameTa }}</span><span class="badge brand num">Code {{ f.code }}</span></div></div>
      <div class="row wrap">
        @if (f.status !== 'Closed' && f.status !== 'Cancelled' && auth.can(P.moiEntry)) { <a class="btn btn-primary" [routerLink]="['/vendor/functions', f.id, 'entry']"><mk-icon name="receipt" />Moi entry</a> }
        <a *mkCan="P.reportsView" class="btn btn-outline" [routerLink]="['/vendor/functions', f.id, 'report']"><mk-icon name="file" />Report</a>
        @if (auth.can(P.functionsManage)) {
          <a class="btn btn-outline" [routerLink]="['/vendor/functions', f.id, 'edit']"><mk-icon name="edit" />Edit</a>
          <select class="select" style="width:auto" [ngModel]="f.status" (ngModelChange)="setStatus($event)">
            @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
          </select>
        }
      </div>
    </div>

    <div class="grid grid-main">
      <div class="stack">
        @if (s(); as s) {
          <div class="grid grid-4">
            <div class="stat"><div class="stat-label">Collected</div><div class="stat-value">{{ s.totalCollected | inr }}</div><div class="xs muted">{{ s.entries }} entries · {{ s.reversedEntries }} reversed</div></div>
            <div class="stat"><div class="stat-label">Cash · UPI</div><div class="stat-value" style="font-size:1.15rem">{{ s.cash | inr }}<br>{{ s.upi | inr }}</div></div>
            <div class="stat"><div class="stat-label">Expenses paid</div><div class="stat-value" style="color:var(--red)">{{ s.totalExpenses | inr }}</div></div>
            <div class="stat"><div class="stat-label">Cash in hand</div><div class="stat-value" style="color:var(--green)">{{ s.cashInHand | inr }}</div><div class="xs muted">cash − cash expenses</div></div>
          </div>
        }

        <div class="tabs">
          <button [class.active]="tab() === 'entries'" (click)="tab.set('entries')">Moi entries</button>
          <button [class.active]="tab() === 'summary'" (click)="tab.set('summary')">Summary</button>
          @if (auth.can(P.expensesView)) { <button [class.active]="tab() === 'expenses'" (click)="tab.set('expenses')">Expenses <span class="count">{{ expenses().length }}</span></button> }
        </div>

        @switch (tab()) {
          @case ('entries') {
            <div class="row">
              <input class="input grow" [(ngModel)]="search" (keyup.enter)="loadEntries()" placeholder="Search name, Tamil name, mobile, city, receipt" />
              <label class="check" style="white-space:nowrap"><input type="checkbox" [(ngModel)]="highlightedOnly" (change)="loadEntries()" />★ only</label>
            </div>
            <div class="card"><div class="table-wrap"><table class="table table-stack">
              <thead><tr><th>#</th><th>Name</th><th>Spouse</th><th>City</th><th>Category</th><th>Mode</th><th class="r">Amount</th><th></th></tr></thead>
              <tbody>
                @for (e of entries(); track e.id) {
                  <tr [class.row-highlight]="e.isHighlighted" [style.opacity]="e.status === 'Reversed' ? .5 : 1">
                    <td data-label="#" class="num muted">{{ e.serialNo }}</td>
                    <td data-label="Name"><b>{{ e.initial }} {{ e.name | pick: e.nameTa }}</b><div class="xs muted ta">{{ e.nameTa }}</div><div class="xs muted">{{ e.mobile }}</div></td>
                    <td data-label="Spouse">{{ e.spouseInitial }} {{ e.spouseName | pick: e.spouseNameTa }}</td>
                    <td data-label="City">{{ e.city | pick: e.cityTa }}<div class="xs muted">{{ e.work }}</div></td>
                    <td data-label="Category">@if (e.categoryName) {<span class="badge" [class.gold]="e.isHighlighted">{{ e.isHighlighted ? '★ ' : '' }}{{ e.categoryName | pick: e.categoryNameTa }}</span>}
                      @if (e.gifts.length) {<div class="xs muted mt-1">🎁 {{ giftText(e) }}</div>}</td>
                    <td data-label="Mode"><span class="badge" [class.green]="e.paymentMode === 'Upi'">{{ e.paymentMode }}</span>
                      @if (e.denominations.length) {<div class="xs muted mt-1">{{ denomText(e) }}</div>}</td>
                    <td data-label="Amount" class="r money">@if (e.status === 'Reversed') {<s>{{ e.amount | inr }}</s><div class="xs" style="color:var(--red)">{{ e.reversalReason }}</div>} @else { {{ e.amount | inr }} }</td>
                    <td class="r" data-label="">@if (e.status === 'Active' && auth.can(P.moiReverseAny)) {<button class="btn btn-ghost btn-sm" (click)="reverse(e)">Reverse</button>}</td>
                  </tr>
                } @empty { <tr><td colspan="8"><div class="empty"><p>No Moi entries yet.</p></div></td></tr> }
              </tbody>
            </table></div></div>
          }
          @case ('summary') {
            @if (s(); as s) {
              <div class="grid grid-2">
                <div class="card"><div class="card-head"><h3>By counter</h3></div><div class="list">
                  @for (c of s.byCounter; track c.name) {<div class="list-item"><div class="grow">{{ c.name }}<div class="xs muted">{{ c.count }} entries</div></div><b>{{ c.amount | inr }}</b></div>}</div></div>
                <div class="card"><div class="card-head"><h3>By Moi category</h3></div><div class="list">
                  @for (c of s.byCategory; track c.name) {<div class="list-item"><div class="grow">{{ c.name | pick: c.nameTa }}<div class="xs muted">{{ c.count }} entries</div></div><b>{{ c.amount | inr }}</b></div>}</div></div>
                <div class="card"><div class="card-head"><h3>Cash notes collected</h3></div><div class="list">
                  @for (n of s.cashDenominations; track n.noteValue) {<div class="list-item"><div class="grow">₹{{ n.noteValue }} × {{ n.count }}</div><b>{{ n.noteValue * n.count | inr }}</b></div>}
                  @empty {<div class="list-item muted small">No denomination details entered.</div>}</div></div>
                <div class="card"><div class="card-head"><h3>★ Highlighted Moi</h3></div><div class="list">
                  @for (h of s.highlighted; track $index) {<div class="list-item"><span class="badge" [style.background]="h.color + '22'" [style.color]="h.color">{{ h.category | pick: h.categoryTa }}</span><div class="grow">{{ h.name | pick: h.nameTa }}</div><b>{{ h.amount | inr }}</b></div>}
                  @empty {<div class="list-item muted small">No Thaimaman / Seer entries yet.</div>}</div></div>
              </div>
            }
          }
          @case ('expenses') {
            <mk-expense-panel [functionId]="f.id" [canDelete]="auth.can(P.expensesDelete)" [canCreate]="auth.can(P.expensesCreate)" (changed)="loadAll()" />
          }
        }
      </div>

      <div class="stack">
        <div class="card card-pad">
          @if (logo(f.tenantLogo)) { <img class="tenant-logo lg mb-2" [src]="logo(f.tenantLogo)" alt="" /> }
          <dl class="kv">
            <dt>Date</dt><dd>{{ d(f.functionDate) | date: 'EEE, d MMM y' }}</dd>
            <dt>Time</dt><dd>{{ f.startTime | hm }} – {{ f.endTime | hm }}</dd>
            <dt>Owner</dt><dd>{{ f.ownerName }}<div class="ta muted small">{{ f.ownerNameTa }}</div></dd>
            <dt>Owner phone</dt><dd>{{ f.ownerMobile }}</dd>
            <dt>Location</dt><dd>{{ f.location }}<div class="ta muted small">{{ f.locationTa }}</div><div class="muted small">{{ f.address }} {{ f.city }}</div></dd>
            <dt>Guests</dt><dd>{{ f.expectedGuests ?? '—' }}</dd>
            <dt>Payments</dt><dd>{{ f.allowCash ? 'Cash' : '' }} {{ f.allowUpi ? '· UPI' : '' }}</dd>
          </dl>
          @if (f.otherDetails) { <p class="small muted mt-3">{{ f.otherDetails }}</p> }
        </div>
        <div class="card">
          <div class="card-head"><h3>Counters</h3><a *mkCan="P.operatorsAssign" class="small" routerLink="/vendor/operators">Assign →</a></div>
          <div class="list">
            @for (c of f.counters; track c.id) {
              <div class="list-item"><div class="icon-tile" style="width:36px;height:36px;font-weight:800;font-size:.8rem">C{{ c.number }}</div>
                <div class="grow">{{ c.name }}<div class="xs muted">{{ c.operatorName ? c.operatorName + ' (' + c.operatorCode + ')' : 'No operator' }}</div></div></div>
            }
          </div>
        </div>
      </div>
    </div>
  }`
})
export class FunctionDetailPage implements OnInit {
  id = input.required<string>();
  protected auth = inject(AuthService);
  protected P = P;
  private api = inject(ApiService);
  private toast = inject(ToastService);
  protected f = signal<FunctionDetail | null>(null);
  protected s = signal<FunctionSummary | null>(null);
  protected entries = signal<MoiEntry[]>([]);
  protected expenses = signal<Expense[]>([]);
  protected tab = signal<'entries' | 'summary' | 'expenses'>('entries');
  protected statuses = ['Draft', 'Scheduled', 'Live', 'Closed', 'Cancelled'];
  protected d = dateOnly;
  search = ''; highlightedOnly = false;

  ngOnInit() { this.loadAll(); }

  async loadAll() {
    try {
      this.f.set(await this.api.get<FunctionDetail>(`/api/functions/${this.id()}`));
      this.s.set(await this.api.get<FunctionSummary>(`/api/moi/functions/${this.id()}/summary`));
      if (this.auth.can(P.expensesView)) this.expenses.set(await this.api.get<Expense[]>(`/api/moi/functions/${this.id()}/expenses`));
      await this.loadEntries();
    } catch (e) { this.toast.err(apiError(e).message); }
  }
  async loadEntries() {
    this.entries.set(await this.api.get<MoiEntry[]>('/api/moi/entries', { functionId: this.id(), search: this.search, highlightedOnly: this.highlightedOnly }));
  }
  async setStatus(status: string) {
    try { this.f.set(await this.api.put<FunctionDetail>(`/api/functions/${this.id()}/status/${status}`)); this.toast.ok(`Status: ${status}`); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
  async reverse(e: MoiEntry) {
    const reason = prompt(`Reverse ${e.receiptNo} (₹${e.amount})?\nEnter the reason:`);
    if (!reason) return;
    try { await this.api.post(`/api/moi/entries/${e.id}/reverse`, { reason }); this.toast.ok('Entry reversed'); this.loadAll(); }
    catch (err) { this.toast.err(apiError(err).message); }
  }
  logo(p?: string) { return this.api.file(p); }
  giftText(e: MoiEntry) { return e.gifts.map(g => g.description).join(', '); }
  denomText(e: MoiEntry) { return e.denominations.map(d => `${d.noteValue}×${d.count}`).join(' + '); }
}
