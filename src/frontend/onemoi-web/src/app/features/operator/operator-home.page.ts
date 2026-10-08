import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MyAssignment } from '../../core/models';
import { PickPipe, dateOnly } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';

/** Operator landing: which function and counter am I working at? */
@Component({
  selector: 'mk-operator-home',
  imports: [RouterLink, DatePipe, PickPipe, IconComponent],
  template: `
  <div class="page-head"><div><h1>வணக்கம், {{ auth.profile()?.name }} 👋</h1><p class="muted">{{ auth.profile()?.tenantName }} · Moi operator</p></div></div>
  <div class="grid grid-2">
    @for (a of rows(); track a.assignmentId) {
      <div class="card card-pad stack-sm">
        <div class="row between"><span class="badge" [class.green]="a.isOpenNow">{{ a.isOpenNow ? 'Open now' : 'Not open' }}</span><span class="badge brand">{{ a.counterName }}</span></div>
        <h2>{{ a.functionName | pick: a.functionNameTa }}</h2>
        <div class="small muted">{{ a.location }} · {{ d(a.functionDate) | date: 'EEE, d MMM' }}</div>
        <div class="xs muted">Login window {{ a.validFrom | date: 'd MMM h:mm a' }} – {{ a.validTo | date: 'd MMM h:mm a' }}</div>
        @if (a.isOpenNow) { <a class="btn btn-primary btn-lg mt-2" [routerLink]="['/operator/entry', a.functionId]"><mk-icon name="receipt" />Start Moi entry</a> }
      </div>
    } @empty {
      <div class="card empty"><div class="empty-art"><mk-icon name="clock" [size]="40" /></div><h3>No assignment</h3><p>Your vendor has not assigned you to a function counter yet.</p></div>
    }
  </div>`
})
export class OperatorHomePage implements OnInit {
  protected auth = inject(AuthService);
  private api = inject(ApiService);
  protected rows = signal<MyAssignment[]>([]);
  protected d = dateOnly;
  async ngOnInit() {
    // validFrom/validTo are local times without zone; show them as-is
    this.rows.set(await this.api.get<MyAssignment[]>('/api/operator/assignments'));
  }
}
