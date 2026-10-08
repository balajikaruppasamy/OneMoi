import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Denomination, FunctionDetail, FunctionListItem, FunctionSummary, MasterItem, MoiEntry, MyAssignment, PersonLookup } from '../../core/models';
import { InrPipe, PickPipe, ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';
import { ExpensePanelComponent } from '../vendor/expense-panel.component';

interface GiftRow { giftItemTypeId: number | null; description: string; descriptionTa: string; quantity: number; estimatedValue: number | null; }

/**
 * Counter screen used by vendor staff (/vendor/functions/:id/entry) and operators (/operator/entry/:id).
 * Without an id it shows a function picker.
 */
@Component({
  selector: 'mk-moi-entry',
  imports: [FormsModule, RouterLink, DatePipe, InrPipe, PickPipe, IconComponent, TamilFieldComponent, ExpensePanelComponent],
  template: `
  @if (!id()) {
    <!-- ───── pick a function ───── -->
    <div class="page-head"><div><h1>Moi entry</h1><p class="muted">Choose the function you are working at</p></div></div>
    <div class="grid grid-3">
      @for (c of choices(); track c.id) {
        <button class="card card-pad stack-sm" style="text-align:left;font:inherit;color:inherit;cursor:pointer" [disabled]="c.disabled" (click)="open(c.id)">
          <div class="row between"><span class="badge" [class.green]="c.open">{{ c.badge }}</span><span class="xs muted">{{ c.sub2 }}</span></div>
          <h3>{{ c.name | pick: c.nameTa }}</h3><div class="small muted">{{ c.sub }}</div>
        </button>
      } @empty {
        <div class="card empty"><div class="empty-art"><mk-icon name="calendar" [size]="40" /></div><h3>No functions available</h3>
          <p>{{ isOperator ? 'You have no counter assignment right now. Ask your vendor.' : 'Create a function first.' }}</p></div>
      }
    </div>
  } @else if (fn(); as fn) {
    <!-- ───── entry screen ───── -->
    <div class="counter-bar card card-pad row wrap" style="padding:12px 16px">
      @if (logo()) { <img class="tenant-logo" [src]="logo()" alt="" /> }
      <div class="grow"><div style="font-weight:800">{{ fn.name | pick: fn.nameTa }}</div>
        <div class="xs muted">{{ fn.location }} · Code {{ fn.code }} · {{ counterLabel() }}</div></div>
      @if (!isOperator) { <a class="btn btn-ghost btn-sm" [routerLink]="['/vendor/functions', fn.id]">Function details →</a> }
    </div>

    <div class="grid grid-main mt-4">
      <form class="card card-pad stack" (ngSubmit)="save()" (keydown.enter)="onEnter($event)" autocomplete="off">
        <div class="row between"><h2>New Moi entry</h2>
          <button type="button" class="btn btn-ghost btn-sm" (click)="reset()"><mk-icon name="refresh" [size]="16" />Clear</button></div>
        @if (error()) { <div class="alert err"><mk-icon name="alert" /><div>{{ error() }}</div></div> }

        <!-- Mobile + lookup -->
        <div class="field">
          <label class="label">Guest mobile</label>
          <div class="input-group"><span class="addon">+91</span>
            <input class="input input-xl" name="mobile" type="tel" inputmode="numeric" maxlength="10" [(ngModel)]="f.mobile" (ngModelChange)="onMobile($event)" placeholder="Mobile no." /></div>
          @if (err('mobile')) {<span class="field-error">{{ err('mobile') }}</span>}
        </div>
        @if (lookup(); as l) {
          @if (l.source !== 'none') {
            <div class="alert ok"><mk-icon name="check" /><div class="small"><b>{{ l.source === 'tenant' ? 'Returning guest' : 'OneMoi member' }}: {{ l.initial }} {{ l.name }}</b>&nbsp;<span class="ta">{{ l.nameTa }}</span>&nbsp;
              · {{ l.city }} @if (l.previousEntriesAtThisVendor) { · {{ l.previousEntriesAtThisVendor }} earlier Moi at your functions } @if (l.isVerified) { · ✔ verified }</div></div>
          } @else if ((f.mobile ?? '').length === 10) {
            <div class="alert warn"><mk-icon name="plus" /><div class="small"><b>New guest.</b> A OneMoi identity will be created for this mobile number.</div></div>
          }
        }

        <!-- Name with initial -->
        <div class="row" style="align-items:flex-start;gap:10px">
          <div class="field" style="width:84px"><label class="label">Initial</label><input class="input" name="ini" [(ngModel)]="f.initial" maxlength="8" placeholder="N." style="text-transform:uppercase" (blur)="checkSameName()" /></div>
          <div class="grow"><mk-tamil-field label="Name" [required]="true" placeholder="Ram" [(en)]="f.name" [(ta)]="f.nameTa" [invalid]="!!err('name')" [errorText]="err('name')" /></div>
        </div>
        <div class="row" style="align-items:flex-start;gap:10px">
          <div class="field" style="width:84px"><label class="label">Initial</label><input class="input" name="sini" [(ngModel)]="f.spouseInitial" maxlength="8" placeholder="R." style="text-transform:uppercase" /></div>
          <div class="grow"><mk-tamil-field label="Spouse name" placeholder="Geetha" [(en)]="f.spouseName" [(ta)]="f.spouseNameTa" /></div>
        </div>
        <div class="grid grid-2" style="gap:12px;align-items:start">
          <div class="field"><label class="label">Work</label><input class="input" name="work" [(ngModel)]="f.work" placeholder="Teacher" /></div>
          <mk-tamil-field label="City / Ooru" placeholder="Pollachi" [(en)]="f.city" [(ta)]="f.cityTa" />
        </div>
        @if (sameName().length) {
          <div class="alert warn"><mk-icon name="alert" /><div class="small"><b>Check the initial!</b> Other "{{ f.name }}" in {{ f.city }}:
            @for (s of sameName(); track $index) { <b>{{ s.initial }} {{ s.name }}</b>@if (s.spouseName) { (spouse {{ s.spouseName }}) } {{ s.mobileMasked }}@if (!$last) {,} }</div></div>
        }

        <!-- Category (Thaimaman etc.) -->
        <div class="field"><span class="label">Moi type</span>
          <div class="chips">
            @for (c of cats(); track c.id) {
              <button type="button" class="chip" [class.active]="f.moiCategoryId === c.id" (click)="f.moiCategoryId = c.id"
                [style.border-color]="c.isHighlighted ? c.color : null" [style.background]="f.moiCategoryId === c.id && c.color ? c.color : null">
                {{ c.isHighlighted ? '★ ' : '' }}{{ c.name | pick: c.nameTa }}</button>
            }
          </div>
          @if (selectedCat()?.isHighlighted) { <span class="hint" style="color:var(--gold-strong);font-weight:700">★ This Moi will be highlighted in the function report.</span> }
        </div>

        <!-- Payment -->
        <div class="segmented">
          @for (m of modes; track m.v) { <button type="button" [class.active]="f.paymentMode === m.v" (click)="setMode(m.v)">{{ m.l }}</button> }
        </div>

        @if (f.paymentMode !== 'GiftOnly') {
          <div class="field">
            <div class="row between"><label class="label">Moi amount <span class="req">*</span></label>
              @if (f.paymentMode === 'Cash') { <label class="check small"><input type="checkbox" name="useDen" [(ngModel)]="useDenoms" (ngModelChange)="syncDenoms()" />Count by notes (500 × 50…)</label> }</div>
            <div class="input-group"><span class="addon" style="font-size:1.4rem">₹</span>
              <input class="input input-xl num" name="amount" inputmode="numeric" [(ngModel)]="f.amount" [readonly]="useDenoms" style="text-align:right" placeholder="0" id="amountInput" /></div>
            @if (err('amount')) {<span class="field-error">{{ err('amount') }}</span>}
            @if (!useDenoms) {
              <div class="chips mt-1">@for (q of quick; track q) {<button type="button" class="chip" (click)="f.amount = q">{{ q | inr }}</button>}</div>
            }
          </div>
          @if (useDenoms && f.paymentMode === 'Cash') {
            <div class="denom-grid">
              @for (d of denoms(); track d.value) {
                <div class="denom"><b>₹{{ d.value }}</b><span class="muted">×</span>
                  <input class="input num" type="text" inputmode="numeric" maxlength="5" [name]="'den' + d.value" [(ngModel)]="counts[d.value]" (ngModelChange)="syncDenoms()" />
                  <small>{{ (counts[d.value] || 0) * d.value | inr }}</small></div>
              }
            </div>
            @if (err('denominations')) {<span class="field-error">{{ err('denominations') }}</span>}
          }
          @if (f.paymentMode !== 'Cash') {
            <div class="field"><label class="label">{{ f.paymentMode === 'Upi' ? 'UPI reference' : 'Reference no.' }}</label><input class="input" name="ref" [(ngModel)]="f.paymentRef" /></div>
          }
        }

        <!-- Gifts -->
        <div class="field">
          <div class="row between"><span class="label">Gifts {{ f.paymentMode === 'GiftOnly' ? '*' : '(optional)' }}</span>
            <button type="button" class="btn btn-ghost btn-sm" (click)="addGift()"><mk-icon name="gift" [size]="16" />Add gift</button></div>
          @for (g of gifts; track $index; let i = $index) {
            <div class="row wrap" style="gap:8px;align-items:flex-end">
              <select class="select" style="width:140px" [name]="'gt' + i" [(ngModel)]="g.giftItemTypeId">
                <option [ngValue]="null">Type</option>@for (t of giftTypes(); track t.id) {<option [ngValue]="t.id">{{ t.name | pick: t.nameTa }}</option>}</select>
              <input class="input grow" style="min-width:160px" [name]="'gd' + i" [(ngModel)]="g.description" placeholder="Gold ring 1 sovereign" />
              <input class="input num" style="width:80px" type="number" [name]="'gq' + i" [(ngModel)]="g.quantity" title="Quantity" />
              <input class="input num" style="width:120px" type="number" [name]="'gv' + i" [(ngModel)]="g.estimatedValue" placeholder="Value ₹" />
              <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="gifts.splice(i, 1)"><mk-icon name="x" [size]="16" /></button>
            </div>
          }
        </div>

        <div class="field"><label class="label">Notes</label><input class="input" name="notes" [(ngModel)]="f.notes" placeholder="Any remark" /></div>

        <div class="sticky-actions row">
          <div class="grow"><div class="xs muted">TOTAL</div><div class="money" style="font-size:1.6rem">{{ (+f.amount || 0) | inr }}</div></div>
          <button class="btn btn-primary btn-lg" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} <mk-icon name="printer" />Save Moi</button>
        </div>
      </form>

      <!-- side -->
      <div class="stack">
        @if (summary(); as s) {
          <div class="hero-card">
            <div class="small muted">{{ isOperator ? 'Function total (all counters)' : 'Function total' }}</div>
            <div class="money" style="font-size:2rem">{{ s.totalCollected | inr }}</div>
            <div class="row mt-2" style="gap:18px"><div><div class="xs muted">Entries</div><b>{{ s.entries }}</b></div>
              <div><div class="xs muted">Cash in hand</div><b>{{ s.cashInHand | inr }}</b></div><div><div class="xs muted">UPI</div><b>{{ s.upi | inr }}</b></div></div>
          </div>
        }
        @if (last(); as l) {
          <div class="card card-pad stack-sm" style="border-color:var(--green)">
            <div class="row between"><span class="badge green">Saved</span><span class="xs muted num">{{ l.receiptNo }}</span></div>
            <div><b>{{ l.initial }} {{ l.name }}</b>&nbsp;<span class="ta">{{ l.nameTa }}</span>&nbsp;</div>
            <div class="money" style="font-size:1.5rem">{{ l.amount | inr }}</div>
            <button class="btn btn-outline btn-sm" (click)="printReceipt(l)"><mk-icon name="printer" [size]="16" />Print receipt</button>
          </div>
        }
        <div class="card">
          <div class="card-head"><h3>{{ isOperator ? 'My recent entries' : 'Recent entries' }}</h3></div>
          <div class="list">
            @for (e of recent(); track e.id) {
              <div class="list-item" [style.opacity]="e.status === 'Reversed' ? .5 : 1">
                <div class="grow"><div class="small" style="font-weight:700">{{ e.initial }} {{ e.name | pick: e.nameTa }} @if (e.isHighlighted) {<span class="badge gold">★</span>}</div>
                  <div class="xs muted">#{{ e.serialNo }} · {{ e.city }} · {{ e.paymentMode }} · {{ e.entryAt | date: 'h:mm a' }}</div></div>
                <b class="num">{{ e.amount | inr }}</b>
                @if (e.status === 'Active') { <button class="btn btn-ghost btn-sm" title="Correct (reverse)" (click)="reverse(e)"><mk-icon name="x" [size]="14" /></button> }
              </div>
            } @empty { <div class="list-item small muted">No entries yet.</div> }
          </div>
        </div>
        @if (auth.can('expenses.create')) {
          <button class="btn btn-outline btn-block" (click)="showExpense.set(!showExpense())"><mk-icon name="wallet" />{{ showExpense() ? 'Hide' : 'Record money given out (expense)' }}</button>
          @if (showExpense()) { <mk-expense-panel [functionId]="fn.id" (changed)="loadSide()" /> }
        }
      </div>
    </div>
  }`
})
export class MoiEntryPage implements OnInit {
  id = input<string>();
  private api = inject(ApiService);
  protected auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  protected isOperator = this.auth.area() === 'operator';
  protected fn = signal<FunctionDetail | null>(null);
  protected cats = signal<MasterItem[]>([]);
  protected giftTypes = signal<MasterItem[]>([]);
  protected denoms = signal<Denomination[]>([]);
  protected summary = signal<FunctionSummary | null>(null);
  protected recent = signal<MoiEntry[]>([]);
  protected last = signal<MoiEntry | null>(null);
  protected lookup = signal<PersonLookup | null>(null);
  protected sameName = signal<PersonLookup['sameNameInCity']>([]);
  protected choices = signal<{ id: number; name: string; nameTa?: string; sub: string; sub2: string; badge: string; open: boolean; disabled: boolean }[]>([]);
  protected busy = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  protected showExpense = signal(false);
  protected counterId = signal<number | null>(null);
  protected logo = computed(() => this.api.file(this.fn()?.tenantLogo ?? this.auth.profile()?.tenantLogo));
  protected selectedCat() { return this.cats().find(c => c.id === this.f?.moiCategoryId); }
  protected modes = [{ v: 'Cash', l: 'Cash' }, { v: 'Upi', l: 'UPI' }, { v: 'Card', l: 'Card' }, { v: 'Cheque', l: 'Cheque' }, { v: 'GiftOnly', l: 'Gift only' }];
  protected quick = [101, 201, 501, 1001, 2001, 5001, 10001];

  f: any = {};
  gifts: GiftRow[] = [];
  counts: Record<number, number | null> = {};
  useDenoms = false;
  private clientRef = '';

  async ngOnInit() {
    if (!this.id()) return this.loadChoices();
    try {
      const [fn, cats, gt, dn] = await Promise.all([
        this.api.get<FunctionDetail>(`/api/functions/${this.id()}`),
        this.api.get<MasterItem[]>('/api/masters/moi-categories', { activeOnly: true }),
        this.api.get<MasterItem[]>('/api/masters/gift-item-types', { activeOnly: true }),
        this.api.get<Denomination[]>('/api/masters/denominations')
      ]);
      this.fn.set(fn); this.cats.set(cats); this.giftTypes.set(gt); this.denoms.set(dn);
      if (this.isOperator) {
        const mine = await this.api.get<MyAssignment[]>('/api/operator/assignments');
        this.counterId.set(mine.find(a => a.functionId === fn.id)?.counterId ?? null);
      }
      this.reset();
      await this.loadSide();
    } catch (e) { this.toast.err(apiError(e).message); }
  }

  counterLabel() {
    const c = this.fn()?.counters.find(x => x.id === this.counterId());
    return this.isOperator ? `${c?.name ?? 'Counter'} · ${this.auth.profile()?.name}` : 'Vendor desk';
  }

  async loadChoices() {
    if (this.isOperator) {
      const mine = await this.api.get<MyAssignment[]>('/api/operator/assignments');
      this.choices.set(mine.map(a => ({ id: a.functionId, name: a.functionName, nameTa: a.functionNameTa, sub: `${a.location} · ${a.counterName}`,
        sub2: new Date(a.functionDate.slice(0, 10) + 'T00:00').toDateString(), badge: a.isOpenNow ? 'Open now' : 'Not open', open: a.isOpenNow, disabled: !a.isOpenNow })));
      if (mine.length === 1 && mine[0].isOpenNow) this.open(mine[0].functionId);
    } else {
      const list = await this.api.get<FunctionListItem[]>('/api/functions', {});
      this.choices.set(list.filter(f => f.status === 'Live' || f.status === 'Scheduled').map(f => ({ id: f.id, name: f.name, nameTa: f.nameTa,
        sub: `${f.location} · ${f.entryCount} entries`, sub2: f.functionDate.slice(0, 10), badge: f.status, open: f.status === 'Live', disabled: false })));
    }
  }
  open(id: number) { this.router.navigateByUrl(this.isOperator ? `/operator/entry/${id}` : `/vendor/functions/${id}/entry`); }

  async loadSide() {
    const fid = this.fn()!.id;
    this.summary.set(await this.api.get<FunctionSummary>(`/api/moi/functions/${fid}/summary`));
    this.recent.set(await this.api.get<MoiEntry[]>('/api/moi/entries', { functionId: fid, take: 12 }));
  }

  reset() {
    this.f = { mobile: '', initial: '', name: '', nameTa: '', spouseInitial: '', spouseName: '', spouseNameTa: '', work: '', city: '', cityTa: '',
      moiCategoryId: this.cats()[0]?.id ?? null, amount: null, paymentMode: 'Cash', paymentRef: '', notes: '' };
    this.gifts = []; this.counts = {}; this.useDenoms = false;
    this.lookup.set(null); this.sameName.set([]); this.error.set(''); this.errors.set({});
    this.clientRef = crypto.randomUUID();
  }

  err(k: string) { return this.errors()[k]?.[0] ?? ''; }

  async onMobile(v: string) {
    const m = (v ?? '').replace(/\D/g, '');
    if (m.length !== 10) { this.lookup.set(null); return; }
    const l = await this.api.get<PersonLookup>('/api/moi/lookup', { mobile: m });
    this.lookup.set(l);
    if (l.source !== 'none') {
      Object.assign(this.f, { initial: l.initial ?? '', name: l.name ?? '', nameTa: l.nameTa ?? '', spouseInitial: l.spouseInitial ?? '', spouseName: l.spouseName ?? '',
        spouseNameTa: l.spouseNameTa ?? '', work: l.work ?? '', city: l.city ?? '', cityTa: l.cityTa ?? '' });
      setTimeout(() => document.getElementById('amountInput')?.focus(), 50);
    }
  }

  /** Same name in the same city → remind operator to confirm the initial. */
  async checkSameName() {
    if (!this.f.name || !this.f.city) return;
    const l = await this.api.get<PersonLookup>('/api/moi/lookup', { mobile: this.f.mobile, name: this.f.name, city: this.f.city });
    this.sameName.set(l.sameNameInCity.filter(s => (s.initial ?? '') !== normInitial(this.f.initial)));
  }

  setMode(m: string) { this.f.paymentMode = m; if (m !== 'Cash') this.useDenoms = false; if (m === 'GiftOnly') { this.f.amount = 0; if (!this.gifts.length) this.addGift(); } }

  syncDenoms() {
    if (!this.useDenoms) return;
    this.f.amount = this.denoms().reduce((s, d) => s + d.value * (Number(this.counts[d.value]) || 0), 0) || null;
  }

  addGift() { this.gifts.push({ giftItemTypeId: null, description: '', descriptionTa: '', quantity: 1, estimatedValue: null }); }

  onEnter(e: Event) {
    // Enter saves only from the amount box; elsewhere it just moves on (avoids accidental saves)
    const t = e.target as HTMLElement;
    if (t.id !== 'amountInput') e.preventDefault();
  }

  async save() {
    if (this.busy()) return;
    this.checkSameName();
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try {
      const body = {
        ...this.f, functionId: this.fn()!.id, counterId: this.counterId(), amount: Number(this.f.amount) || 0, clientRef: this.clientRef,
        denominations: this.useDenoms ? this.denoms().filter(d => Number(this.counts[d.value]) > 0).map(d => ({ noteValue: d.value, count: Number(this.counts[d.value]) })) : [],
        gifts: this.gifts.filter(g => g.description.trim())
      };
      const saved = await this.api.post<MoiEntry>('/api/moi/entries', body);
      this.last.set(saved);
      this.toast.ok(`${saved.receiptNo} · ₹${saved.amount.toLocaleString('en-IN')} saved`);
      this.reset();
      await this.loadSide();
      setTimeout(() => (document.querySelector('input[name=mobile]') as HTMLInputElement)?.focus(), 50);
    } catch (e) {
      const a = apiError(e); this.error.set(a.message); this.errors.set(a.errors ?? {});
    } finally { this.busy.set(false); }
  }

  async reverse(e: MoiEntry) {
    const reason = prompt(`Correct ${e.receiptNo} (₹${e.amount})? It will be reversed, not deleted.\nReason:`);
    if (!reason) return;
    try { await this.api.post(`/api/moi/entries/${e.id}/reverse`, { reason }); this.toast.ok('Entry reversed'); await this.loadSide(); }
    catch (err) { this.toast.err(apiError(err).message); }
  }

  printReceipt(e: MoiEntry) {
    const fn = this.fn()!;
    const logo = this.logo();
    const w = window.open('', '_blank', 'width=380,height=600');
    if (!w) return;
    w.document.write(`<html><head><title>${e.receiptNo}</title><style>body{font-family:'Noto Sans Tamil',sans-serif;padding:16px;width:300px}
      h2,h3,p{margin:4px 0;text-align:center}.r{display:flex;justify-content:space-between;font-size:14px;margin:3px 0}.amt{font-size:26px;font-weight:800;text-align:center;margin:10px 0}
      hr{border:0;border-top:1px dashed #999}</style></head><body>
      ${logo ? `<p><img src="${logo}" style="height:48px"></p>` : ''}<h3>${fn.tenantName}</h3><p>${fn.tenantNameTa ?? ''}</p><hr>
      <h2>${fn.nameTa ?? fn.name}</h2><p>${fn.name}</p><hr>
      <div class="r"><span>ரசீது / Receipt</span><b>${e.receiptNo}</b></div>
      <div class="r"><span>பெயர் / Name</span><b>${e.initial ?? ''} ${e.nameTa ?? e.name}</b></div>
      ${e.spouseName ? `<div class="r"><span>துணைவர்</span><b>${e.spouseInitial ?? ''} ${e.spouseNameTa ?? e.spouseName}</b></div>` : ''}
      <div class="r"><span>ஊர் / City</span><b>${e.cityTa ?? e.city ?? ''}</b></div>
      ${e.categoryName ? `<div class="r"><span>வகை</span><b>${e.categoryNameTa ?? e.categoryName}</b></div>` : ''}
      <div class="amt">₹${e.amount.toLocaleString('en-IN')}</div>
      ${e.gifts.map(g => `<div class="r"><span>🎁</span><b>${g.description}</b></div>`).join('')}
      <hr><p style="font-size:12px">${new Date(e.entryAt).toLocaleString('en-IN')}</p><p>நன்றி! Thank you</p>
      <script>setTimeout(()=>print(),300)<\/script></body></html>`);
    w.document.close();
  }
}

function normInitial(i?: string) {
  const l = (i ?? '').replace(/[^A-Za-z]/g, '').toUpperCase();
  return l ? l.split('').map(c => c + '.').join('') : '';
}
