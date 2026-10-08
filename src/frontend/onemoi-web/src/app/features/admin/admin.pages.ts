import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ApiService, apiError } from '../../core/api.service';
import { AdminDashboard, AdminTenant, AdminUser, AuditLog, NotificationLog, OtpLog } from '../../core/models';
import { InrPipe, ToastService } from '../../core/ui';
import { AuthService } from '../../core/auth.service';
import { IconComponent } from '../../shared/icon.component';

/** Platform KPIs */
@Component({
  selector: 'mk-admin-dashboard',
  imports: [InrPipe, IconComponent],
  template: `
  <div class="page-head"><div><h1>Platform overview</h1><p class="muted">OneMoi super admin</p></div></div>
  @if (d(); as d) {
    <div class="grid grid-4">
      <div class="stat"><div class="stat-label"><span class="stat-icon"><mk-icon name="store" [size]="18" /></span>Active vendors</div><div class="stat-value">{{ d.activeTenants }}</div><div class="xs muted">{{ d.pendingTenants }} pending · {{ d.suspendedTenants }} suspended</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon gold"><mk-icon name="calendar" [size]="18" /></span>Functions this month</div><div class="stat-value">{{ d.functionsThisMonth }}</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon green"><mk-icon name="users" [size]="18" /></span>Moi identities</div><div class="stat-value">{{ d.persons }}</div><div class="xs muted">{{ d.verifiedPersons }} verified by OTP · {{ d.users }} logins</div></div>
      <div class="stat"><div class="stat-label"><span class="stat-icon blue"><mk-icon name="rupee" [size]="18" /></span>Moi recorded this month</div><div class="stat-value">{{ d.moiThisMonth | inr }}</div><div class="xs muted">{{ d.entriesThisMonth }} entries</div></div>
    </div>
  }`
})
export class AdminDashboardPage implements OnInit {
  private api = inject(ApiService);
  protected d = signal<AdminDashboard | null>(null);
  async ngOnInit() { this.d.set(await this.api.get<AdminDashboard>('/api/admin/dashboard')); }
}

