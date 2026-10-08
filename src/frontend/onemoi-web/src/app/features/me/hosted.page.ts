import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { HostedFunction } from '../../core/models';
import { InrPipe, PickPipe, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';

/** Functions where this person is the owner (matched by their verified mobile). */
@Component({
  selector: 'mk-hosted',
  imports: [RouterLink, DatePipe, InrPipe, PickPipe, IconComponent],
  template: `
  <div class="page-head"><div><h1>My functions</h1><p class="muted">Functions where you are the owner — see the Moi received, live</p></div></div>
  <div class="grid grid-3">
    @for (f of rows(); track f.id) {
      <a class="card card-pad stack-sm" [routerLink]="['/me/functions', f.id, 'report']" style="color:inherit;font-weight:400;text-decoration:none">
        <div class="row between"><span class="badge" [class.green]="f.status === 'Live'">{{ f.status }}</span><span class="xs muted">{{ d(f.functionDate) | date: 'd MMM y' }}</span></div>
        <h3>{{ f.name | pick: f.nameTa }}</h3>
        <div class="small muted">{{ f.typeName }} · {{ f.location }}</div>
        <div class="row between mt-2"><div><div class="xs muted">RECEIVED</div><div class="money" style="font-size:1.5rem">{{ f.total | inr }}</div></div>
          <div class="center"><div class="xs muted">ENTRIES</div><b>{{ f.entries }}</b></div></div>
        <div class="xs muted">Managed by {{ f.vendorName }}</div>
      </a>
    } @empty {
      <div class="card empty"><div class="empty-art"><mk-icon name="calendar" [size]="40" /></div><h3>No functions</h3><p>When a Moi vendor creates a function with your mobile number as owner, it shows here.</p></div>
    }
  </div>`
})
export class HostedPage implements OnInit {
  private api = inject(ApiService);
  protected rows = signal<HostedFunction[]>([]);
  protected d = dateOnly;
  async ngOnInit() { this.rows.set(await this.api.get<HostedFunction[]>('/api/me/hosted-functions')); }
}
