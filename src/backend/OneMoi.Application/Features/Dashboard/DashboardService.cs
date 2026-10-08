using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Application.Features.Tenants;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Dashboard;

public record VendorDashboardDto(
    decimal TodayCollected, int TodayEntries, decimal TodayCash, decimal TodayUpi, decimal TodayExpenses,
    int FunctionsToday, int UpcomingFunctions, int ActiveOperators, decimal MonthCollected,
    List<TodayFunctionDto> Today, List<RecentEntryDto> Recent);

public record TodayFunctionDto(int Id, string Name, string? NameTa, string TypeName, string Location, string Status, int Entries, decimal Collected,
    List<string> Operators);

public record RecentEntryDto(int Id, DateTime EntryAt, string Name, string? NameTa, string? City, decimal Amount, string PaymentMode,
    string FunctionName, string? OperatorName, bool IsHighlighted);

public record AdminDashboardDto(int ActiveTenants, int PendingTenants, int SuspendedTenants, int FunctionsThisMonth, int Persons,
    int VerifiedPersons, int Users, decimal MoiThisMonth, int EntriesThisMonth);

public record AdminTenantDto(int Id, string Code, string Name, string? City, string OwnerName, string Mobile, string? Email, string Status,
    string? Plan, int Functions, int Operators, decimal TotalMoi, DateTime CreatedAt, string? LogoPath);

public record AdminUserDto(int Id, string FullName, string? Mobile, string? Email, string UserType, string? Tenant, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt, bool IsLocked);

public record OtpLogDto(int Id, string Channel, string Destination, string Purpose, DateTime CreatedAt, DateTime ExpiresAt, int Attempts, bool IsUsed);

public record NotificationLogDto(int Id, string Channel, string Recipient, string Body, string Status, DateTime CreatedAt);

public record AuditLogDto(int Id, string Action, string? Details, int? TenantId, string? PrincipalType, int? PrincipalId, string? IpAddress, DateTime CreatedAt);

public class DashboardService(IAppDbContext db, ICurrentUser current, ISessionStateStore sessions)
{
    public async Task<VendorDashboardDto> VendorAsync(CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantDashboard);
        var today = DateTime.Today;
        var startUtc = DateTime.Today.ToUniversalTime();       // start of today (server local time) in UTC
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todayEntries = db.MoiEntries.Where(m => m.EntryAt >= startUtc && m.Status == MoiEntryStatus.Active);
        var byMode = await todayEntries.GroupBy(m => m.PaymentMode).Select(g => new { g.Key, C = g.Count(), A = g.Sum(x => x.Amount) }).ToListAsync(ct);
        var todayExp = await db.FunctionExpenses.Where(e => e.EntryAt >= startUtc).SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var monthTotal = await db.MoiEntries.Where(m => m.Status == MoiEntryStatus.Active && m.Function!.FunctionDate >= monthStart)
            .SumAsync(m => (decimal?)m.Amount, ct) ?? 0;

        var functions = await db.Functions.Where(f => f.FunctionDate == today && f.Status != FunctionStatus.Cancelled)
            .OrderBy(f => f.StartTime)
            .Select(f => new TodayFunctionDto(f.Id, f.Name, f.NameTa, f.FunctionType!.Name, f.Location, f.Status.ToString(),
                db.MoiEntries.Count(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active),
                db.MoiEntries.Where(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active).Sum(m => (decimal?)m.Amount) ?? 0,
                db.OperatorAssignments.Where(a => a.FunctionId == f.Id && a.IsActive).OrderBy(a => a.Counter!.Number).Select(a => a.Operator!.Name).ToList()))
            .ToListAsync(ct);

        var recent = await db.MoiEntries.OrderByDescending(m => m.Id).Take(12)
            .Select(m => new RecentEntryDto(m.Id, m.EntryAt, (m.Initial != null ? m.Initial + " " : "") + m.Name, m.NameTa, m.City, m.Amount,
                m.PaymentMode.ToString(), m.Function!.Name, m.Operator!.Name, m.IsHighlighted))
            .ToListAsync(ct);

