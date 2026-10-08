using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Reports;

public record ReportHeaderDto(
    string VendorName, string? VendorNameTa, string? VendorLogo, string? VendorAddress, string? VendorMobile, string? ReceiptHeader, string? ReceiptFooter,
    string FunctionName, string? FunctionNameTa, string FunctionType, string? FunctionTypeTa, string OwnerName, string? OwnerNameTa,
    string Location, string? LocationTa, DateTime FunctionDate, string Code);

public record ReportRowDto(
    int SerialNo, string ReceiptNo, DateTime EntryAt, string? Initial, string Name, string? NameTa, string? SpouseInitial, string? SpouseName,
    string? SpouseNameTa, string? Work, string? City, string? CityTa, string? Mobile, string? Category, string? CategoryTa, bool IsHighlighted,
    decimal Amount, string PaymentMode, string? Counter, string Gifts);

public record FunctionReportDto(ReportHeaderDto Header, List<ReportRowDto> Rows, decimal Total, decimal Cash, decimal Upi, decimal Expenses, decimal NetCash, int Count);

/// <summary>
/// Function Moi book (guest-wise), available in English or Tamil. Visible to the vendor's staff and
/// to the function owner (host) logged in with the owner mobile.
/// </summary>
public class ReportService(IAppDbContext db, ICurrentUser current)
{
    public async Task<FunctionReportDto> FunctionReportAsync(int functionId, bool maskMobiles, CancellationToken ct = default)
    {
        var f = await db.Functions.IgnoreQueryFilters().Include(x => x.Tenant).Include(x => x.FunctionType)
            .FirstOrDefaultAsync(x => x.Id == functionId && !x.IsDeleted, ct) ?? throw new NotFoundException("Function");

        // Vendor staff of THIS vendor (with reports permission), the function owner (host), or OneMoi admin
        var isVendor = current.TenantId == f.TenantId && !current.IsOperator && current.Can(Permissions.ReportsView);
        var isHost = current.UserType == UserType.Individual && current.Mobile != null && current.Mobile == f.OwnerMobile;
        if (!isVendor && !isHost && !current.IsSuperAdmin) throw new ForbiddenException("You do not have access to this function report.");

        var entries = db.MoiEntries.IgnoreQueryFilters().Where(m => m.FunctionId == functionId && !m.IsDeleted && m.Status == MoiEntryStatus.Active);
        // Gifts are joined in memory (STRING_AGG is not available on older SQL Server versions)
        var raw = await entries.OrderBy(m => m.SerialNo)
            .Select(m => new { m.SerialNo, m.ReceiptNo, m.EntryAt, m.Initial, m.Name, m.NameTa, m.SpouseInitial, m.SpouseName, m.SpouseNameTa,
                m.Work, m.City, m.CityTa, m.Mobile, Cat = m.MoiCategory!.Name, CatTa = m.MoiCategory.NameTa, m.IsHighlighted, m.Amount, m.PaymentMode,
                Counter = m.Counter!.Name, Gifts = m.Gifts.Select(g => g.Description).ToList() })
            .ToListAsync(ct);
        var rows = raw.Select(m => new ReportRowDto(m.SerialNo, m.ReceiptNo, m.EntryAt, m.Initial, m.Name, m.NameTa, m.SpouseInitial, m.SpouseName,
            m.SpouseNameTa, m.Work, m.City, m.CityTa, m.Mobile, m.Cat, m.CatTa, m.IsHighlighted, m.Amount, m.PaymentMode.ToString(),
            m.Counter, string.Join(", ", m.Gifts))).ToList();
        if (maskMobiles) rows = rows.Select(r => r with { Mobile = r.Mobile == null ? null : Helpers.MaskMobile(r.Mobile) }).ToList();

        var expenses = await db.FunctionExpenses.IgnoreQueryFilters().Where(e => e.FunctionId == functionId && !e.IsDeleted && e.PaymentMode == PaymentMode.Cash)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var t = f.Tenant!;
        var header = new ReportHeaderDto(t.Name, t.NameTa, t.LogoPath, string.Join(", ", new[] { t.AddressLine, t.City }.Where(s => !string.IsNullOrEmpty(s))),
            t.Mobile, t.ReceiptHeader, t.ReceiptFooter, f.Name, f.NameTa, f.FunctionType!.Name, f.FunctionType.NameTa, f.OwnerName, f.OwnerNameTa,
            f.Location, f.LocationTa, f.FunctionDate, f.Code);
        var cash = rows.Where(r => r.PaymentMode == "Cash").Sum(r => r.Amount);
        return new FunctionReportDto(header, rows, rows.Sum(r => r.Amount), cash, rows.Where(r => r.PaymentMode == "Upi").Sum(r => r.Amount),
            expenses, cash - expenses, rows.Count);
    }

