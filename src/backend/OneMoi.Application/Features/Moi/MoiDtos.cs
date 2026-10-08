namespace OneMoi.Application.Features.Moi;

public record DenominationLineDto(int NoteValue, int Count);

public record GiftLineDto(int? GiftItemTypeId, string Description, string? DescriptionTa, decimal Quantity, decimal? EstimatedValue);

public record CreateMoiEntryRequest(
    int FunctionId,
    int? CounterId,
    string? Mobile,
    string? Initial,
    string Name,
    string? NameTa,
    string? SpouseInitial,
    string? SpouseName,
    string? SpouseNameTa,
    string? Work,
    string? City,
    string? CityTa,
    int? MoiCategoryId,
    decimal Amount,
    string PaymentMode,               // Cash | Upi | Card | Cheque | GiftOnly
    string? PaymentRef,
    string? Notes,
    List<DenominationLineDto>? Denominations,
    List<GiftLineDto>? Gifts,
    string? ClientRef);               // idempotency key from the device

public record MoiEntryDto(
    int Id, int SerialNo, string ReceiptNo, DateTime EntryAt,
    string? Mobile, string? Initial, string Name, string? NameTa,
    string? SpouseInitial, string? SpouseName, string? SpouseNameTa,
    string? Work, string? City, string? CityTa,
    int? MoiCategoryId, string? CategoryName, string? CategoryNameTa, bool IsHighlighted, string? HighlightColor,
    decimal Amount, string PaymentMode, string? PaymentRef, string? Notes, string Status, string? ReversalReason,
    string? CounterName, string? OperatorName,
    List<DenominationLineDto> Denominations, List<GiftLineDto> Gifts);

public record MoiListQuery(int FunctionId, string? Search, int? CounterId, int? MoiCategoryId, bool HighlightedOnly = false, bool IncludeReversed = true, int Take = 300);

/// <summary>What we know about a mobile number when the operator types it.</summary>
public record PersonLookupDto(
    string Source,                    // "tenant" = seen at this vendor before, "global" = OneMoi identity, "none"
    string? Initial, string? Name, string? NameTa, string? SpouseInitial, string? SpouseName, string? SpouseNameTa,
    string? Work, string? City, string? CityTa, int PreviousEntriesAtThisVendor, bool IsVerified,
    List<SameNameHintDto> SameNameInCity);

/// <summary>Other people with the same name in the same city — so the operator checks the initial.</summary>
public record SameNameHintDto(string? Initial, string Name, string? SpouseName, string? MobileMasked);

public record ReverseRequest(string Reason);

public record CreateExpenseRequest(
    int FunctionId, int? ExpenseCategoryId, string TakenByName, string? TakenByNameTa, string? Relation, string? TakenByMobile,
    string Purpose, decimal Amount, string PaymentMode, string? Notes);

public record ExpenseDto(
    int Id, DateTime EntryAt, string? CategoryName, string? CategoryNameTa, string TakenByName, string? TakenByNameTa,
    string? Relation, string? TakenByMobile, string Purpose, decimal Amount, string PaymentMode, string? Notes, string? RecordedBy);

public record FunctionSummaryDto(
    int FunctionId, string FunctionName, string? FunctionNameTa,
    int Entries, int ReversedEntries, decimal TotalCollected, decimal Cash, decimal Upi, decimal OtherModes,
    int GiftCount, decimal TotalExpenses, decimal CashExpenses, decimal CashInHand,
    List<NameAmountDto> ByCounter, List<NameAmountDto> ByCategory, List<DenominationLineDto> CashDenominations,
    List<HighlightRowDto> Highlighted);

public record NameAmountDto(string Name, string? NameTa, int Count, decimal Amount);

public record HighlightRowDto(string Category, string? CategoryTa, string? Color, string Name, string? NameTa, decimal Amount);
