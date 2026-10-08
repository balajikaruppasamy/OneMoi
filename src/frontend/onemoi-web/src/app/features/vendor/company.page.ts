import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, apiError } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { RolePermissions, TenantMember, TenantProfile } from '../../core/models';
import { P, PERMISSION_LABELS } from '../../core/permissions';
import { ToastService } from '../../core/ui';
import { IconComponent } from '../../shared/icon.component';
import { TamilFieldComponent } from '../../shared/tamil-field.component';

/**
 * Vendor company page:
 *  • Profile & logo (owner only — tenant.profile.manage)
 *  • Staff logins: add, change role, disable, reset password (owner only — tenant.staff.manage)
 *  • Roles & access: who can do what (read-only)
 */
@Component({
  selector: 'mk-company',
  imports: [FormsModule, DatePipe, IconComponent, TamilFieldComponent],
  template: `
  <div class="page-head"><div><h1>Company</h1><p class="muted">Profile, logo, staff logins and access</p></div></div>
  <div class="tabs">
    <button [class.active]="tab() === 'profile'" (click)="tab.set('profile')">Profile &amp; logo</button>
    @if (auth.can(P.tenantStaffView)) {
      <button [class.active]="tab() === 'staff'" (click)="tab.set('staff')">Staff logins <span class="count">{{ members().length }}</span></button>
      <button [class.active]="tab() === 'roles'" (click)="tab.set('roles')">Roles &amp; access</button>
    }
  </div>
  @if (error()) { <div class="alert err mt-4"><mk-icon name="alert" /><div>{{ error() }}</div></div> }
  @if (!canEdit && tab() === 'profile') { <div class="alert mt-4"><mk-icon name="lock" /><div class="small">Only the vendor <b>owner</b> can change company details. You can view them.</div></div> }

  @if (tab() === 'profile' && p(); as p) {
  <div class="grid grid-main mt-4">
    <fieldset class="stack" [disabled]="!canEdit" style="border:0;padding:0;margin:0;min-width:0">
      <div class="card card-pad stack">
        <div class="section-title">Company</div>
        <mk-tamil-field label="Company name" [required]="true" [(en)]="f.name" [(ta)]="f.nameTa" [invalid]="!!err('name')" [errorText]="err('name')" />
        <div class="field"><label class="label">Tagline</label><input class="input" [(ngModel)]="f.tagline" /></div>
        <div class="grid grid-2" style="gap:12px">
          <div class="field"><label class="label">Owner name *</label><input class="input" [(ngModel)]="f.ownerName" /></div>
          <div class="field"><label class="label">GSTIN</label><input class="input" [(ngModel)]="f.gstin" maxlength="15" style="text-transform:uppercase" />@if (err('gstin')) {<span class="field-error">{{ err('gstin') }}</span>}</div>
          <div class="field"><label class="label">Mobile *</label><input class="input" [(ngModel)]="f.mobile" maxlength="10" />@if (err('mobile')) {<span class="field-error">{{ err('mobile') }}</span>}</div>
          <div class="field"><label class="label">Alternate mobile</label><input class="input" [(ngModel)]="f.altMobile" maxlength="10" /></div>
          <div class="field"><label class="label">E-mail</label><input class="input" type="email" [(ngModel)]="f.email" />@if (err('email')) {<span class="field-error">{{ err('email') }}</span>}</div>
          <div class="field"><label class="label">Brand colour</label><input class="input" type="color" [(ngModel)]="f.primaryColor" style="height:48px;padding:4px" /></div>
        </div>
      </div>
      <div class="card card-pad stack">
        <div class="section-title">Address</div>
        <div class="field"><label class="label">Address</label><input class="input" [(ngModel)]="f.addressLine" /></div>
        <div class="grid grid-2" style="gap:12px">
          <div class="field"><label class="label">City</label><input class="input" [(ngModel)]="f.city" /></div>
          <div class="field"><label class="label">District</label><input class="input" [(ngModel)]="f.district" /></div>
          <div class="field"><label class="label">State</label><input class="input" [(ngModel)]="f.state" /></div>
          <div class="field"><label class="label">Pincode</label><input class="input" [(ngModel)]="f.pincode" maxlength="6" /></div>
        </div>
      </div>
      <div class="card card-pad stack">
        <div class="section-title">Receipt &amp; report</div>
        <div class="field"><label class="label">Receipt header</label><input class="input" [(ngModel)]="f.receiptHeader" /></div>
        <div class="field"><label class="label">Receipt footer</label><input class="input ta-input" [(ngModel)]="f.receiptFooter" /></div>
      </div>
      @if (canEdit) { <div class="row end"><button class="btn btn-primary btn-lg" (click)="save()" [disabled]="busy()">Save profile</button></div> }
    </fieldset>

    <div class="card card-pad center stack">
      <div class="section-title">Logo</div>
      @if (logo()) { <img class="tenant-logo lg" style="margin:0 auto;width:140px;height:140px" [src]="logo()" alt="Logo" /> }
      @else { <div style="margin:0 auto;width:140px;height:140px;border-radius:30px;display:grid;place-items:center;background:var(--brand-soft);color:var(--brand)"><mk-icon name="store" [size]="48" /></div> }
      @if (canEdit) {
        <label class="btn btn-outline"><mk-icon name="upload" />Upload logo<input type="file" accept="image/png,image/jpeg,image/webp" hidden (change)="upload($event)" /></label>
        <span class="xs muted">PNG / JPG / WEBP, up to 2 MB. Square works best.</span>
      }
      <dl class="kv" style="text-align:left"><dt>Vendor code</dt><dd>{{ p.code }}</dd><dt>Status</dt><dd>{{ p.status }}</dd><dt>Plan</dt><dd>{{ p.planName ?? '—' }}</dd></dl>
    </div>
  </div>
  }

  @if (tab() === 'staff') {
    <div class="card mt-4">
      <div class="card-head"><h3>Staff logins</h3>
        @if (canStaff) { <button class="btn btn-primary btn-sm" (click)="adding.set(!adding())"><mk-icon name="plus" [size]="16" />Add staff</button> }</div>
      @if (adding()) {
        <div class="card-body grid grid-3" style="gap:10px;border-bottom:1px solid var(--border)">
          <input class="input" placeholder="Name" [(ngModel)]="nm.name" />
          <input class="input" placeholder="Mobile" maxlength="10" [(ngModel)]="nm.mobile" />
          <input class="input" placeholder="E-mail (optional)" [(ngModel)]="nm.email" />
          <select class="select" [(ngModel)]="nm.role"><option>Manager</option><option>Accountant</option><option>Owner</option></select>
          <input class="input" type="password" placeholder="Password (8+ letters & numbers)" [(ngModel)]="nm.password" />
          <button class="btn btn-primary" (click)="addMember()">Create login</button>
        </div>
      }
      <div class="table-wrap"><table class="table table-stack">
        <thead><tr><th>Name</th><th>Login</th><th>Role</th><th>Status</th><th>Last login</th><th></th></tr></thead>
        <tbody>
          @for (m of members(); track m.id) {
            <tr [style.opacity]="m.isActive ? 1 : .55">
              <td data-label="Name"><b>{{ m.name }}</b> @if (m.isYou) {<span class="badge brand">You</span>}</td>
              <td data-label="Login" class="small">{{ m.mobile }}<div class="muted">{{ m.email }}</div></td>
              <td data-label="Role">
                @if (canStaff && !m.isYou) {
                  <select class="select" style="min-height:38px;width:auto" [ngModel]="m.role" (ngModelChange)="update(m, $event, m.isActive)">
                    <option>Owner</option><option>Manager</option><option>Accountant</option></select>
                } @else { <span class="badge brand">{{ m.role }}</span> }
              </td>
              <td data-label="Status">
                <span class="badge" [class.green]="m.isActive" [class.red]="!m.isActive">{{ m.isActive ? 'Active' : 'Disabled' }}</span>
                @if (m.isLocked) { <span class="badge gold">Locked</span> }
              </td>
              <td data-label="Last login" class="small">{{ m.lastLoginAt ? (m.lastLoginAt | date: 'd MMM, h:mm a') : 'never' }}</td>
              <td class="r" data-label="">
                @if (canStaff && !m.isYou) {
                  <button class="btn btn-ghost btn-sm" (click)="update(m, m.role, !m.isActive)">{{ m.isActive ? 'Disable' : 'Enable' }}</button>
                  <button class="btn btn-ghost btn-sm" (click)="resetPassword(m)">Reset password</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table></div>
      <div class="card-foot small muted">Changing a role or disabling a login signs that person out immediately. A vendor always keeps at least one active owner.</div>
    </div>
  }

  @if (tab() === 'roles') {
    <div class="card mt-4"><div class="table-wrap"><table class="table">
      <thead><tr><th>Permission</th>@for (r of roles(); track r.role) {<th class="center">{{ r.role }}</th>}</tr></thead>
      <tbody>
        @for (perm of allPerms(); track perm) {
          <tr><td><b class="small">{{ label(perm) }}</b><div class="xs muted">{{ perm }}</div></td>
            @for (r of roles(); track r.role) { <td class="center">@if (r.permissions.includes(perm)) {<span style="color:var(--green);font-weight:800">✔</span>} @else {<span class="muted">—</span>}</td> }
          </tr>
        }
      </tbody>
    </table></div>
    <div class="card-foot small muted">Operators also can work only for the function and counter they are assigned to, during its time window. Functions and Moi entry stay locked until OneMoi approves the vendor.</div></div>
  }`
})
export class CompanyPage implements OnInit {
  private api = inject(ApiService);
  protected auth = inject(AuthService);
  private toast = inject(ToastService);
  protected P = P;
  protected canEdit = this.auth.can(P.tenantProfileManage);
  protected canStaff = this.auth.can(P.tenantStaffManage);
  protected tab = signal<'profile' | 'staff' | 'roles'>('profile');
  protected p = signal<TenantProfile | null>(null);
  protected members = signal<TenantMember[]>([]);
  protected roles = signal<RolePermissions[]>([]);
  protected allPerms = signal<string[]>([]);
  protected busy = signal(false);
  protected adding = signal(false);
  protected error = signal('');
  protected errors = signal<Record<string, string[]>>({});
  protected logo = () => this.api.file(this.p()?.logoPath);
  f: any = {};
  nm: any = this.blank();

