using OneMoi.Domain.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Domain.Entities;

/// <summary>An event (marriage, keda vettu …) whose Moi is managed by a vendor.</summary>
public class Function : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Code { get; set; } = "";             // 6-char public code for QR / guest pay
    public int FunctionTypeId { get; set; }
    public FunctionType? FunctionType { get; set; }

    public string Name { get; set; } = "";             // "Ram illa villa"
    public string? NameTa { get; set; }                // "ராம் இல்லா வில்லா"
    public string OwnerName { get; set; } = "";
    public string? OwnerNameTa { get; set; }
    public string OwnerMobile { get; set; } = "";      // host logs in with this mobile
    public string? OwnerEmail { get; set; }

    public string Location { get; set; } = "";         // venue / mandapam
    public string? LocationTa { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }

    public DateTime FunctionDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? ExpectedGuests { get; set; }
    public string? OtherDetails { get; set; }

    public bool AllowCash { get; set; } = true;
    public bool AllowUpi { get; set; } = true;
    public FunctionStatus Status { get; set; } = FunctionStatus.Scheduled;

    public ICollection<Counter> Counters { get; set; } = new List<Counter>();
    public ICollection<OperatorAssignment> Assignments { get; set; } = new List<OperatorAssignment>();
}

/// <summary>A Moi table at the function (Counter 1, Counter 2 …).</summary>
public class Counter : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int FunctionId { get; set; }
    public Function? Function { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Operator X works at Function Y, Counter Z between these times.</summary>
public class OperatorAssignment : AuditableEntity, ITenantEntity
{
    public int TenantId { get; set; }
    public int FunctionId { get; set; }
    public Function? Function { get; set; }
    public int CounterId { get; set; }
    public Counter? Counter { get; set; }
    public int OperatorId { get; set; }
    public Operator? Operator { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
