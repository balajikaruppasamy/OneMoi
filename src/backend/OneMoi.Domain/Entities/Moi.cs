using OneMoi.Domain.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Domain.Entities;

/// <summary>
/// Global Moi identity — NOT owned by any tenant. One mobile number = one person.
/// This is what lets Ram see Moi given through every vendor.
/// </summary>
public class Person : AuditableEntity
{
    public string? Mobile { get; set; }
    public string? Initial { get; set; }
    public string Name { get; set; } = "";
    public string? NameTa { get; set; }
    public string? SpouseInitial { get; set; }
    public string? SpouseName { get; set; }
    public string? SpouseNameTa { get; set; }
    public string? Work { get; set; }
    public string? City { get; set; }
    public string? CityTa { get; set; }
    public bool IsVerified { get; set; }               // true once the mobile owner logged in by OTP
}

/// <summary>
/// One Moi given at a function. Name fields are a snapshot of what the operator typed
/// (English + Tamil), so receipts and reports print exactly what was said at the counter.
/// </summary>
public class MoiEntry : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int FunctionId { get; set; }
    public Function? Function { get; set; }
    public int? CounterId { get; set; }
    public Counter? Counter { get; set; }
    public int? OperatorId { get; set; }
    public Operator? Operator { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }

    public int SerialNo { get; set; }                  // running number inside the function
    public string ReceiptNo { get; set; } = "";

    public string? Mobile { get; set; }
    public string? Initial { get; set; }               // "N."
    public string Name { get; set; } = "";             // "Ram"
    public string? NameTa { get; set; }                // "ராம்"
    public string? SpouseInitial { get; set; }
    public string? SpouseName { get; set; }
    public string? SpouseNameTa { get; set; }
    public string? Work { get; set; }
    public string? City { get; set; }
    public string? CityTa { get; set; }

    public int? MoiCategoryId { get; set; }            // Thaimaman moi, Seer …
    public MoiCategory? MoiCategory { get; set; }
    public bool IsHighlighted { get; set; }

    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public string? PaymentRef { get; set; }
    public string? Notes { get; set; }

    public MoiEntryStatus Status { get; set; } = MoiEntryStatus.Active;
    public string? ReversalReason { get; set; }
    public DateTime EntryAt { get; set; }
    public string? ClientRef { get; set; }             // idempotency key for offline sync

    public ICollection<MoiEntryDenomination> Denominations { get; set; } = new List<MoiEntryDenomination>();
    public ICollection<MoiEntryGift> Gifts { get; set; } = new List<MoiEntryGift>();
}

/// <summary>500 × 50, 100 × 100 … breakdown of a cash Moi.</summary>
public class MoiEntryDenomination : BaseEntity
{
    public int MoiEntryId { get; set; }
    public int NoteValue { get; set; }
    public int Count { get; set; }
    public decimal Total { get; set; }
}

/// <summary>Gift recorded with the Moi (gold ring 1 sovereign, silver lamp …).</summary>
public class MoiEntryGift : BaseEntity
{
    public int MoiEntryId { get; set; }
    public int? GiftItemTypeId { get; set; }
    public GiftItemType? GiftItemType { get; set; }
    public string Description { get; set; } = "";
    public string? DescriptionTa { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal? EstimatedValue { get; set; }
}

/// <summary>
/// Money handed out from the Moi collection during the function
/// (e.g. host's brother takes ₹5,000 for food). Reduces cash in hand.
/// </summary>
public class FunctionExpense : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int FunctionId { get; set; }
    public Function? Function { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    public string TakenByName { get; set; } = "";
    public string? TakenByNameTa { get; set; }
    public string? Relation { get; set; }              // host's brother, uncle …
    public string? TakenByMobile { get; set; }
    public string Purpose { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public DateTime EntryAt { get; set; }
    public int? OperatorId { get; set; }
    public string? Notes { get; set; }
}
