import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { FunctionListItem } from '../../core/models';
import { InrPipe, PickPipe, TimePipe, ToastService, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { CanDirective } from '../../core/guards';
import { P } from '../../core/permissions';

@Component({
  selector: 'mk-functions-list',
  imports: [FormsModule, RouterLink, DatePipe, InrPipe, PickPipe, TimePipe, IconComponent, CanDirective],
  template: `
  <div class="page-head"><div><h1>Functions</h1><p class="muted">All events managed by your Moi service</p></div>
    <a *mkCan="P.functionsManage" class="btn btn-primary" routerLink="/vendor/functions/new"><mk-icon name="plus" />New function</a></div>

  <div class="tabs">
    @for (t of tabs; track t) { <button [class.active]="status() === t" (click)="status.set(t); load()">{{ t || 'All' }}</button> }
  </div>
  <div class="row mt-4 wrap">
    <div class="input-group grow" style="min-width:220px"><span class="addon"><mk-icon name="search" [size]="18" /></span>
      <input class="input" [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search name, Tamil name, owner, mobile, code" /></div>
    <button class="btn btn-outline" (click)="load()">Search</button>
  </div>

  <div class="card mt-4"><div class="table-wrap">
    <table class="table table-stack">
      <thead><tr><th>Function</th><th>Date</th><th>Owner</th><th>Code</th><th class="r">Entries</th><th class="r">Collected</th><th>Status</th><th></th></tr></thead>
      <tbody>
        @for (f of rows(); track f.id) {
          <tr class="clickable" [routerLink]="['/vendor/functions', f.id]">
            <td data-label="Function"><div><b>{{ f.name | pick: f.nameTa }}</b><div class="xs muted ta">{{ f.nameTa }}</div><div class="xs muted">{{ f.typeName | pick: f.typeNameTa }} · {{ f.location }}</div></div></td>
            <td data-label="Date">{{ d(f.functionDate) | date: 'd MMM y' }}<div class="xs muted">{{ f.startTime | hm }}</div></td>
            <td data-label="Owner">{{ f.ownerName | pick: f.ownerNameTa }}<div class="xs muted">{{ f.ownerMobile }}</div></td>
            <td data-label="Code"><span class="badge brand num">{{ f.code }}</span></td>
            <td data-label="Entries" class="r num">{{ f.entryCount }}</td>
            <td data-label="Collected" class="r money">{{ f.collected | inr }}</td>
            <td data-label="Status"><span class="badge" [class.green]="f.status === 'Live'" [class.blue]="f.status === 'Scheduled'" [class.red]="f.status === 'Cancelled'">{{ f.status }}</span></td>
            <td class="r" data-label=""><mk-icon name="chevron" [size]="18" /></td>
          </tr>
        } @empty {
          <tr><td colspan="8"><div class="empty"><div class="empty-art"><mk-icon name="calendar" [size]="40" /></div><h3>No functions</h3><p>Create a function to get its code, counters and operators.</p></div></td></tr>
        }
      </tbody>
    </table>
  </div></div>`
})
export class FunctionsListPage implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  protected P = P;
  protected tabs = ['', 'Live', 'Scheduled', 'Closed', 'Draft', 'Cancelled'];
  protected status = signal('');
  protected rows = signal<FunctionListItem[]>([]);
  protected d = dateOnly;
  search = '';
  ngOnInit() { this.load(); }
  async load() {
    try { this.rows.set(await this.api.get<FunctionListItem[]>('/api/functions', { status: this.status(), search: this.search })); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
}