/** Approve / suspend Moi vendors */
@Component({
  selector: 'mk-admin-tenants',
  imports: [DatePipe, InrPipe],
  template: `
  <div class="page-head"><div><h1>Moi vendors</h1><p class="muted">Approve new vendors, suspend misuse</p></div></div>
  <div class="card"><div class="table-wrap"><table class="table table-stack">
    <thead><tr><th>Vendor</th><th>Code</th><th>Owner</th><th>Plan</th><th class="r">Functions</th><th class="r">Operators</th><th class="r">Total Moi</th><th>Status</th><th></th></tr></thead>
    <tbody>
      @for (t of rows(); track t.id) {
        <tr>
          <td data-label="Vendor"><div class="row" style="gap:10px">@if (t.logoPath) {<img class="tenant-logo" [src]="api.file(t.logoPath)" alt="" />}<div><b>{{ t.name }}</b><div class="xs muted">{{ t.city }} · since {{ t.createdAt | date: 'd MMM y' }}</div></div></div></td>
          <td data-label="Code"><span class="badge">{{ t.code }}</span></td>
          <td data-label="Owner">{{ t.ownerName }}<div class="xs muted">{{ t.mobile }} · {{ t.email }}</div></td>
          <td data-label="Plan">{{ t.plan }}</td>
          <td data-label="Functions" class="r num">{{ t.functions }}</td><td data-label="Operators" class="r num">{{ t.operators }}</td>
          <td data-label="Total Moi" class="r money">{{ t.totalMoi | inr }}</td>
          <td data-label="Status"><span class="badge" [class.green]="t.status === 'Active'" [class.gold]="t.status === 'Pending'" [class.red]="t.status === 'Suspended'">{{ t.status }}</span></td>
          <td class="r" data-label="">
            @if (t.status !== 'Active') { <button class="btn btn-success btn-sm" (click)="set(t, 'Active')">Approve</button> }
            @if (t.status === 'Active') { <button class="btn btn-danger btn-sm" (click)="set(t, 'Suspended')">Suspend</button> }
          </td>
        </tr>
      }
    </tbody>
  </table></div></div>`
})
export class AdminTenantsPage implements OnInit {
  protected api = inject(ApiService);
  private toast = inject(ToastService);
  protected rows = signal<AdminTenant[]>([]);
  ngOnInit() { this.load(); }
  async load() { this.rows.set(await this.api.get<AdminTenant[]>('/api/admin/tenants')); }
  async set(t: AdminTenant, status: string) {
    if (!confirm(`${status === 'Active' ? 'Approve' : 'Suspend'} ${t.name}?`)) return;
    try { await this.api.put(`/api/admin/tenants/${t.id}/status/${status}`); this.toast.ok(`${t.name}: ${status}`); await this.load(); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
}

/** All logins */
@Component({
  selector: 'mk-admin-users',
  imports: [DatePipe],
  template: `
  <div class="page-head"><div><h1>Users</h1><p class="muted">Latest 200 logins (auth.Users)</p></div></div>
  <div class="card"><div class="table-wrap"><table class="table table-stack">
    <thead><tr><th>#</th><th>Name</th><th>Mobile</th><th>E-mail</th><th>Type</th><th>Vendor</th><th>Status</th><th>Last login</th><th></th></tr></thead>
    <tbody>
      @for (u of rows(); track u.id) {
        <tr [style.opacity]="u.isActive ? 1 : .55"><td data-label="#" class="num muted">{{ u.id }}</td><td data-label="Name"><b>{{ u.fullName }}</b></td><td data-label="Mobile">{{ u.mobile }}</td>
          <td data-label="E-mail">{{ u.email }}</td><td data-label="Type"><span class="badge" [class.brand]="u.userType === 'TenantUser'" [class.gold]="u.userType === 'SuperAdmin'">{{ u.userType }}</span></td>
          <td data-label="Vendor">{{ u.tenant }}</td>
          <td data-label="Status"><span class="badge" [class.green]="u.isActive" [class.red]="!u.isActive">{{ u.isActive ? 'Active' : 'Disabled' }}</span>
            @if (u.isLocked) {<span class="badge gold">Locked</span>}</td>
          <td data-label="Last login" class="small">{{ u.lastLoginAt ? (u.lastLoginAt | date: 'd MMM, h:mm a') : '—' }}</td>
          <td class="r" data-label="">
            @if (canManage && u.id !== me) {
              @if (u.isLocked) { <button class="btn btn-ghost btn-sm" (click)="unlock(u)">Unlock</button> }
              <button class="btn btn-ghost btn-sm" (click)="toggle(u)">{{ u.isActive ? 'Disable' : 'Enable' }}</button>
            }
          </td></tr>
      }
    </tbody>
  </table></div>
  <div class="card-foot small muted">Disabling a login signs it out on its next request. Locked = too many wrong passwords (unlocks automatically after 15 minutes).</div></div>`
})
export class AdminUsersPage implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  protected canManage = this.auth.can('platform.users.manage');
  protected me = this.auth.profile()?.id;
  protected rows = signal<AdminUser[]>([]);
  ngOnInit() { this.load(); }
  async load() { this.rows.set(await this.api.get<AdminUser[]>('/api/admin/users')); }
  async toggle(u: AdminUser) {
    if (!confirm(`${u.isActive ? 'Disable' : 'Enable'} ${u.fullName}?`)) return;
    try { await this.api.put(`/api/admin/users/${u.id}/active/${!u.isActive}`); this.toast.ok('Updated'); await this.load(); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
  async unlock(u: AdminUser) {
    try { await this.api.post(`/api/admin/users/${u.id}/unlock`); this.toast.ok(`${u.fullName} unlocked`); await this.load(); }
    catch (e) { this.toast.err(apiError(e).message); }
  }
}

/** OTP requests, sent notifications and audit trail — shows what is stored in the database. */
@Component({
  selector: 'mk-admin-logs',
  imports: [DatePipe],
  template: `
  <div class="page-head"><div><h1>OTP, SMS &amp; audit logs</h1><p class="muted">Tables auth.OtpRequests, auth.NotificationLogs, auth.AuditLogs</p></div>
    <button class="btn btn-outline" (click)="load()">Refresh</button></div>
  <div class="tabs">
    <button [class.active]="tab() === 'otp'" (click)="tab.set('otp')">OTP requests</button>
    <button [class.active]="tab() === 'sms'" (click)="tab.set('sms')">SMS / e-mail sent</button>
    <button [class.active]="tab() === 'audit'" (click)="tab.set('audit')">Audit log</button>
  </div>
  <div class="card mt-4"><div class="table-wrap">
    @switch (tab()) {
      @case ('otp') {
        <table class="table table-stack"><thead><tr><th>#</th><th>To</th><th>Channel</th><th>Purpose</th><th>Sent</th><th>Expires</th><th>Attempts</th><th>Used</th></tr></thead><tbody>
          @for (o of otps(); track o.id) { <tr><td data-label="#" class="num muted">{{ o.id }}</td><td data-label="To">{{ o.destination }}</td><td data-label="Channel">{{ o.channel }}</td><td data-label="Purpose">{{ o.purpose }}</td>
            <td data-label="Sent" class="small">{{ o.createdAt | date: 'd MMM, h:mm:ss a' }}</td><td data-label="Expires" class="small">{{ o.expiresAt | date: 'h:mm:ss a' }}</td>
            <td data-label="Attempts" class="num">{{ o.attempts }}</td><td data-label="Used"><span class="badge" [class.green]="o.isUsed">{{ o.isUsed ? 'Used' : 'No' }}</span></td></tr> }
        </tbody></table>
        <p class="xs muted card-pad">Only a hash of each OTP is stored (column CodeHash). The plain code is never saved.</p>
      }
      @case ('sms') {
        <table class="table table-stack"><thead><tr><th>#</th><th>To</th><th>Channel</th><th>Message (OTP masked)</th><th>Status</th><th>When</th></tr></thead><tbody>
          @for (n of sms(); track n.id) { <tr><td data-label="#" class="num muted">{{ n.id }}</td><td data-label="To">{{ n.recipient }}</td><td data-label="Channel">{{ n.channel }}</td>
            <td data-label="Message" class="small">{{ n.body }}</td><td data-label="Status"><span class="badge">{{ n.status }}</span></td><td data-label="When" class="small">{{ n.createdAt | date: 'd MMM, h:mm a' }}</td></tr> }
        </tbody></table>
      }
      @case ('audit') {
        <table class="table table-stack"><thead><tr><th>#</th><th>Action</th><th>Who</th><th>Vendor</th><th>Details</th><th>IP</th><th>When</th></tr></thead><tbody>
          @for (a of audit(); track a.id) { <tr><td data-label="#" class="num muted">{{ a.id }}</td><td data-label="Action"><span class="badge brand">{{ a.action }}</span></td>
            <td data-label="Who">{{ a.principalType }} {{ a.principalId }}</td><td data-label="Vendor">{{ a.tenantId }}</td><td data-label="Details" class="small">{{ a.details }}</td>
            <td data-label="IP" class="small">{{ a.ipAddress }}</td><td data-label="When" class="small">{{ a.createdAt | date: 'd MMM, h:mm a' }}</td></tr> }
        </tbody></table>
      }
    }
  </div></div>`
})
export class AdminLogsPage implements OnInit {
  private api = inject(ApiService);
  protected tab = signal<'otp' | 'sms' | 'audit'>('otp');
  protected otps = signal<OtpLog[]>([]);
  protected sms = signal<NotificationLog[]>([]);
  protected audit = signal<AuditLog[]>([]);
  ngOnInit() { this.load(); }
  async load() {
    this.otps.set(await this.api.get<OtpLog[]>('/api/admin/otp-requests'));
    this.sms.set(await this.api.get<NotificationLog[]>('/api/admin/notifications'));
    this.audit.set(await this.api.get<AuditLog[]>('/api/admin/audit'));
  }
}