  async ngOnInit() {
    const p = await this.api.get<TenantProfile>('/api/vendor/profile');
    this.p.set(p); this.f = { ...p, primaryColor: p.primaryColor ?? '#8E1B3A' };
    if (this.auth.can(P.tenantStaffView)) {
      await this.loadMembers();
      const roles = await this.api.get<RolePermissions[]>('/api/vendor/roles');
      this.roles.set(roles);
      this.allPerms.set([...new Set(roles.flatMap(r => r.permissions))].sort());
    }
  }
  err(k: string) { return this.errors()[k]?.[0] ?? ''; }
  label(p: string) { return PERMISSION_LABELS[p] ?? p; }
  async loadMembers() { this.members.set(await this.api.get<TenantMember[]>('/api/vendor/members')); }

  async save() {
    this.busy.set(true); this.error.set(''); this.errors.set({});
    try { this.p.set(await this.api.put<TenantProfile>('/api/vendor/profile', this.f)); await this.auth.reloadProfile(); this.toast.ok('Profile saved'); }
    catch (e) { const a = apiError(e); this.error.set(a.message); this.errors.set(a.errors ?? {}); }
    finally { this.busy.set(false); }
  }
  async upload(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    try {
      const r = await this.api.upload<{ logoPath: string }>('/api/vendor/logo', file);
      this.p.update(p => (p ? { ...p, logoPath: r.logoPath } : p));
      await this.auth.reloadProfile();
      this.toast.ok('Logo updated');
    } catch (e) { this.toast.err(apiError(e).errors?.['logo']?.[0] ?? apiError(e).message); }
  }

