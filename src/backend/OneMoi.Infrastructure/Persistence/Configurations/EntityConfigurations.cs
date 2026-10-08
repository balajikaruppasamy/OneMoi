using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneMoi.Domain.Entities;

namespace OneMoi.Infrastructure.Persistence.Configurations;

/*  PostgreSQL database layout (schemas) — all names are lower_snake_case,
 *  so in pgAdmin you can simply write:  select * from moi.moi_entries;
 *  ─────────────────────────
 *  auth.*    logins: users, otp_requests, refresh_tokens, notification_logs, audit_logs
 *  core.*    vendors: plans, tenants, tenant_members, operators
 *  master.*  lookups: function_types, moi_categories, gift_item_types, expense_categories, denominations
 *  evt.*     events: functions, counters, operator_assignments
 *  moi.*     money: persons, moi_entries, moi_entry_denominations, moi_entry_gifts, function_expenses
 *
 *  Text columns are "character varying" (UTF-8), so Tamil is stored correctly.
 *  Columns become snake_case automatically (UseSnakeCaseNamingConvention): NameTa → name_ta.
 */

internal static class Sql
{
    /// <summary>Partial-index condition, e.g. mobile IS NOT NULL AND is_deleted = false</summary>
    public static string NotNullAndActive<T>(EntityTypeBuilder<T> _, string column) where T : class =>
        $"{Snake(column)} IS NOT NULL AND is_deleted = false";

    public static string NotNull(string column) => $"{Snake(column)} IS NOT NULL";

    private static string Snake(string pascal) =>
        string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}

// ───────────────────────────── auth ─────────────────────────────

public class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("users", "auth");
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.FullNameTa).HasMaxLength(150);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Mobile).HasMaxLength(15);
        b.Property(x => x.PasswordHash).HasMaxLength(100);
        b.Property(x => x.SecurityStamp).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.Mobile, x.UserType }).IsUnique().HasFilter(Sql.NotNullAndActive(b, "Mobile"));
        b.HasIndex(x => x.Email).IsUnique().HasFilter(Sql.NotNullAndActive(b, "Email"));
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OtpRequestConfig : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> b)
    {
        b.ToTable("otp_requests", "auth");
        b.Property(x => x.Destination).HasMaxLength(150).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.HasIndex(x => new { x.Destination, x.Purpose, x.CreatedAt });
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens", "auth");
        b.Property(x => x.TokenHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.CreatedByIp).HasMaxLength(50);
        b.Property(x => x.RevokedReason).HasMaxLength(30);
        b.Property(x => x.FamilyId).HasMaxLength(40).IsRequired();
        b.Property(x => x.ReplacedByHash).HasMaxLength(100);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.FamilyId);
        b.HasIndex(x => new { x.PrincipalType, x.PrincipalId });
    }
}

public class NotificationLogConfig : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> b)
    {
        b.ToTable("notification_logs", "auth");
        b.Property(x => x.Recipient).HasMaxLength(150).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Provider).HasMaxLength(50);
        b.Property(x => x.Error).HasMaxLength(1000);
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs", "auth");
        b.Property(x => x.Action).HasMaxLength(60).IsRequired();
        b.Property(x => x.EntityName).HasMaxLength(60);
        b.Property(x => x.EntityId).HasMaxLength(40);
        b.Property(x => x.Details).HasMaxLength(1000);
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.HasIndex(x => new { x.TenantId, x.CreatedAt });
    }
}

// ───────────────────────────── core ─────────────────────────────

public class PlanConfig : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("plans", "core");
        b.Property(x => x.Name).HasMaxLength(60).IsRequired();
        b.Property(x => x.PriceMonthly).HasPrecision(12, 2);
        b.Property(x => x.PricePerFunction).HasPrecision(12, 2);
    }
}