        return new VendorDashboardDto(
            byMode.Sum(x => x.A), byMode.Sum(x => x.C), byMode.Where(x => x.Key == PaymentMode.Cash).Sum(x => x.A),
            byMode.Where(x => x.Key == PaymentMode.Upi).Sum(x => x.A), todayExp, functions.Count,
            await db.Functions.CountAsync(f => f.FunctionDate > today && f.Status == FunctionStatus.Scheduled, ct),
            await db.Operators.CountAsync(o => o.IsActive, ct), monthTotal, functions, recent);
    }

    // ─────────────────────────── Super admin ───────────────────────────

    public async Task<AdminDashboardDto> AdminAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var entries = db.MoiEntries.IgnoreQueryFilters().Where(m => !m.IsDeleted && m.Status == MoiEntryStatus.Active && m.Function!.FunctionDate >= monthStart);
        return new AdminDashboardDto(
            await db.Tenants.CountAsync(t => t.Status == TenantStatus.Active, ct),
            await db.Tenants.CountAsync(t => t.Status == TenantStatus.Pending, ct),
            await db.Tenants.CountAsync(t => t.Status == TenantStatus.Suspended, ct),
            await db.Functions.IgnoreQueryFilters().CountAsync(f => !f.IsDeleted && f.FunctionDate >= monthStart, ct),
            await db.Persons.CountAsync(ct), await db.Persons.CountAsync(p => p.IsVerified, ct), await db.Users.CountAsync(ct),
            await entries.SumAsync(m => (decimal?)m.Amount, ct) ?? 0, await entries.CountAsync(ct));
    }

    public async Task<List<AdminTenantDto>> TenantsAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.Tenants.OrderBy(t => t.Status).ThenBy(t => t.Name)
            .Select(t => new AdminTenantDto(t.Id, t.Code, t.Name, t.City, t.OwnerName, t.Mobile, t.Email, t.Status.ToString(), t.Plan!.Name,
                db.Functions.IgnoreQueryFilters().Count(f => f.TenantId == t.Id && !f.IsDeleted),
                db.Operators.IgnoreQueryFilters().Count(o => o.TenantId == t.Id && !o.IsDeleted),
                db.MoiEntries.IgnoreQueryFilters().Where(m => m.TenantId == t.Id && !m.IsDeleted && m.Status == MoiEntryStatus.Active).Sum(m => (decimal?)m.Amount) ?? 0,
                t.CreatedAt, t.LogoPath))
            .ToListAsync(ct);
    }

    public async Task<TenantProfileDto> SetTenantStatusAsync(int id, string status, CancellationToken ct = default)
    {
        EnsureAdmin();
        if (!Enum.TryParse<TenantStatus>(status, true, out var st)) throw new ValidationException("status", "Unknown status");
        var t = await db.Tenants.Include(x => x.Plan).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Vendor");
        current.Demand(Permissions.PlatformTenantsManage);
        t.Status = st;
        db.AuditLogs.Add(new Domain.Entities.AuditLog { Action = "TENANT_" + st.ToString().ToUpper(), EntityName = "Tenant", EntityId = id.ToString(),
            PrincipalType = PrincipalType.User, PrincipalId = current.UserId, IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        sessions.InvalidateTenant(id);               // suspension applies to that vendor immediately
        return TenantService.ToDto(t);
    }

    public async Task<List<AdminUserDto>> UsersAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.Users.OrderByDescending(u => u.Id).Take(200)
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Mobile, u.Email, u.UserType.ToString(),
                db.TenantMembers.IgnoreQueryFilters().Where(m => m.UserId == u.Id).Select(m => m.Tenant!.Name).FirstOrDefault(),
                u.IsActive, u.LastLoginAt, u.CreatedAt, u.LockoutUntil != null && u.LockoutUntil > DateTime.UtcNow))
            .ToListAsync(ct);
    }

    /// <summary>Platform admin disables / enables any login. Disabling ends its sessions on the next request.</summary>
    public async Task SetUserActiveAsync(int userId, bool active, CancellationToken ct = default)
    {
        current.Demand(Permissions.PlatformUsersManage);
        if (userId == current.UserId) throw new AppException("You cannot disable your own account.");
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct) ?? throw new NotFoundException("User");
        u.IsActive = active;
        if (!active) u.SecurityStamp = Guid.NewGuid().ToString("N");
        db.AuditLogs.Add(new Domain.Entities.AuditLog { Action = active ? "USER_ENABLED" : "USER_DISABLED", EntityName = "User", EntityId = userId.ToString(),
            Details = u.FullName, PrincipalType = PrincipalType.User, PrincipalId = current.UserId, IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        sessions.InvalidatePrincipal(PrincipalType.User, userId);
    }

    public async Task UnlockUserAsync(int userId, CancellationToken ct = default)
    {
        current.Demand(Permissions.PlatformUsersManage);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct) ?? throw new NotFoundException("User");
        u.FailedLoginCount = 0;
        u.LockoutUntil = null;
        db.AuditLogs.Add(new Domain.Entities.AuditLog { Action = "USER_UNLOCKED", EntityName = "User", EntityId = userId.ToString(), Details = u.FullName,
            PrincipalType = PrincipalType.User, PrincipalId = current.UserId, IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        sessions.InvalidatePrincipal(PrincipalType.User, userId);
    }

    public async Task<List<OtpLogDto>> OtpLogAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.OtpRequests.OrderByDescending(o => o.Id).Take(100)
            .Select(o => new OtpLogDto(o.Id, o.Channel.ToString(), o.Destination, o.Purpose.ToString(), o.CreatedAt, o.ExpiresAt, o.Attempts, o.IsUsed))
            .ToListAsync(ct);
    }

    public async Task<List<NotificationLogDto>> NotificationsAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.NotificationLogs.OrderByDescending(o => o.Id).Take(100)
            .Select(n => new NotificationLogDto(n.Id, n.Channel.ToString(), n.Recipient, n.Body, n.Status.ToString(), n.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<List<AuditLogDto>> AuditAsync(CancellationToken ct = default)
    {
        EnsureAdmin();
        return await db.AuditLogs.OrderByDescending(o => o.Id).Take(150)
            .Select(a => new AuditLogDto(a.Id, a.Action, a.Details, a.TenantId, a.PrincipalType.ToString(), a.PrincipalId, a.IpAddress, a.CreatedAt))
            .ToListAsync(ct);
    }

    private void EnsureAdmin()
    {
        current.Demand(Permissions.PlatformDashboard);
    }
}
