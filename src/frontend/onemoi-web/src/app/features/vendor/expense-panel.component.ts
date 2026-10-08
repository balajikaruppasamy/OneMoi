import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { Expense, MasterItem } from '../../core/models';
import { InrPipe, PickPipe, ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

/**
 * Money handed out from the Moi collection while the function is going on
 * (e.g. host's brother takes ₹5,000 for food). Reduces cash in hand.
 */
@Component({
  selector: 'mk-expense-panel',
  imports: [FormsModule, DatePipe, InrPipe, PickPipe, IconComponent, TamilFieldComponent],
  template: `
  <div class="card">
    <div class="card-head"><h3>Function expenses</h3><b class="money" style="color:var(--red)">{{ total() | inr }}</b></div>
    @if (canCreate()) {
    <form class="card-body stack" (ngSubmit)="add()">
      <div class="grid grid-2" style="gap:12px">
        <mk-tamil-field label="Taken by" [required]="true" placeholder="Saravanan" [(en)]="f.takenByName" [(ta)]="f.takenByNameTa" [invalid]="!!err('takenByName')" [errorText]="err('takenByName')" />
        <div class="stack-sm">
          <div class="field"><label class="label">Relation to host</label><input class="input" name="rel" [(ngModel)]="f.relation" placeholder="Host's brother" /></div>
          <div class="field"><label class="label">Mobile</label><input class="input" name="mob" type="tel" inputmode="numeric" maxlength="10" [(ngModel)]="f.takenByMobile" /></div>
        </div>
      </div>
      <div class="grid grid-3" style="gap:12px">
        <div class="field"><label class="label">Category</label><select class="select" name="cat" [(ngModel)]="f.expenseCategoryId">
          <option [ngValue]="null">—</option>@for (c of cats(); track c.id) {<option [ngValue]="c.id">{{ c.name | pick: c.nameTa }}</option>}</select></div>
        <div class="field"><label class="label">Purpose <span class="req">*</span></label><input class="input" name="pur" [(ngModel)]="f.purpose" placeholder="Extra food" />
          @if (err('purpose')) {<span class="field-error">{{ err('purpose') }}</span>}</div>
        <div class="field"><label class="label">Amount <span class="req">*</span></label><input class="input num" name="amt" type="number" inputmode="numeric" [(ngModel)]="f.amount" />
          @if (err('amount')) {<span class="field-error">{{ err('amount') }}</span>}</div>
      </div>
      <div class="row end"><button class="btn btn-danger" [disabled]="busy()"><mk-icon name="plus" [size]="18" />Record expense</button></div>
    </form>
    }
    <div class="list" style="border-top:1px solid var(--border)">
      @for (x of rows(); track x.id) {
        <div class="list-item">
          <div class="grow"><div class="small" style="font-weight:700">{{ x.takenByName | pick: x.takenByNameTa }} <span class="muted xs">{{ x.relation }}</span></div>
            <div class="xs muted">{{ x.purpose }} · {{ x.categoryName | pick: x.categoryNameTa }} · {{ x.entryAt | date: 'd MMM, h:mm a' }} · by {{ x.recordedBy }}</div></div>
          <b class="num" style="color:var(--red)">− {{ x.amount | inr }}</b>
          @if (canDelete()) { <button class="btn btn-ghost btn-icon btn-sm" title="Remove" (click)="remove(x)"><mk-icon name="trash" [size]="16" /></button> }
        </div>
      } @empty { <div class="list-item small muted">No money given out yet.</div> }
    </div>
  </div>`
})
export class ExpensePanelComponent implements OnInit {
  functionId = input.required<number>();
  canDelete = input(false);
  canCreate = input(true);
  changed = output<void>();
  private api = inject(ApiService);
  private toast = inject(ToastService);
  protected cats = signal<MasterItem[]>([]);
  protected rows = signal<Expense[]>([]);
  protected total = signal(0);
  protected busy = signal(false);
  protected errors = signal<Record<string, string[]>>({});
  f: any = this.blank();

  async ngOnInit() {
    this.cats.set(await this.api.get<MasterItem[]>('/api/masters/expense-categories', { activeOnly: true }));
    await this.load();
  }
  err(k: string) { return this.errors()[k]?.[0] ?? ''; }
  async load() {
    const r = await this.api.get<Expense[]>(`/api/moi/functions/${this.functionId()}/expenses`);
    this.rows.set(r);
    this.total.set(r.reduce((s, x) => s + x.amount, 0));
  }
  async add() {
    this.busy.set(true); this.errors.set({});
    try {
      await this.api.post('/api/moi/expenses', { ...this.f, functionId: this.functionId(), paymentMode: 'Cash' });
      this.toast.ok('Expense recorded');
      this.f = this.blank();
      await this.load();
      this.changed.emit();
    } catch (e) { const a = apiError(e); this.errors.set(a.errors ?? {}); if (!a.errors) this.toast.err(a.message); }
    finally { this.busy.set(false); }
  }
  async remove(x: Expense) {
    if (!confirm(`Remove expense ₹${x.amount} (${x.purpose})?`)) return;
    try { await this.api.delete(`/api/moi/expenses/${x.id}`); await this.load(); this.changed.emit(); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
  private blank() { return { takenByName: '', takenByNameTa: '', relation: '', takenByMobile: '', expenseCategoryId: null, purpose: '', amount: null }; }
}
