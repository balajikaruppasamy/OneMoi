import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { P } from '../../core/permissions';
import { Denomination, MasterItem } from '../../core/models';
import { ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

const KINDS = [
  { key: 'function-types', label: 'Function types', hint: 'Marriage, Keda Vettu, Housewarming…' },
  { key: 'moi-categories', label: 'Moi types', hint: 'Thaimaman Moi, Seer Varisai… highlighted types are coloured in entry and reports' },
  { key: 'gift-item-types', label: 'Gift types', hint: 'Gold, silver, vessels…' },
  { key: 'expense-categories', label: 'Expense types', hint: 'Food, transport… money given out during the function' }
];

/**
 * Master data. System rows (🔒) come with OneMoi and are managed by the super admin;
 * vendors can add their own rows on top.
 */
@Component({
  selector: 'mk-masters',
  imports: [FormsModule, IconComponent, TamilFieldComponent],
  template: `
  <div class="page-head"><div><h1>Masters</h1><p class="muted">{{ admin ? 'System defaults shared by every vendor' : 'Lookup lists used across your functions' }}</p></div>
    @if (kind() !== 'denominations' && canEdit) { <button class="btn btn-primary" (click)="open()"><mk-icon name="plus" />Add</button> }</div>

  <div class="tabs">
    @for (k of kinds; track k.key) { <button [class.active]="kind() === k.key" (click)="select(k.key)">{{ k.label }}</button> }
    <button [class.active]="kind() === 'denominations'" (click)="select('denominations')">Denominations</button>
  </div>
  <p class="small muted mt-2">{{ hint() }}</p>

  @if (kind() === 'denominations') {
    <div class="card mt-4"><div class="list">
      @for (d of denoms(); track d.id) { <div class="list-item"><div class="grow"><b>₹{{ d.value }}</b></div><span class="badge">{{ d.isCoin ? 'Coin' : 'Note' }}</span></div> }
    </div></div>
  } @else {
    <div class="card mt-4"><div class="table-wrap"><table class="table table-stack">
      <thead><tr><th>#</th><th>Name</th><th>Tamil</th>@if (kind() === 'moi-categories') {<th>Highlight</th>}@if (kind() === 'gift-item-types') {<th>Unit</th>}<th>Status</th><th></th></tr></thead>
      <tbody>
        @for (m of rows(); track m.id) {
          <tr>
            <td data-label="#" class="num muted">{{ m.sortOrder }}</td>
            <td data-label="Name"><b>{{ m.name }}</b> @if (m.isSystem) {<span class="xs muted" title="System default">🔒</span>}</td>
            <td data-label="Tamil" class="ta">{{ m.nameTa }}</td>
            @if (kind() === 'moi-categories') { <td data-label="Highlight">@if (m.isHighlighted) {<span class="badge" [style.background]="m.color + '22'" [style.color]="m.color">★ {{ m.color }}</span>}</td> }
            @if (kind() === 'gift-item-types') { <td data-label="Unit">{{ m.unit }}</td> }
            <td data-label="Status"><span class="badge" [class.green]="m.isActive">{{ m.isActive ? 'Active' : 'Hidden' }}</span></td>
            <td class="r" data-label="">@if (canEdit && (!m.isSystem || admin)) {<button class="btn btn-ghost btn-sm" (click)="open(m)">Edit</button><button class="btn btn-ghost btn-sm" (click)="remove(m)">Delete</button>}</td>
          </tr>
        }
      </tbody>
    </table></div></div>
  }

  @if (editing()) {
    <div class="modal-backdrop open" (click)="editing.set(false)">
      <div class="modal" style="max-width:460px" (click)="$event.stopPropagation()">
        <div class="modal-head"><h2>{{ m.id ? 'Edit' : 'Add' }}</h2><button class="btn btn-ghost btn-icon btn-sm" (click)="editing.set(false)"><mk-icon name="x" /></button></div>
        <div class="modal-body stack">
          <mk-tamil-field label="Name" [required]="true" [(en)]="m.name" [(ta)]="m.nameTa" [invalid]="!!err" [errorText]="err" />
          <div class="field"><label class="label">Sort order</label><input class="input" type="number" [(ngModel)]="m.sortOrder" /></div>
          @if (kind() === 'moi-categories') {
            <label class="check"><input type="checkbox" [(ngModel)]="m.isHighlighted" /> Highlight this Moi type (e.g. Thaimaman Moi)</label>
            @if (m.isHighlighted) { <div class="field"><label class="label">Colour</label><input class="input" type="color" [(ngModel)]="m.color" style="height:48px;padding:4px" /></div> }
          }
          @if (kind() === 'gift-item-types') { <div class="field"><label class="label">Unit</label><input class="input" [(ngModel)]="m.unit" placeholder="sovereign / grams / pieces" /></div> }
          <label class="check"><input type="checkbox" [(ngModel)]="m.isActive" /> Active</label>
        </div>
        <div class="modal-foot"><button class="btn btn-outline" (click)="editing.set(false)">Cancel</button><button class="btn btn-primary" (click)="save()">Save</button></div>
      </div>
    </div>
  }`
})
export class MastersPage implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  protected admin = this.auth.area() === 'admin';
  /** Super admin edits system rows; vendor owner / manager edit their own rows */
  protected canEdit = this.admin ? this.auth.can(P.platformMastersManage) : this.auth.can(P.mastersManage);
  protected kinds = KINDS;
  protected kind = signal('function-types');
  protected rows = signal<MasterItem[]>([]);
  protected denoms = signal<Denomination[]>([]);
  protected editing = signal(false);
  protected hint = signal(KINDS[0].hint);
  m: any = {}; err = '';

  ngOnInit() { this.load(); }
  select(k: string) { this.kind.set(k); this.hint.set(KINDS.find(x => x.key === k)?.hint ?? 'Notes and coins used for "count by notes" in Moi entry'); this.load(); }
  async load() {
    if (this.kind() === 'denominations') this.denoms.set(await this.api.get<Denomination[]>('/api/masters/denominations'));
    else this.rows.set(await this.api.get<MasterItem[]>(`/api/masters/${this.kind()}`));
  }
  open(x?: MasterItem) {
    this.err = '';
    this.m = x ? { ...x } : { name: '', nameTa: '', sortOrder: (this.rows().at(-1)?.sortOrder ?? 0) + 1, isActive: true, isHighlighted: false, color: '#F2A516', unit: '' };
    this.editing.set(true);
  }
  async save() {
    try {
      if (this.m.id) await this.api.put(`/api/masters/${this.kind()}/${this.m.id}`, this.m);
      else await this.api.post(`/api/masters/${this.kind()}`, this.m);
      this.editing.set(false); this.toast.ok('Saved'); await this.load();
    } catch (e) { const a = apiError(e); this.err = a.errors?.['name']?.[0] ?? ''; if (!this.err) this.toast.err(a.message); }
  }
  async remove(x: MasterItem) {
    if (!confirm(`Delete "${x.name}"?`)) return;
    try { await this.api.delete(`/api/masters/${this.kind()}/${x.id}`); await this.load(); } catch (e) { this.toast.err(apiError(e).message); }
  }
}
