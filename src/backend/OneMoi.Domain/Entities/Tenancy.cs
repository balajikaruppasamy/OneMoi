using OneMoi.Domain.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Domain.Entities;

/// <summary>Subscription plan sold to Moi vendors.</summary>
public class Plan : AuditableEntity
{
    public string Name { get; set; } = "";
    public decimal PriceMonthly { get; set; }
    public decimal PricePerFunction { get; set; }
    public int MaxOperators { get; set; }
    public int MaxFunctionsPerMonth { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A Moi vendor company (JD Moi Tech, Meenakshi Moi …) with its own branding.</summary>
public class Tenant : AuditableEntity
{
    public string Code { get; set; } = "";             // short login code, e.g. JDMOI
    public string Name { get; set; } = "";
    public string? NameTa { get; set; }
    public string? Tagline { get; set; }
    public string OwnerName { get; set; } = "";
    public string? Email { get; set; }
    public string Mobile { get; set; } = "";
    public string? AltMobile { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string State { get; set; } = "Tamil Nadu";
    public string? Pincode { get; set; }
    public string? Gstin { get; set; }

    // Branding used on receipts and reports
    public string? LogoPath { get; set; }              // /uploads/tenants/{id}/logo.png
    public string? PrimaryColor { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    public ICollection<TenantMember> Members { get; set; } = new List<TenantMember>();
}

/// <summary>Links a login account to a tenant with a role.</summary>
public class TenantMember : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public TenantRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Counter staff. Logs in with Tenant code + Operator code + PIN.</summary>
public class Operator : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public string Code { get; set; } = "";             // OP-101 (unique per tenant)
    public string Name { get; set; } = "";
    public string? NameTa { get; set; }
    public string Mobile { get; set; } = "";
    public string PinHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedPinCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
}
