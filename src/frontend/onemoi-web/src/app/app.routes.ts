import { Routes } from '@angular/router';
import { areaGuard, guestGuard, loggedInGuard, permissionGuard } from './core/guards';
import { P } from './core/permissions';
import { ShellComponent } from './layout/shell.component';

/**
 * Three layers of protection on every page:
 *   1. areaGuard        → right kind of login (admin / vendor / operator / individual)
 *   2. permissionGuard  → role has the permission in data.permission
 *   3. the API          → checks the same permission again + tenant isolation (the real security)
 * Pages are lazy-loaded so each user downloads only what they use.
 */
const need = (permission: string) => ({ data: { permission }, canActivate: [permissionGuard] });

export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login.page').then(m => m.LoginPage) },
  { path: 'otp', loadComponent: () => import('./features/auth/otp.page').then(m => m.OtpPage) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./features/auth/register.page').then(m => m.RegisterPage) },
  { path: 'forgot-password', canActivate: [guestGuard], loadComponent: () => import('./features/account/account.pages').then(m => m.ForgotPasswordPage) },

  {
    path: 'admin', component: ShellComponent, canActivate: [areaGuard('admin')],
    children: [
      { path: 'dashboard', ...need(P.platformDashboard), loadComponent: () => import('./features/admin/admin.pages').then(m => m.AdminDashboardPage) },
      { path: 'tenants', ...need(P.platformTenantsView), loadComponent: () => import('./features/admin/admin.pages').then(m => m.AdminTenantsPage) },
      { path: 'users', ...need(P.platformUsersView), loadComponent: () => import('./features/admin/admin.pages').then(m => m.AdminUsersPage) },
      { path: 'logs', ...need(P.platformLogsView), loadComponent: () => import('./features/admin/admin.pages').then(m => m.AdminLogsPage) },
      { path: 'masters', ...need(P.platformMastersManage), loadComponent: () => import('./features/vendor/masters.page').then(m => m.MastersPage) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  {
    path: 'vendor', component: ShellComponent, canActivate: [areaGuard('vendor')],
    children: [
      { path: 'dashboard', ...need(P.tenantDashboard), loadComponent: () => import('./features/vendor/dashboard.page').then(m => m.VendorDashboardPage) },
      { path: 'functions', ...need(P.functionsView), loadComponent: () => import('./features/vendor/functions-list.page').then(m => m.FunctionsListPage) },
      { path: 'functions/new', ...need(P.functionsManage), loadComponent: () => import('./features/vendor/function-form.page').then(m => m.FunctionFormPage) },
      { path: 'functions/:id', ...need(P.functionsView), loadComponent: () => import('./features/vendor/function-detail.page').then(m => m.FunctionDetailPage) },
      { path: 'functions/:id/edit', ...need(P.functionsManage), loadComponent: () => import('./features/vendor/function-form.page').then(m => m.FunctionFormPage) },
      { path: 'functions/:id/entry', ...need(P.moiEntry), loadComponent: () => import('./features/moi/moi-entry.page').then(m => m.MoiEntryPage) },
      { path: 'functions/:id/report', ...need(P.reportsView), loadComponent: () => import('./features/moi/report.page').then(m => m.ReportPage) },
      { path: 'entry', ...need(P.moiEntry), loadComponent: () => import('./features/moi/moi-entry.page').then(m => m.MoiEntryPage) },
      { path: 'operators', ...need(P.operatorsView), loadComponent: () => import('./features/vendor/operators.page').then(m => m.OperatorsPage) },
      { path: 'masters', ...need(P.mastersView), loadComponent: () => import('./features/vendor/masters.page').then(m => m.MastersPage) },
      { path: 'company', ...need(P.tenantProfileView), loadComponent: () => import('./features/vendor/company.page').then(m => m.CompanyPage) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  {
    path: 'operator', component: ShellComponent, canActivate: [areaGuard('operator')],
    children: [
      { path: 'home', loadComponent: () => import('./features/operator/operator-home.page').then(m => m.OperatorHomePage) },
      { path: 'counter', redirectTo: 'home' },
      { path: 'entry/:id', ...need(P.moiEntry), loadComponent: () => import('./features/moi/moi-entry.page').then(m => m.MoiEntryPage) },
      { path: '', pathMatch: 'full', redirectTo: 'home' }
    ]
  },
  {
    path: 'me', component: ShellComponent, canActivate: [areaGuard('individual')],
    children: [
      { path: 'moi', ...need(P.meMoiView), loadComponent: () => import('./features/me/my-moi.page').then(m => m.MyMoiPage) },
      { path: 'functions', ...need(P.meHostedView), loadComponent: () => import('./features/me/hosted.page').then(m => m.HostedPage) },
      { path: 'functions/:id/report', ...need(P.reportsView), loadComponent: () => import('./features/moi/report.page').then(m => m.ReportPage) },
      { path: 'profile', ...need(P.meProfileManage), loadComponent: () => import('./features/me/profile.page').then(m => m.ProfilePage) },
      { path: '', pathMatch: 'full', redirectTo: 'moi' }
    ]
  },
  {
    // Shared by every kind of login
    path: '', component: ShellComponent, canActivate: [loggedInGuard],
    children: [
      { path: 'account', loadComponent: () => import('./features/account/account.pages').then(m => m.AccountPage) },
      { path: 'forbidden', loadComponent: () => import('./features/account/account.pages').then(m => m.ForbiddenPage) }
    ]
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' }
];