public class TenantConfig : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants", "core");
        b.Property(x => x.Code).HasMaxLength(12).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(150);
        b.Property(x => x.Tagline).HasMaxLength(200);
        b.Property(x => x.OwnerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.AltMobile).HasMaxLength(15);
        b.Property(x => x.AddressLine).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.District).HasMaxLength(80);
        b.Property(x => x.State).HasMaxLength(80);
        b.Property(x => x.Pincode).HasMaxLength(10);
        b.Property(x => x.Gstin).HasMaxLength(15);
        b.Property(x => x.LogoPath).HasMaxLength(300);
        b.Property(x => x.PrimaryColor).HasMaxLength(10);
        b.Property(x => x.ReceiptHeader).HasMaxLength(300);
        b.Property(x => x.ReceiptFooter).HasMaxLength(300);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TenantMemberConfig : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> b)
    {
        b.ToTable("tenant_members", "core");
        b.HasOne(x => x.Tenant).WithMany(t => t.Members).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany(u => u.Memberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
    }
}

public class OperatorConfig : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> b)
    {
        b.ToTable("operators", "core");
        b.Property(x => x.Code).HasMaxLength(12).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(150);
        b.Property(x => x.Mobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.PinHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.SecurityStamp).HasMaxLength(40).IsRequired();
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

// ───────────────────────────── master ─────────────────────────────

public abstract class MasterConfig<T> : IEntityTypeConfiguration<T> where T : MasterEntity
{
    protected abstract string Table { get; }
    public virtual void Configure(EntityTypeBuilder<T> b)
    {
        b.ToTable(Table, "master");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(100);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SortOrder });
    }
}
public class FunctionTypeConfig : MasterConfig<FunctionType> { protected override string Table => "function_types"; }
public class ExpenseCategoryConfig : MasterConfig<ExpenseCategory> { protected override string Table => "expense_categories"; }
public class MoiCategoryConfig : MasterConfig<MoiCategory>
{
    protected override string Table => "moi_categories";
    public override void Configure(EntityTypeBuilder<MoiCategory> b) { base.Configure(b); b.Property(x => x.Color).HasMaxLength(10); }
}
public class GiftItemTypeConfig : MasterConfig<GiftItemType>
{
    protected override string Table => "gift_item_types";
    public override void Configure(EntityTypeBuilder<GiftItemType> b) { base.Configure(b); b.Property(x => x.Unit).HasMaxLength(30); }
}

public class DenominationConfig : IEntityTypeConfiguration<Denomination>
{
    public void Configure(EntityTypeBuilder<Denomination> b)
    {
        b.ToTable("denominations", "master");
        b.HasIndex(x => x.Value).IsUnique();
    }
}

// ───────────────────────────── evt ─────────────────────────────

public class FunctionConfig : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> b)
    {
        b.ToTable("functions", "evt");
        b.Property(x => x.Code).HasMaxLength(8).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(200);
        b.Property(x => x.OwnerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.OwnerNameTa).HasMaxLength(150);
        b.Property(x => x.OwnerMobile).HasMaxLength(15).IsRequired();
        b.Property(x => x.OwnerEmail).HasMaxLength(150);
        b.Property(x => x.Location).HasMaxLength(200).IsRequired();
        b.Property(x => x.LocationTa).HasMaxLength(200);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.OtherDetails).HasMaxLength(1000);
        b.Property(x => x.FunctionDate).HasColumnType("date");
        b.Property(x => x.StartTime).HasColumnType("time");            // clock time, not a duration
        b.Property(x => x.EndTime).HasColumnType("time");
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.FunctionDate });
        b.HasIndex(x => x.OwnerMobile);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FunctionType).WithMany().HasForeignKey(x => x.FunctionTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CounterConfig : IEntityTypeConfiguration<Counter>
{
    public void Configure(EntityTypeBuilder<Counter> b)
    {
        b.ToTable("counters", "evt");
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.HasOne(x => x.Function).WithMany(f => f.Counters).HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.FunctionId, x.Number });
    }
}

public class OperatorAssignmentConfig : IEntityTypeConfiguration<OperatorAssignment>
{
    public void Configure(EntityTypeBuilder<OperatorAssignment> b)
    {
        b.ToTable("operator_assignments", "evt");
        // Working window is local (India) time, not UTC
        b.Property(x => x.ValidFrom).HasColumnType("timestamp without time zone");
        b.Property(x => x.ValidTo).HasColumnType("timestamp without time zone");
        b.HasOne(x => x.Function).WithMany(f => f.Assignments).HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Counter).WithMany().HasForeignKey(x => x.CounterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OperatorId, x.IsActive });
    }
}

