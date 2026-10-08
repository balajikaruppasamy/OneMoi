namespace OneMoi.Domain.Common;

/// <summary>Every table has an integer identity key.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

/// <summary>Adds who/when audit columns and soft delete. Filled automatically by the DbContext.</summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Rows that belong to one Moi vendor (tenant). The DbContext applies a global
/// filter so a vendor can never read another vendor's rows.
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
