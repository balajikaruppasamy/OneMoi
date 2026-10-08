using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Individuals;

public record MyMoiItemDto(
    int EntryId, DateTime EntryAt, DateTime FunctionDate, string FunctionName, string? FunctionNameTa, string FunctionType, string? FunctionTypeTa,
    string Location, string? OwnerName, string VendorName, string? VendorNameTa, string? VendorLogo,
    string? NameAsWritten, string? NameTaAsWritten, decimal Amount, string PaymentMode, string? Category, string? CategoryTa, bool IsHighlighted,
    string ReceiptNo, List<string> Gifts);

public record MyMoiResponse(decimal Total, int Count, int VendorCount, List<int> Years, List<VendorTotalDto> ByVendor, List<MyMoiItemDto> Items);

public record VendorTotalDto(string Vendor, int Count, decimal Amount);

public record MyProfileDto(int UserId, string? Mobile, string? Email, string FullName, string? FullNameTa,
    string? Initial, string? Name, string? NameTa, string? SpouseInitial, string? SpouseName, string? SpouseNameTa, string? Work, string? City, string? CityTa);

public record UpdateMyProfileRequest(string? Email, string? Initial, string Name, string? NameTa, string? SpouseInitial, string? SpouseName,
    string? SpouseNameTa, string? Work, string? City, string? CityTa);

public record HostedFunctionDto(int Id, string Code, string Name, string? NameTa, string TypeName, DateTime FunctionDate, string Location,
    string Status, string VendorName, string? VendorLogo, int Entries, decimal Total);

/// <summary>
/// The individual's own view. This is the ONLY place where data is read across vendors,
/// and it is always limited to the caller's own PersonId / own mobile.
/// </summary>
public class MyMoiService(IAppDbContext db, ICurrentUser current)
{
    public async Task<MyMoiResponse> MyMoiAsync(int? year, CancellationToken ct = default)
    {
        var personId = current.PersonId ?? -1;
        var all = db.MoiEntries.IgnoreQueryFilters()
            .Where(m => m.PersonId == personId && m.Status == MoiEntryStatus.Active && !m.IsDeleted);

        var years = await all.Select(m => m.Function!.FunctionDate.Year).Distinct().OrderByDescending(y => y).ToListAsync(ct);
        var q = year == null ? all : all.Where(m => m.Function!.FunctionDate.Year == year);

        var items = await q.OrderByDescending(m => m.Function!.FunctionDate).ThenByDescending(m => m.Id)
            .Select(m => new MyMoiItemDto(m.Id, m.EntryAt, m.Function!.FunctionDate, m.Function.Name, m.Function.NameTa,
                m.Function.FunctionType!.Name, m.Function.FunctionType.NameTa, m.Function.Location, m.Function.OwnerName,
                m.Function.Tenant!.Name, m.Function.Tenant.NameTa, m.Function.Tenant.LogoPath,
                (m.Initial != null ? m.Initial + " " : "") + m.Name, m.NameTa, m.Amount, m.PaymentMode.ToString(),
                m.MoiCategory!.Name, m.MoiCategory.NameTa, m.IsHighlighted, m.ReceiptNo,
                m.Gifts.Select(g => g.Description).ToList()))
            .ToListAsync(ct);

        var byVendor = items.GroupBy(i => i.VendorName).Select(g => new VendorTotalDto(g.Key, g.Count(), g.Sum(x => x.Amount)))
            .OrderByDescending(v => v.Amount).ToList();
        return new MyMoiResponse(items.Sum(i => i.Amount), items.Count, byVendor.Count, years, byVendor, items);
    }

    public async Task<MyProfileDto> ProfileAsync(CancellationToken ct = default)
    {
        var u = await db.Users.Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == current.UserId, ct) ?? throw new ForbiddenException();
        var p = u.Person;
        return new MyProfileDto(u.Id, u.Mobile, u.Email, u.FullName, u.FullNameTa, p?.Initial, p?.Name, p?.NameTa, p?.SpouseInitial,
            p?.SpouseName, p?.SpouseNameTa, p?.Work, p?.City, p?.CityTa);
    }

    public async Task<MyProfileDto> UpdateProfileAsync(UpdateMyProfileRequest r, CancellationToken ct = default)
    {
        new Validator().Require("name", r.Name, "Enter your name")
            .Check(string.IsNullOrWhiteSpace(r.Email) || Helpers.IsEmail(r.Email), "email", "Enter a valid e-mail").ThrowIfInvalid();
        var u = await db.Users.Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == current.UserId, ct) ?? throw new ForbiddenException();
        var email = Helpers.Clean(r.Email)?.ToLower();
        if (email != null && email != u.Email && await db.Users.AnyAsync(x => x.Email == email && x.Id != u.Id, ct))
            throw new ValidationException("email", "This e-mail is already used by another account.");

        var initial = Helpers.NormalizeInitial(r.Initial);
        u.Email = email;
        u.FullName = (initial != null ? initial + " " : "") + r.Name.Trim();
        u.FullNameTa = Helpers.Clean(r.NameTa);
        if (u.Person != null)
        {
            var p = u.Person;
            p.Initial = initial; p.Name = r.Name.Trim(); p.NameTa = Helpers.Clean(r.NameTa);
            p.SpouseInitial = Helpers.NormalizeInitial(r.SpouseInitial); p.SpouseName = Helpers.Clean(r.SpouseName); p.SpouseNameTa = Helpers.Clean(r.SpouseNameTa);
            p.Work = Helpers.Clean(r.Work); p.City = Helpers.Clean(r.City); p.CityTa = Helpers.Clean(r.CityTa);
        }
        await db.SaveChangesAsync(ct);
        return await ProfileAsync(ct);
    }

    /// <summary>Functions where I am the owner/host (matched by my verified mobile).</summary>
    public async Task<List<HostedFunctionDto>> HostedFunctionsAsync(CancellationToken ct = default)
    {
        var mobile = current.Mobile ?? "-";
        return await db.Functions.IgnoreQueryFilters().Where(f => f.OwnerMobile == mobile && !f.IsDeleted)
            .OrderByDescending(f => f.FunctionDate)
            .Select(f => new HostedFunctionDto(f.Id, f.Code, f.Name, f.NameTa, f.FunctionType!.Name, f.FunctionDate, f.Location, f.Status.ToString(),
                f.Tenant!.Name, f.Tenant.LogoPath,
                db.MoiEntries.IgnoreQueryFilters().Count(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active && !m.IsDeleted),
                db.MoiEntries.IgnoreQueryFilters().Where(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active && !m.IsDeleted).Sum(m => (decimal?)m.Amount) ?? 0))
            .ToListAsync(ct);
    }
}
