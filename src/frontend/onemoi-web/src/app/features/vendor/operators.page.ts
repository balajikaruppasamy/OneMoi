import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { P } from '../../core/permissions';
import { AssignmentBoard, OperatorCredentials, OperatorDto } from '../../core/models';
import { PickPipe, TimePipe, ToastService, maskMobile } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

/** Operators + the day's assignment board (3 operators × 3 functions on the same day). */
@Component({
  selector: 'mk-operators',
  imports: [FormsModule, DatePipe, PickPipe, TimePipe, IconComponent, TamilFieldComponent],
  template: `
  <div class="page-head"><div><h1>Operators</h1><p class="muted">Counter staff and where they work</p></div>
    @if (canManage) { <button class="btn btn-primary" (click)="openForm()"><mk-icon name="plus" />Add operator</button> }</div>

  <div class="tabs">
    <button [class.active]="tab() === 'board'" (click)="tab.set('board')">Assign to functions</button>
    <button [class.active]="tab() === 'list'" (click)="tab.set('list')">All operators <span class="count">{{ ops().length }}</span></button>
  </div>

  @if (tab() === 'board') {
    <div class="row wrap mt-4">
      <div class="field"><label class="label">Date</label><input class="input" type="date" [(ngModel)]="date" (ngModelChange)="loadBoard()" style="width:auto" /></div>
      <div class="alert grow"><mk-icon name="info" /><div class="small"><b>Tap an operator, then tap an empty counter.</b> Operators can log in only to their function and counter, during its time window.</div></div>
    </div>
    @if (board(); as b) {
      <div class="grid mt-4" style="grid-template-columns:minmax(0,260px) minmax(0,1fr);align-items:start" [style.grid-template-columns]="narrow ? '1fr' : null">
        <div class="card card-pad">
          <div class="row between mb-2"><h3>Operator pool</h3><span class="badge">{{ free(b) }} free</span></div>
          @for (o of b.operators; track o.id) {
            <button class="list-item link-btn" style="width:100%;text-align:left;border-radius:12px;color:inherit;font-weight:400"
              [style.background]="picked() === o.id ? 'var(--brand-soft)' : null" [style.opacity]="o.assignedFunctionId ? .5 : 1"
              [disabled]="!!o.assignedFunctionId || !canAssign" (click)="picked.set(picked() === o.id ? null : o.id)">
              <div class="avatar sm">{{ o.name.slice(0, 2).toUpperCase() }}</div>
              <div class="grow"><div class="small" style="font-weight:700">{{ o.name }}</div><div class="xs muted">{{ o.code }} · {{ mask(o.mobile) }}</div></div>
              @if (o.assignedFunctionId) {<span class="badge green">Assigned</span>}
            </button>
          }
        </div>
        <div class="grid grid-3" style="align-items:start">
          @for (f of b.functions; track f.id) {
            <div class="card">
              <div class="card-head" style="flex-direction:column;align-items:flex-start;gap:4px">
                <span class="badge" [class.green]="f.status === 'Live'">{{ f.status }}</span>
                <h3>{{ f.name | pick: f.nameTa }}</h3><div class="xs muted">{{ f.typeName }} · {{ f.location }} · {{ f.startTime | hm }}–{{ f.endTime | hm }}</div>
              </div>
              <div class="card-body stack-sm">
                @for (c of f.counters; track c.id) {
                  @if (c.operatorId) {
                    <div class="row" style="padding:10px;border-radius:14px;background:var(--green-soft)">
                      <span class="badge green">C{{ c.number }}</span><div class="grow small"><b>{{ c.operatorName }}</b><div class="xs muted">{{ c.operatorCode }}</div></div>
                      @if (canAssign) { <button class="btn btn-ghost btn-icon btn-sm" title="Remove" (click)="unassign(c.assignmentId!)"><mk-icon name="x" [size]="16" /></button> }
                    </div>
                  } @else {
                    <button class="row link-btn" style="width:100%;padding:10px;border-radius:14px;border:2px dashed var(--border);color:var(--muted);font-weight:600"
                      [style.border-color]="picked() ? 'var(--brand)' : null" (click)="assign(f.id, c.id)">
                      <span class="badge">C{{ c.number }}</span><span class="grow small" style="text-align:left">{{ picked() ? 'Tap to assign here' : 'Empty counter' }}</span><mk-icon name="plus" [size]="18" />
                    </button>
                  }
                }
              </div>
            </div>
          } @empty {
            <div class="card empty"><h3>No functions on this date</h3><p>Pick another date or create a function.</p></div>
          }
        </div>
      </div>
    }
  } @else {
    <div class="card mt-4"><div class="table-wrap"><table class="table table-stack">
      <thead><tr><th>Operator</th><th>ID</th><th>Mobile</th><th>Today</th><th class="r">Entries</th><th>Last login</th><th>Status</th><th></th></tr></thead>
      <tbody>
        @for (o of ops(); track o.id) {
          <tr>
            <td data-label="Operator"><b>{{ o.name }}</b><div class="xs muted ta">{{ o.nameTa }}</div></td>
            <td data-label="ID"><span class="badge">{{ o.code }}</span></td>
            <td data-label="Mobile">{{ o.mobile }}</td>
            <td data-label="Today">{{ o.todayAssignment ?? '—' }}</td>
            <td data-label="Entries" class="r num">{{ o.totalEntries }}</td>
            <td data-label="Last login" class="small">{{ o.lastLoginAt ? (o.lastLoginAt | date: 'd MMM, h:mm a') : '—' }}</td>
            <td data-label="Status"><span class="badge" [class.green]="o.isActive">{{ o.isActive ? 'Active' : 'Disabled' }}</span></td>
            <td class="r" data-label="">@if (canManage) {<button class="btn btn-ghost btn-sm" (click)="openForm(o)">Edit</button><button class="btn btn-ghost btn-sm" (click)="resetPin(o)">Reset PIN</button>}</td>
          </tr>
        }
      </tbody>
    </table></div></div>
  }

  @if (form()) {
    <div class="modal-backdrop open" (click)="form.set(false)">
      <div class="modal" style="max-width:480px" (click)="$event.stopPropagation()">
        <div class="modal-head"><h2>{{ editId ? 'Edit operator' : 'Add operator' }}</h2><button class="btn btn-ghost btn-icon btn-sm" (click)="form.set(false)"><mk-icon name="x" /></button></div>
        <div class="modal-body stack">
          <mk-tamil-field label="Full name" [required]="true" [(en)]="of.name" [(ta)]="of.nameTa" [invalid]="!!err('name')" [errorText]="err('name')" />
          <div class="field"><label class="label">Mobile <span class="req">*</span></label><div class="input-group"><span class="addon">+91</span><input class="input" type="tel" maxlength="10" [(ngModel)]="of.mobile" /></div>
            @if (err('mobile')) {<span class="field-error">{{ err('mobile') }}</span>}</div>
          <label class="check"><input type="checkbox" [(ngModel)]="of.isActive" /> Active (can log in)</label>
        </div>
        <div class="modal-foot"><button class="btn btn-outline" (click)="form.set(false)">Cancel</button><button class="btn btn-primary" (click)="saveOp()">Save</button></div>
      </div>
    </div>
  }
  @if (cred(); as c) {
    <div class="modal-backdrop open">
      <div class="modal" style="max-width:420px">
        <div class="modal-head"><h2>Operator login details</h2></div>
        <div class="modal-body stack">
          <p class="small muted">Share these with the operator. <b>The PIN is shown only once</b> — it is stored encrypted.</p>
          <div class="credential-box stack-sm">
            <div class="row between"><span>Vendor code</span><b>{{ c.tenantCode }}</b></div>
            <div class="row between"><span>Operator ID</span><b>{{ c.operatorCode }}</b></div>
            <div class="row between"><span>PIN</span><b>{{ c.pin }}</b></div>
          </div>
        </div>
        <div class="modal-foot"><button class="btn btn-primary" (click)="cred.set(null)">Done</button></div>
      </div>
    </div>
  }`
})
export class OperatorsPage implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  protected canManage = this.auth.can(P.operatorsManage);
  protected canAssign = this.auth.can(P.operatorsAssign);
  protected tab = signal<'board' | 'list'>('board');
  protected ops = signal<OperatorDto[]>([]);
  protected board = signal<AssignmentBoard | null>(null);
  protected picked = signal<number | null>(null);
  protected form = signal(false);
  protected cred = signal<OperatorCredentials | null>(null);
  protected errors = signal<Record<string, string[]>>({});
  protected mask = maskMobile;
  protected narrow = window.innerWidth < 960;
  date = new Date().toISOString().slice(0, 10);
  editId: number | null = null;
  of: any = {};

  ngOnInit() { this.loadOps(); this.loadBoard(); }
  err(k: string) { return this.errors()[k]?.[0] ?? ''; }
  free(b: AssignmentBoard) { return b.operators.filter(o => !o.assignedFunctionId).length; }

  async loadOps() { this.ops.set(await this.api.get<OperatorDto[]>('/api/operators')); }
  async loadBoard() { this.board.set(await this.api.get<AssignmentBoard>('/api/operators/board', { date: this.date })); }

  async assign(functionId: number, counterId: number) {
    const op = this.picked();
    if (!op) { this.toast.show('Select an operator from the pool first'); return; }
    try { await this.api.post('/api/operators/assignments', { functionId, counterId, operatorId: op }); this.picked.set(null); this.toast.ok('Operator assigned'); await this.loadBoard(); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
  async unassign(id: number) {
    try { await this.api.delete(`/api/operators/assignments/${id}`); await this.loadBoard(); } catch (e) { this.toast.err(apiError(e).message); }
  }

  openForm(o?: OperatorDto) {
    this.editId = o?.id ?? null;
    this.of = o ? { name: o.name, nameTa: o.nameTa ?? '', mobile: o.mobile, isActive: o.isActive } : { name: '', nameTa: '', mobile: '', isActive: true };
    this.errors.set({}); this.form.set(true);
  }
  async saveOp() {
    try {
      if (this.editId) { await this.api.put(`/api/operators/${this.editId}`, this.of); this.toast.ok('Saved'); }
      else this.cred.set(await this.api.post<OperatorCredentials>('/api/operators', this.of));
      this.form.set(false); await this.loadOps(); await this.loadBoard();
    } catch (e) { const a = apiError(e); this.errors.set(a.errors ?? {}); if (!a.errors) this.toast.err(a.message); }
  }
  async resetPin(o: OperatorDto) {
    if (!confirm(`Generate a new PIN for ${o.name}?`)) return;
    try { this.cred.set(await this.api.post<OperatorCredentials>(`/api/operators/${o.id}/reset-pin`)); } catch (e) { this.toast.err(apiError(e).message); }
  }
}
