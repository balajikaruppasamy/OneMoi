import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MyProfile } from '../../core/models';
import { ToastService } from '../../core/ui';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

@Component({
  selector: 'mk-profile',
  imports: [FormsModule, TamilFieldComponent],
  template: `
  <div class="page-head"><div><h1>My profile</h1><p class="muted">Mobile {{ f.mobile }} is your permanent Moi identity</p></div></div>
  <div class="card card-pad stack" style="max-width:720px">
    <div class="row" style="align-items:flex-start;gap:10px">
      <div class="field" style="width:84px"><label class="label">Initial</label><input class="input" [(ngModel)]="f.initial" maxlength="8" /></div>
      <div class="grow"><mk-tamil-field label="Name" [required]="true" [(en)]="f.name" [(ta)]="f.nameTa" [invalid]="!!err('name')" [errorText]="err('name')" /></div>
    </div>
    <div class="row" style="align-items:flex-start;gap:10px">
      <div class="field" style="width:84px"><label class="label">Initial</label><input class="input" [(ngModel)]="f.spouseInitial" maxlength="8" /></div>
      <div class="grow"><mk-tamil-field label="Spouse name" [(en)]="f.spouseName" [(ta)]="f.spouseNameTa" /></div>
    </div>
    <div class="grid grid-2" style="gap:12px;align-items:start">
      <div class="field"><label class="label">Work</label><input class="input" [(ngModel)]="f.work" /></div>
      <mk-tamil-field label="City / Ooru" [(en)]="f.city" [(ta)]="f.cityTa" />
    </div>
    <div class="field"><label class="label">E-mail</label><input class="input" type="email" [(ngModel)]="f.email" />@if (err('email')) {<span class="field-error">{{ err('email') }}</span>}</div>
    <div class="row end"><button class="btn btn-primary" (click)="save()">Save</button></div>
  </div>`
})
export class ProfilePage implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  protected errors = signal<Record<string, string[]>>({});
  f: any = {};
  async ngOnInit() { this.f = { ...(await this.api.get<MyProfile>('/api/me/profile')) }; }
  err(k: string) { return this.errors()[k]?.[0] ?? ''; }
  async save() {
    this.errors.set({});
    try { this.f = { ...(await this.api.put<MyProfile>('/api/me/profile', this.f)) }; await this.auth.reloadProfile(); this.toast.ok('Profile saved'); }
    catch (e) { const a = apiError(e); this.errors.set(a.errors ?? {}); if (!a.errors) this.toast.err(a.message); }
  }
}
