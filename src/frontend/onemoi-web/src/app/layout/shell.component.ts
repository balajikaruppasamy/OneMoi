import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { LangService } from '../core/ui';
import { P } from '../core/permissions';
import { IconComponent } from '../shared/icon.component';
import { LogoComponent } from '../shared/logo.component';

interface NavItem { label: string; icon: string; link: string; fab?: boolean; permission?: string; hostOnly?: boolean; }

/** Menus for each login type. An item is shown only if the user has its permission. */
const NAV: Record<string, NavItem[]> = {
  admin: [
    { label: 'Dashboard', icon: 'grid', link: '/admin/dashboard', permission: P.platformDashboard },
    { label: 'Vendors', icon: 'store', link: '/admin/tenants', permission: P.platformTenantsView },
    { label: 'Users', icon: 'users', link: '/admin/users', permission: P.platformUsersView },
    { label: 'OTP & SMS log', icon: 'mail', link: '/admin/logs', permission: P.platformLogsView },
    { label: 'Masters', icon: 'layers', link: '/admin/masters', permission: P.platformMastersManage }
  ],
  vendor: [
    { label: 'Dashboard', icon: 'grid', link: '/vendor/dashboard', permission: P.tenantDashboard },
    { label: 'Functions', icon: 'calendar', link: '/vendor/functions', permission: P.functionsView },
    { label: 'Moi entry', icon: 'receipt', link: '/vendor/entry', fab: true, permission: P.moiEntry },
    { label: 'Operators', icon: 'users', link: '/vendor/operators', permission: P.operatorsView },
    { label: 'Masters', icon: 'layers', link: '/vendor/masters', permission: P.mastersView },
    { label: 'Company', icon: 'store', link: '/vendor/company', permission: P.tenantProfileView }
  ],
  operator: [
    { label: 'My counter', icon: 'receipt', link: '/operator/home' }
  ],
  individual: [
    { label: 'My Moi', icon: 'wallet', link: '/me/moi', permission: P.meMoiView },
    { label: 'My functions', icon: 'calendar', link: '/me/functions', permission: P.meHostedView, hostOnly: true },
    { label: 'Profile', icon: 'user', link: '/me/profile', permission: P.meProfileManage }
  ]
};
const ACCOUNT: NavItem = { label: 'Account & security', icon: 'shield', link: '/account' };

@Component({
  selector: 'mk-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent, LogoComponent],
  template: `
  <div class="app">
    <aside class="sidebar">
      <div class="brand"><mk-logo /></div>
      @if (p()?.tenantName) {
        <div class="tenant-card">
          @if (logo()) { <img class="tenant-logo" [src]="logo()" alt="" /> } @else { <div class="avatar brand">{{ initials(p()!.tenantName!) }}</div> }
          <div class="grow"><div style="font-weight:800;font-size:.9rem">{{ p()!.tenantName }}</div>
            <div class="xs muted">{{ p()!.tenantCode }} · {{ p()!.role }}</div></div>
        </div>
      }
      @for (n of items(); track n.link) {
        <a class="nav-link" [routerLink]="n.link" routerLinkActive="active"><mk-icon [name]="n.icon" /><span>{{ n.label }}</span></a>
      }
      <div class="nav-section">Account</div>
      <a class="nav-link" [routerLink]="account.link" routerLinkActive="active"><mk-icon [name]="account.icon" /><span>{{ account.label }}</span></a>
      <div class="spacer"></div>
      <div class="tenant-card">
        <div class="avatar">{{ initials(p()?.name ?? '') }}</div>
        <div class="grow" style="min-width:0"><div style="font-weight:700;font-size:.88rem;overflow:hidden;text-overflow:ellipsis">{{ p()?.name }}</div>
          <div class="xs muted">{{ roleLabel() }}</div></div>
        <button class="btn btn-ghost btn-icon btn-sm" title="Log out" (click)="auth.logout()"><mk-icon name="logout" /></button>
      </div>
    </aside>

    <div class="main-col">
      <header class="topbar">
        <span class="hide-desktop"><mk-logo [size]="34" [word]="false" /></span>
        <div class="grow" style="min-width:0">
          <div class="crumb">{{ p()?.tenantName ?? roleLabel() }}</div>
          <div class="page-title" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis">{{ p()?.name }}</div>
        </div>
        <div class="segmented" style="width:auto">
          <button [class.active]="lang.lang() === 'en'" (click)="lang.set('en')">EN</button>
          <button class="ta" [class.active]="lang.lang() === 'ta'" (click)="lang.set('ta')">த</button>
        </div>
        <button class="btn btn-ghost btn-icon" (click)="toggleTheme()" title="Theme"><mk-icon [name]="dark() ? 'sun' : 'moon'" /></button>
        <a class="btn btn-ghost btn-icon hide-desktop" routerLink="/account" title="Account &amp; security"><mk-icon name="shield" /></a>
        <button class="btn btn-ghost btn-icon hide-desktop" (click)="auth.logout()" title="Log out"><mk-icon name="logout" /></button>
      </header>
      <main class="content"><router-outlet /></main>
    </div>
  </div>

  <nav class="bottom-nav">
    @for (n of items().slice(0, 5); track n.link) {
      @if (n.fab) {
        <a class="fab" [routerLink]="n.link" routerLinkActive="active"><span class="fab-inner"><mk-icon [name]="n.icon" /></span>{{ n.label }}</a>
      } @else {
        <a [routerLink]="n.link" routerLinkActive="active"><mk-icon [name]="n.icon" [size]="22" />{{ n.label }}</a>
      }
    }
  </nav>`
})
export class ShellComponent {
  protected auth = inject(AuthService);
  protected lang = inject(LangService);
  private api = inject(ApiService);
  protected p = this.auth.profile;
  protected account = ACCOUNT;
  protected logo = computed(() => this.api.file(this.p()?.tenantLogo));
  protected items = computed(() => {
    const area = this.auth.area() ?? 'individual';
    return NAV[area].filter(i => (!i.permission || this.auth.can(i.permission)) && (!i.hostOnly || this.p()?.isHost));
  });
  protected roleLabel = computed(() => {
    const p = this.p();
    if (!p) return '';
    if (p.principalType === 'Operator') return 'Moi Operator';
    return p.userType === 'SuperAdmin' ? 'Super Admin' : p.userType === 'TenantUser' ? `Vendor ${p.role}` : p.isHost ? 'Individual · Function host' : 'Individual';
  });
  protected dark = signal(document.documentElement.getAttribute('data-theme') === 'dark' || (!document.documentElement.getAttribute('data-theme') && matchMedia('(prefers-color-scheme: dark)').matches));

  toggleTheme() {
    const next = this.dark() ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    this.dark.set(next === 'dark');
    try { localStorage.setItem('mk-theme', next); } catch { /* ignore */ }
  }
  initials(s: string) { return s.split(/\s+/).filter(w => /[A-Za-z]/.test(w[0] ?? '')).map(w => w[0]).join('').slice(0, 2).toUpperCase() || 'OM'; }
}
