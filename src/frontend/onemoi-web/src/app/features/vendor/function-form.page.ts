import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, apiError } from '../../core/api.service';
import { FunctionDetail, MasterItem } from '../../core/models';
import { PickPipe, ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

/** Create / edit a function. Names are saved in English and Tamil. */
@Component({
  selector: 'mk-function-form',
  imports: [FormsModule, RouterLink, PickPipe, IconComponent, TamilFieldComponent],
  template: `
  <div class="page-head"><div><a class="small" [routerLink]="id() ? ['/vendor/functions', id()] : '/vendor/functions'">← Back</a>
    <h1 class="mt-1">{{ id() ? 'Edit function' : 'Create function' }}</h1>
    <p class="muted">Type in English — the Tamil name fills automatically. Pick a different spelling or edit it if needed.</p></div></div>

  @if (error()) { <div class="alert err mb-4"><mk-icon name="alert" /><div>{{ error() }}</div></div> }

  <form class="grid grid-main" (ngSubmit)="save()">
    <div class="stack">
      <div class="card card-pad stack">
        <div class="section-title">Function details</div>
        <div class="field"><span class="label">Function type <span class="req">*</span></span>
          <div class="chips">
            @for (t of types(); track t.id) {
              <button type="button" class="chip" [class.active]="f.functionTypeId === t.id" (click)="f.functionTypeId = t.id">{{ t.name | pick: t.nameTa }}</button>
            }
          </div>
          @if (err('functionTypeId')) {<span class="field-error">{{ err('functionTypeId') }}</span>}
        </div>
        <mk-tamil-field label="Function name" [required]="true" placeholder="Ram illa villa" [(en)]="f.name" [(ta)]="f.nameTa" [invalid]="!!err('name')" [errorText]="err('name')" />
        <div class="grid grid-3" style="gap:12px">
          <div class="field"><label class="label">Function date <span class="req">*</span></label><input class="input" type="date" name="date" [(ngModel)]="f.functionDate" required /></div>
          <div class="field"><label class="label">Start time</label><input class="input" type="time" name="st" [(ngModel)]="f.startTime" /></div>
          <div class="field"><label class="label">End time</label><input class="input" type="time" name="et" [(ngModel)]="f.endTime" /></div>
        </div>
      </div>

      <div class="card card-pad stack">
        <div class="section-title">Function owner (host)</div>
        <mk-tamil-field label="Owner name" [required]="true" placeholder="N. Ram" [(en)]="f.ownerName" [(ta)]="f.ownerNameTa" [invalid]="!!err('ownerName')" [errorText]="err('ownerName')" />
        <div class="grid grid-2" style="gap:12px">
          <div class="field"><label class="label">Owner phone number <span class="req">*</span></label>
            <div class="input-group"><span class="addon">+91</span><input class="input" name="om" type="tel" inputmode="numeric" maxlength="10" [(ngModel)]="f.ownerMobile" /></div>
            @if (err('ownerMobile')) {<span class="field-error">{{ err('ownerMobile') }}</span>} @else {<span class="hint">The owner logs in with this number to see the live Moi report.</span>}</div>
          <div class="field"><label class="label">Owner e-mail</label><input class="input" name="oe" type="email" [(ngModel)]="f.ownerEmail" />
            @if (err('ownerEmail')) {<span class="field-error">{{ err('ownerEmail') }}</span>}</div>
        </div>
      </div>

      <div class="card card-pad stack">
        <div class="section-title">Location</div>
        <mk-tamil-field label="Function location / Mandapam" [required]="true" placeholder="KPR Mahal" [(en)]="f.location" [(ta)]="f.locationTa" [invalid]="!!err('location')" [errorText]="err('location')" />
        <div class="grid grid-2" style="gap:12px">
          <div class="field"><label class="label">Address</label><input class="input" name="addr" [(ngModel)]="f.address" /></div>
          <div class="field"><label class="label">City</label><input class="input" name="city" [(ngModel)]="f.city" /></div>
        </div>
        <div class="field"><label class="label">Other function details</label><textarea class="textarea" name="other" [(ngModel)]="f.otherDetails" placeholder="Lunch timing, return gifts, special instructions for operators…"></textarea></div>
      </div>
    </div>

    <div class="stack">
      <div class="card card-pad stack">
        <div class="section-title">Counters &amp; payments</div>
        <div class="field"><label class="label">Moi counters</label>
          <div class="row"><button type="button" class="btn btn-outline btn-icon" (click)="f.counterCount = max(1, f.counterCount - 1)">−</button>
            <b style="font-size:1.4rem;min-width:40px;text-align:center">{{ f.counterCount }}</b>
            <button type="button" class="btn btn-outline btn-icon" (click)="f.counterCount = min(20, f.counterCount + 1)">+</button></div>
          <span class="hint">@if (f.expectedGuests) { ≈ {{ round(f.expectedGuests / f.counterCount) }} guests per counter } @else { One operator per counter }</span>
          @if (err('counterCount')) {<span class="field-error">{{ err('counterCount') }}</span>}</div>
        <div class="field"><label class="label">Expected guests</label><input class="input" type="number" name="eg" [(ngModel)]="f.expectedGuests" /></div>
        <label class="check"><input type="checkbox" name="cash" [(ngModel)]="f.allowCash" /> Accept cash at counter</label>
        <label class="check"><input type="checkbox" name="upi" [(ngModel)]="f.allowUpi" /> Accept UPI</label>
      </div>
      <button class="btn btn-primary btn-lg btn-block" [disabled]="busy()">@if (busy()) {<span class="spinner"></span>} {{ id() ? 'Save changes' : 'Create function' }}</button>
      @if (!id()) { <p class="xs muted center">A 6-character function code is generated automatically.</p> }
    </div>
  </form>`
})
export class FunctionFormPage implements OnInit {
  id = input<string>();          // from route param :id (withComponentInputBinding)
  private api = inject(ApiService);
  private router = inject(Router);
  private toast = inject(ToastService);
  protected types = signal<MasterItem[]>([]);
  protected busy = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  protected max = Math.max; protected min = Math.min; protected round = Math.round;
  f: any = {
    functionTypeId: 0, name: '', nameTa: '', ownerName: '', ownerNameTa: '', ownerMobile: '', ownerEmail: '', location: '', locationTa: '',
    address: '', city: '', functionDate: new Date().toISOString().slice(0, 10), startTime: '06:00', endTime: '14:00', expectedGuests: null,
    otherDetails: '', allowCash: true, allowUpi: true, counterCount: 3
  };

  async ngOnInit() {
    this.types.set(await this.api.get<MasterItem[]>('/api/masters/function-types', { activeOnly: true }));
    if (this.id()) {
      const d = await this.api.get<FunctionDetail>(`/api/functions/${this.id()}`);
      this.f = { ...d, functionDate: d.functionDate.slice(0, 10), counterCount: d.counters.length };
    } else if (this.types().length) this.f.functionTypeId = this.types()[0].id;
  }

  err(k: string) { return this.errors()[k]?.[0] ?? ''; }

  async save() {
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try {
      const saved = this.id()
        ? await this.api.put<FunctionDetail>(`/api/functions/${this.id()}`, this.f)
        : await this.api.post<FunctionDetail>('/api/functions', this.f);
      this.toast.ok(this.id() ? 'Function updated' : `Function created · code ${saved.code}`);
      await this.router.navigate(['/vendor/functions', saved.id]);
    } catch (e) {
      const a = apiError(e); this.error.set(a.message); this.errors.set(a.errors ?? {});
    } finally { this.busy.set(false); }
  }
}
