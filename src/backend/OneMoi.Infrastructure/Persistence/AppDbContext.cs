using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OneMoi.Application.Common;
using OneMoi.Domain.Common;
using OneMoi.Domain.Entities;

namespace OneMoi.Infrastructure.Persistence;

/// <summary>
/// EF Core database context.
///  • Tables are grouped in schemas: auth, core, master, evt, moi (see Configurations).
///  • Global query filters hide soft-deleted rows and other vendors' rows.
///  • Audit columns (CreatedAt/By, UpdatedAt/By) are filled automatically.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
    : DbContext(options), IAppDbContext
{
    /// <summary>Used inside query filters; EF evaluates it per query.</summary>
    public int? CurrentTenantId => currentUser?.TenantId;

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<FunctionType> FunctionTypes => Set<FunctionType>();
    public DbSet<MoiCategory> MoiCategories => Set<MoiCategory>();
    public DbSet<GiftItemType> GiftItemTypes => Set<GiftItemType>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Denomination> Denominations => Set<Denomination>();
    public DbSet<Function> Functions => Set<Function>();
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<OperatorAssignment> OperatorAssignments => Set<OperatorAssignment>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<MoiEntry> MoiEntries => Set<MoiEntry>();
    public DbSet<MoiEntryDenomination> MoiEntryDenominations => Set<MoiEntryDenomination>();
    public DbSet<MoiEntryGift> MoiEntryGifts => Set<MoiEntryGift>();
    public DbSet<FunctionExpense> FunctionExpenses => Set<FunctionExpense>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Soft delete only (global, not tenant-owned) ──
        b.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Plan>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Person>().HasQueryFilter(e => !e.IsDeleted);

        // ── Tenant isolation: a vendor only ever sees its own rows ──
        b.Entity<TenantMember>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<Operator>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<Function>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<Counter>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<OperatorAssignment>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<MoiEntry>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);
        b.Entity<FunctionExpense>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == CurrentTenantId);

        // ── Masters: system rows (TenantId null) + my vendor's own rows ──
        b.Entity<FunctionType>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || e.TenantId == CurrentTenantId));
        b.Entity<MoiCategory>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || e.TenantId == CurrentTenantId));
        b.Entity<GiftItemType>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || e.TenantId == CurrentTenantId));
        b.Entity<ExpenseCategory>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || e.TenantId == CurrentTenantId));

        // ── Timestamps are stored in UTC; mark them as UTC when read so the UI converts to IST correctly ──
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(v => v, v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
        string[] utcColumns = { "CreatedAt", "UpdatedAt", "EntryAt", "ExpiresAt", "UsedAt", "LastLoginAt", "RevokedAt", "TrialEndsAt", "LockoutUntil", "PasswordChangedAt" };
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var p in entity.GetProperties().Where(p => utcColumns.Contains(p.Name)))
                p.SetValueConverter(p.ClrType == typeof(DateTime) ? utc : utcNullable);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= currentUser?.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser?.UserId;
                    break;
                case EntityState.Deleted:      // never hard-delete business rows
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser?.UserId;
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
