using OneMoi.Domain.Enums;

namespace OneMoi.Application.Common.Security;

/// <summary>
/// Every action in OneMoi is protected by ONE of these permissions.
/// Controllers use [HasPermission(Permissions.X)]; services double-check with current.Can(Permissions.X).
/// The Angular app receives the caller's list and hides menus / buttons it cannot use.
/// </summary>
public static class Permissions
{
    // ── Platform (OneMoi super admin) ──
    public const string PlatformDashboard = "platform.dashboard";
    public const string PlatformTenantsView = "platform.tenants.view";
    public const string PlatformTenantsManage = "platform.tenants.manage";   // approve / suspend vendors
    public const string PlatformUsersView = "platform.users.view";
    public const string PlatformUsersManage = "platform.users.manage";       // activate / deactivate / unlock
    public const string PlatformLogsView = "platform.logs.view";
    public const string PlatformMastersManage = "platform.masters.manage";   // system default master rows

    // ── Vendor (tenant) ──
    public const string TenantDashboard = "tenant.dashboard";
    public const string TenantProfileView = "tenant.profile.view";
    public const string TenantProfileManage = "tenant.profile.manage";       // company details, logo
    public const string TenantStaffView = "tenant.staff.view";
    public const string TenantStaffManage = "tenant.staff.manage";           // add staff, change role, deactivate

    public const string MastersView = "masters.view";
    public const string MastersManage = "masters.manage";                    // vendor's own master rows

    public const string FunctionsView = "functions.view";
    public const string FunctionsManage = "functions.manage";                // create / edit / delete / status

    public const string OperatorsView = "operators.view";
    public const string OperatorsManage = "operators.manage";                // add / edit / reset PIN
    public const string OperatorsAssign = "operators.assign";                // assignment board

    public const string MoiView = "moi.view";
    public const string MoiEntry = "moi.entry";
    public const string MoiReverseAny = "moi.reverse.any";                   // correct any entry
    public const string MoiReverseOwn = "moi.reverse.own";                   // operator: own entry within N minutes

    public const string ExpensesView = "expenses.view";
    public const string ExpensesCreate = "expenses.create";
    public const string ExpensesDelete = "expenses.delete";

    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";

    // ── Individual ──
    public const string MeMoiView = "me.moi.view";
    public const string MeProfileManage = "me.profile.manage";
    public const string MeHostedView = "me.hosted.view";

    // ── Everyone logged in ──
    public const string AccountManage = "account.manage";                    // change password, logout all devices

    /// <summary>
    /// These change money or events, so they are blocked while the vendor is still
    /// "Pending" approval (the vendor can still set up profile, masters, operators, staff).
    /// </summary>
    public static readonly HashSet<string> RequireActiveTenant = new()
    {
        FunctionsManage, OperatorsAssign, MoiEntry, MoiReverseAny, MoiReverseOwn, ExpensesCreate, ExpensesDelete
    };
}

/// <summary>Role → permission matrix. This is the single place to change who can do what.</summary>
public static class RolePermissions
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Accountant = "Accountant";
    public const string Operator = "Operator";
    public const string Individual = "Individual";

    private static readonly string[] OwnerSet =
    {
        Permissions.TenantDashboard, Permissions.TenantProfileView, Permissions.TenantProfileManage,
        Permissions.TenantStaffView, Permissions.TenantStaffManage, Permissions.MastersView, Permissions.MastersManage,
        Permissions.FunctionsView, Permissions.FunctionsManage, Permissions.OperatorsView, Permissions.OperatorsManage,
        Permissions.OperatorsAssign, Permissions.MoiView, Permissions.MoiEntry, Permissions.MoiReverseAny,
        Permissions.ExpensesView, Permissions.ExpensesCreate, Permissions.ExpensesDelete,
        Permissions.ReportsView, Permissions.ReportsExport, Permissions.AccountManage
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Matrix = new Dictionary<string, HashSet<string>>
    {
        [SuperAdmin] = new()
        {
            Permissions.PlatformDashboard, Permissions.PlatformTenantsView, Permissions.PlatformTenantsManage,
            Permissions.PlatformUsersView, Permissions.PlatformUsersManage, Permissions.PlatformLogsView,
            Permissions.PlatformMastersManage, Permissions.MastersView, Permissions.ReportsView, Permissions.AccountManage
        },
        [Owner] = new(OwnerSet),
        // Manager = owner minus company branding and staff logins
        [Manager] = new(OwnerSet.Except(new[] { Permissions.TenantProfileManage, Permissions.TenantStaffManage })),
        // Accountant = read everything, record expenses, export reports
        [Accountant] = new()
        {
            Permissions.TenantDashboard, Permissions.TenantProfileView, Permissions.TenantStaffView, Permissions.MastersView,
            Permissions.FunctionsView, Permissions.OperatorsView, Permissions.MoiView,
            Permissions.ExpensesView, Permissions.ExpensesCreate, Permissions.ReportsView, Permissions.ReportsExport,
            Permissions.AccountManage
        },
        // Operator = counter work only, and only for the assigned function (checked in services)
        [Operator] = new()
        {
            Permissions.FunctionsView, Permissions.MastersView, Permissions.MoiView, Permissions.MoiEntry,
            Permissions.MoiReverseOwn, Permissions.ExpensesView, Permissions.ExpensesCreate
        },
        [Individual] = new()
        {
            Permissions.MeMoiView, Permissions.MeProfileManage, Permissions.MeHostedView, Permissions.ReportsView,
            Permissions.AccountManage
        }
    };

    /// <summary>Role key for a principal: SuperAdmin | Owner | Manager | Accountant | Operator | Individual.</summary>
    public static string? RoleKey(PrincipalType? principal, UserType? userType, TenantRole? tenantRole) =>
        principal == PrincipalType.Operator ? Operator
        : userType == UserType.SuperAdmin ? SuperAdmin
        : userType == UserType.Individual ? Individual
        : userType == UserType.TenantUser ? tenantRole?.ToString()
        : null;

    public static IReadOnlyCollection<string> For(string? roleKey) =>
        roleKey != null && Matrix.TryGetValue(roleKey, out var set) ? set : Array.Empty<string>();
}

public static class CurrentUserExtensions
{
    public static string? RoleKey(this ICurrentUser u) => RolePermissions.RoleKey(u.PrincipalType, u.UserType, u.TenantRole);

    public static bool Can(this ICurrentUser u, string permission) => RolePermissions.For(u.RoleKey()).Contains(permission);

    /// <summary>Throws 403 if the caller lacks the permission (defence in depth inside services).</summary>
    public static void Demand(this ICurrentUser u, string permission, string? message = null)
    {
        if (!u.Can(permission)) throw new ForbiddenException(message ?? "You do not have permission to do this.");
    }
}
