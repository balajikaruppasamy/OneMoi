/**
 * Permission names — must match the backend file
 * OneMoi.Application/Common/Security/Permissions.cs
 * The server sends the logged-in user's list in SessionProfile.permissions.
 */
export const P = {
  platformDashboard: 'platform.dashboard',
  platformTenantsView: 'platform.tenants.view',
  platformTenantsManage: 'platform.tenants.manage',
  platformUsersView: 'platform.users.view',
  platformUsersManage: 'platform.users.manage',
  platformLogsView: 'platform.logs.view',
  platformMastersManage: 'platform.masters.manage',

  tenantDashboard: 'tenant.dashboard',
  tenantProfileView: 'tenant.profile.view',
  tenantProfileManage: 'tenant.profile.manage',
  tenantStaffView: 'tenant.staff.view',
  tenantStaffManage: 'tenant.staff.manage',

  mastersView: 'masters.view',
  mastersManage: 'masters.manage',
  functionsView: 'functions.view',
  functionsManage: 'functions.manage',
  operatorsView: 'operators.view',
  operatorsManage: 'operators.manage',
  operatorsAssign: 'operators.assign',
  moiView: 'moi.view',
  moiEntry: 'moi.entry',
  moiReverseAny: 'moi.reverse.any',
  moiReverseOwn: 'moi.reverse.own',
  expensesView: 'expenses.view',
  expensesCreate: 'expenses.create',
  expensesDelete: 'expenses.delete',
  reportsView: 'reports.view',
  reportsExport: 'reports.export',

  meMoiView: 'me.moi.view',
  meProfileManage: 'me.profile.manage',
  meHostedView: 'me.hosted.view',
  accountManage: 'account.manage'
} as const;

/** Friendly labels for the "Roles & access" screen. */
export const PERMISSION_LABELS: Record<string, string> = {
  'tenant.dashboard': 'See dashboard',
  'tenant.profile.view': 'View company profile',
  'tenant.profile.manage': 'Edit company profile & logo',
  'tenant.staff.view': 'View staff logins',
  'tenant.staff.manage': 'Add staff, change roles, reset passwords',
  'masters.view': 'View masters',
  'masters.manage': 'Edit masters',
  'functions.view': 'View functions',
  'functions.manage': 'Create / edit / close functions',
  'operators.view': 'View operators',
  'operators.manage': 'Add operators, reset PIN',
  'operators.assign': 'Assign operators to counters',
  'moi.view': 'View Moi entries',
  'moi.entry': 'Record Moi',
  'moi.reverse.any': 'Correct any Moi entry',
  'moi.reverse.own': 'Correct own entry (10 min)',
  'expenses.view': 'View expenses',
  'expenses.create': 'Record expenses',
  'expenses.delete': 'Remove expenses',
  'reports.view': 'View reports',
  'reports.export': 'Download reports',
  'account.manage': 'Change own password'
};