    /// <summary>CSV (opens in Excel, UTF-8 with BOM so Tamil shows correctly). lang = "en" | "ta"</summary>
    public async Task<(byte[] content, string fileName)> FunctionCsvAsync(int functionId, string lang, CancellationToken ct = default)
    {
        if (current.UserType != UserType.Individual) current.Demand(Permissions.ReportsExport, "You do not have permission to export reports.");
        var r = await FunctionReportAsync(functionId, maskMobiles: true, ct);
        var ta = lang == "ta";
        string Pick(string? en, string? tamil) => ta ? (string.IsNullOrWhiteSpace(tamil) ? en ?? "" : tamil) : en ?? "";
        string Name(string? i, string? n) => string.IsNullOrWhiteSpace(i) ? n ?? "" : $"{i} {n}";

        var sb = new StringBuilder();
        void Line(params object?[] cells) => sb.AppendLine(string.Join(",", cells.Select(c => Csv(Convert.ToString(c, CultureInfo.InvariantCulture)))));

        var h = r.Header;
        Line(Pick(h.VendorName, h.VendorNameTa));
        Line(ta ? "விழா" : "Function", Pick(h.FunctionName, h.FunctionNameTa), ta ? "வகை" : "Type", Pick(h.FunctionType, h.FunctionTypeTa));
        Line(ta ? "விழா நடத்துபவர்" : "Owner", Pick(h.OwnerName, h.OwnerNameTa), ta ? "இடம்" : "Venue", Pick(h.Location, h.LocationTa));
        Line(ta ? "தேதி" : "Date", h.FunctionDate.ToString("dd-MM-yyyy"), ta ? "குறியீடு" : "Code", h.Code);
        sb.AppendLine();
        Line(ta ? "வ.எண்" : "S.No", ta ? "ரசீது எண்" : "Receipt", ta ? "பெயர்" : "Name", ta ? "துணைவர்" : "Spouse", ta ? "தொழில்" : "Work",
             ta ? "ஊர்" : "City", ta ? "கைபேசி" : "Mobile", ta ? "வகை" : "Category", ta ? "பரிசு" : "Gifts", ta ? "முறை" : "Mode", ta ? "தொகை" : "Amount");
        foreach (var x in r.Rows)
            Line(x.SerialNo, x.ReceiptNo, Name(x.Initial, Pick(x.Name, x.NameTa)), Name(x.SpouseInitial, Pick(x.SpouseName, x.SpouseNameTa)), x.Work,
                 Pick(x.City, x.CityTa), x.Mobile, Pick(x.Category, x.CategoryTa), x.Gifts, x.PaymentMode, x.Amount);
        sb.AppendLine();
        Line("", "", "", "", "", "", "", "", "", ta ? "மொத்தம்" : "Total", r.Total);
        Line("", "", "", "", "", "", "", "", "", ta ? "செலவு" : "Expenses", r.Expenses);
        Line("", "", "", "", "", "", "", "", "", ta ? "கையிருப்பு" : "Net cash", r.NetCash);

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, $"Moi-{h.Code}-{(ta ? "Tamil" : "English")}.csv");
    }

    private static string Csv(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}
