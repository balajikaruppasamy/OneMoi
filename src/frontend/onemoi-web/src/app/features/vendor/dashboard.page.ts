import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { VendorDashboard } from '../../core/models';
import { InrPipe, PickPipe, ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { CanDirective } from '../../core/guards';
import { P } from '../../core/permissions';

@Component({
  selector: 'mk-vendor-dashboard',
  imports: [RouterLink, DatePipe, InrPipe, PickPipe, IconComponent, CanDirective],
  template: `
  @if (auth.profile()?.tenantStatus === 'Pending') {
    <div class="alert warn mb-4"><mk-icon name="clock" /><div><b>Waiting for OneMoi approval.</b> You can set up your company profile, logo, masters, operators and staff now. Creating functions and Moi entry open after approval.</div></div>
  }
  <div class="page-head"><div><h1>Today</h1><p class="muted">{{ today | date: 'EEEE, d MMMM y' }}</p></div>
    <a *mkCan="P.functionsManage" class="btn btn-primary" routerLink="/vendor/functions/new"><mk-icon name="plus" />New function</a></div>

  @if (d(); as d) {
    <div class="grid grid-4">
      <div class="stat"><div class="stat-label"><span class="stat-icon"><mk-icon name="rupee" [size]="18" /></span>Collected today</div><div class="stat-value">{{ d.todayCollected | inr }}</div><div class="xs muted">Month: {{ d.monthCollected | inr }}</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon gold"><mk-icon name="receipt" [size]="18" /></span>Entries today</div><div class="stat-value">{{ d.todayEntries }}</div><div class="xs muted">{{ d.functionsToday }} functions today · {{ d.upcomingFunctions }} upcoming</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon green"><mk-icon name="cash" [size]="18" /></span>Cash · UPI</div><div class="stat-value" style="font-size:1.2rem">{{ d.todayCash | inr }} · {{ d.todayUpi | inr }}</div><div class="xs muted">Expenses paid out: {{ d.todayExpenses | inr }}</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon blue"><mk-icon name="users" [size]="18" /></span>Active operators</div><div class="stat-value">{{ d.activeOperators }}</div><a *mkCan="P.operatorsAssign" class="xs" routerLink="/vendor/operators">Assign to functions →</a></div>
    </div>

    <h2 class="mt-8 mb-4">Today's functions</h2>
    @if (d.today.length === 0) {
      <div class="card empty"><div class="empty-art"><mk-icon name="calendar" [size]="40" /></div><h3>No functions today</h3><p>Create a function, then assign operators to its counters.</p></div>
    } @else {
      <div class="grid grid-3">
        @for (f of d.today; track f.id) {
          <a class="card card-pad stack-sm" [routerLink]="['/vendor/functions', f.id]" style="color:inherit;font-weight:400;text-decoration:none">
            <div class="row between"><span class="badge" [class.green]="f.status === 'Live'" [class.live]="f.status === 'Live'" [class.blue]="f.status === 'Scheduled'">@if (f.status === 'Live') {<span class="dot"></span>}{{ f.status }}</span><span class="badge">{{ f.typeName }}</span></div>
            <h3>{{ f.name | pick: f.nameTa }}</h3>
            <div class="small muted">{{ f.location }}</div>
            <div class="row between mt-2"><div><div class="xs muted">COLLECTED</div><div class="money" style="font-size:1.4rem">{{ f.collected | inr }}</div></div>
              <div class="center"><div class="xs muted">ENTRIES</div><div class="money" style="font-size:1.4rem">{{ f.entries }}</div></div></div>
            <div class="xs muted">Operators: {{ f.operators.length ? f.operators.join(', ') : 'none assigned' }}</div>
          </a>
        }
      </div>
    }

    <div class="card mt-6">
      <div class="card-head"><h3>Latest entries</h3><button class="btn btn-ghost btn-sm" (click)="load()"><mk-icon name="refresh" [size]="16" />Refresh</button></div>
      <div class="list">
        @for (r of d.recent; track r.id) {
          <div class="list-item">
            <div class="icon-tile" [class.gold]="r.paymentMode === 'Cash'" [class.green]="r.paymentMode === 'Upi'" style="width:38px;height:38px"><mk-icon [name]="r.paymentMode === 'Upi' ? 'upi' : 'cash'" [size]="18" /></div>
            <div class="grow"><div class="small" style="font-weight:700">{{ r.name | pick: r.nameTa }} @if (r.isHighlighted) {<span class="badge gold">★</span>}</div>
              <div class="xs muted">{{ r.city }} · {{ r.functionName }} · {{ r.operatorName ?? 'Vendor desk' }} · {{ r.entryAt | date: 'd MMM, h:mm a' }}</div></div>
            <b class="num">{{ r.amount | inr }}</b>
          </div>
        } @empty { <div class="empty"><p>No entries yet.</p></div> }
      </div>
    </div>
  } @else {
    <div class="grid grid-4">@for (i of [1,2,3,4]; track i) {<div class="skeleton" style="height:110px;border-radius:22px"></div>}</div>
  }`
})
export class VendorDashboardPage implements OnInit {
  protected auth = inject(AuthService);
  protected P = P;
  private api = inject(ApiService);
  private toast = inject(ToastService);
  protected d = signal<VendorDashboard | null>(null);
  protected today = new Date();
  ngOnInit() { this.load(); }
  async load() {
    try { this.d.set(await this.api.get<VendorDashboard>('/api/vendor/dashboard')); } catch (e) { this.toast.err(apiError(e).message); }
  }
}
