using OneMoi.Domain.Common;

namespace OneMoi.Domain.Entities;

/// <summary>
/// Base for lookup tables. TenantId = null means a system default visible to all
/// vendors; a vendor can add its own rows (TenantId = its id).
/// </summary>
public abstract class MasterEntity : AuditableEntity
{
    public int? TenantId { get; set; }
    public string Name { get; set; } = "";
    public string? NameTa { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Marriage, Keda Vettu, Housewarming, Ear piercing …</summary>
public class FunctionType : MasterEntity { }

/// <summary>
/// How the Moi is given: regular, Thaimaman moi, Seer, relative …
/// Highlighted categories are shown in a colour on the entry screen and reports.
/// </summary>
public class MoiCategory : MasterEntity
{
    public bool IsHighlighted { get; set; }
    public string? Color { get; set; }                 // hex, e.g. #F2A516
}

/// <summary>Gold, silver, vessels, dress … recorded alongside (or instead of) cash.</summary>
public class GiftItemType : MasterEntity
{
    public string? Unit { get; set; }                  // grams, sovereign, pieces
}

/// <summary>Food, transport, decoration … money given out during the function.</summary>
public class ExpenseCategory : MasterEntity { }

/// <summary>Currency notes / coins used for the 500 × 50 style counting.</summary>
public class Denomination : BaseEntity
{
    public int Value { get; set; }
    public bool IsCoin { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
