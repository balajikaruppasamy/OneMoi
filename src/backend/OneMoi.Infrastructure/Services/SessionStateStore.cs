using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OneMoi.Application.Common;
using OneMoi.Domain.Enums;
using OneMoi.Infrastructure.Persistence;

namespace OneMoi.Infrastructure.Services;

/// <summary>
/// Caches "is this login / vendor still allowed?" for 30 seconds so every API request can be checked
/// without a heavy database hit. Changes made in this app call Invalidate… so they apply instantly.
/// </summary>
public class SessionStateStore(IMemoryCache cache, IServiceScopeFactory scopes) : ISessionStateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public async Task<PrincipalState?> GetPrincipalAsync(PrincipalType type, int id, CancellationToken ct = default)
    {
        var key = $"principal:{type}:{id}";
        if (cache.TryGetValue(key, out PrincipalState? state)) return state;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (type == PrincipalType.Operator)
        {
            state = await db.Operators.IgnoreQueryFilters().AsNoTracking().Where(o => o.Id == id && !o.IsDeleted)
                .Select(o => new PrincipalState(o.IsActive, o.SecurityStamp, o.LockoutUntil, o.TenantId, null))
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            var u = await db.Users.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.IsActive, x.SecurityStamp, x.LockoutUntil, x.UserType }).FirstOrDefaultAsync(ct);
            if (u != null)
            {
                int? tenantId = null; TenantRole? role = null;
                if (u.UserType == UserType.TenantUser)
                {
                    var m = await db.TenantMembers.IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.UserId == id && x.IsActive && !x.IsDeleted).OrderBy(x => x.Role)
                        .Select(x => new { x.TenantId, x.Role }).FirstOrDefaultAsync(ct);
                    tenantId = m?.TenantId; role = m?.Role;
                }
                // A vendor user whose membership was removed is treated as inactive
                state = new PrincipalState(u.IsActive && (u.UserType != UserType.TenantUser || tenantId != null), u.SecurityStamp, u.LockoutUntil, tenantId, role);
            }
        }
        cache.Set(key, state, Ttl);
        return state;
    }

    public async Task<TenantStatus?> GetTenantStatusAsync(int tenantId, CancellationToken ct = default)
    {
        var key = $"tenant:{tenantId}";
        if (cache.TryGetValue(key, out TenantStatus? status)) return status;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        status = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => (TenantStatus?)t.Status).FirstOrDefaultAsync(ct);
        cache.Set(key, status, Ttl);
        return status;
    }

    public void InvalidatePrincipal(PrincipalType type, int id) => cache.Remove($"principal:{type}:{id}");
    public void InvalidateTenant(int tenantId) => cache.Remove($"tenant:{tenantId}");
}