  async addMember() {
    try { await this.api.post('/api/vendor/members', this.nm); this.toast.ok('Login created'); this.adding.set(false); this.nm = this.blank(); await this.loadMembers(); }
    catch (e) { const a = apiError(e); this.toast.err(Object.values(a.errors ?? {})[0]?.[0] ?? a.message); }
  }
  async update(m: TenantMember, role: string, isActive: boolean) {
    const what = !isActive ? `Disable ${m.name}'s login?` : role !== m.role ? `Change ${m.name} to ${role}?` : `Enable ${m.name}'s login?`;
    if (!confirm(what + ' They will be signed out immediately.')) { await this.loadMembers(); return; }
    try { await this.api.put(`/api/vendor/members/${m.id}`, { role, isActive }); this.toast.ok('Access updated'); }
    catch (e) { this.toast.err(apiError(e).message); }
    await this.loadMembers();
  }
  async resetPassword(m: TenantMember) {
    const pw = prompt(`New password for ${m.name} (8+ characters, letters and numbers):`);
    if (!pw) return;
    try { await this.api.post(`/api/vendor/members/${m.id}/reset-password`, { newPassword: pw }); this.toast.ok('Password reset. Their old sessions are signed out.'); await this.loadMembers(); }
    catch (e) { const a = apiError(e); this.toast.err(Object.values(a.errors ?? {})[0]?.[0] ?? a.message); }
  }
  private blank() { return { name: '', mobile: '', email: '', role: 'Manager', password: '' }; }
}