// ───────────────────────────── moi ─────────────────────────────

public class PersonConfig : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("persons", "moi");
        b.Property(x => x.Mobile).HasMaxLength(15);
        b.Property(x => x.Initial).HasMaxLength(12);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(150);
        b.Property(x => x.SpouseInitial).HasMaxLength(12);
        b.Property(x => x.SpouseName).HasMaxLength(150);
        b.Property(x => x.SpouseNameTa).HasMaxLength(150);
        b.Property(x => x.Work).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.CityTa).HasMaxLength(80);
        b.HasIndex(x => x.Mobile).IsUnique().HasFilter(Sql.NotNullAndActive(b, "Mobile"));
    }
}

public class MoiEntryConfig : IEntityTypeConfiguration<MoiEntry>
{
    public void Configure(EntityTypeBuilder<MoiEntry> b)
    {
        b.ToTable("moi_entries", "moi");
        b.Property(x => x.ReceiptNo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Mobile).HasMaxLength(15);
        b.Property(x => x.Initial).HasMaxLength(12);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameTa).HasMaxLength(150);
        b.Property(x => x.SpouseInitial).HasMaxLength(12);
        b.Property(x => x.SpouseName).HasMaxLength(150);
        b.Property(x => x.SpouseNameTa).HasMaxLength(150);
        b.Property(x => x.Work).HasMaxLength(100);
        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.CityTa).HasMaxLength(80);
        b.Property(x => x.Amount).HasPrecision(14, 2);
        b.Property(x => x.PaymentRef).HasMaxLength(60);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.ReversalReason).HasMaxLength(300);
        b.Property(x => x.ClientRef).HasMaxLength(60);

        b.HasOne(x => x.Function).WithMany().HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Counter).WithMany().HasForeignKey(x => x.CounterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MoiCategory).WithMany().HasForeignKey(x => x.MoiCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Denominations).WithOne().HasForeignKey(d => d.MoiEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Gifts).WithOne().HasForeignKey(g => g.MoiEntryId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.FunctionId, x.SerialNo }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Mobile });
        b.HasIndex(x => x.PersonId);
        b.HasIndex(x => new { x.Name, x.City });
        b.HasIndex(x => x.ClientRef).IsUnique().HasFilter(Sql.NotNull("ClientRef"));
    }
}

public class MoiEntryDenominationConfig : IEntityTypeConfiguration<MoiEntryDenomination>
{
    public void Configure(EntityTypeBuilder<MoiEntryDenomination> b)
    {
        b.ToTable("moi_entry_denominations", "moi");
        b.Property(x => x.Total).HasPrecision(14, 2);
    }
}

public class MoiEntryGiftConfig : IEntityTypeConfiguration<MoiEntryGift>
{
    public void Configure(EntityTypeBuilder<MoiEntryGift> b)
    {
        b.ToTable("moi_entry_gifts", "moi");
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.Property(x => x.DescriptionTa).HasMaxLength(200);
        b.Property(x => x.Quantity).HasPrecision(10, 3);
        b.Property(x => x.EstimatedValue).HasPrecision(14, 2);
        b.HasOne(x => x.GiftItemType).WithMany().HasForeignKey(x => x.GiftItemTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FunctionExpenseConfig : IEntityTypeConfiguration<FunctionExpense>
{
    public void Configure(EntityTypeBuilder<FunctionExpense> b)
    {
        b.ToTable("function_expenses", "moi");
        b.Property(x => x.TakenByName).HasMaxLength(150).IsRequired();
        b.Property(x => x.TakenByNameTa).HasMaxLength(150);
        b.Property(x => x.Relation).HasMaxLength(80);
        b.Property(x => x.TakenByMobile).HasMaxLength(15);
        b.Property(x => x.Purpose).HasMaxLength(200).IsRequired();
        b.Property(x => x.Amount).HasPrecision(14, 2);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne(x => x.Function).WithMany().HasForeignKey(x => x.FunctionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.FunctionId);
    }
}
