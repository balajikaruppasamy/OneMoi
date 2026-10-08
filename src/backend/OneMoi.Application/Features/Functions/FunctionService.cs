using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Functions;

public record FunctionListItemDto(
    int Id, string Code, string Name, string? NameTa, string TypeName, string? TypeNameTa, string OwnerName, string? OwnerNameTa,
    string OwnerMobile, string Location, DateTime FunctionDate, string? StartTime, string? EndTime, string Status,
    int CounterCount, int EntryCount, decimal Collected);

public record CounterDto(int Id, int Number, string Name, int? OperatorId, string? OperatorName, string? OperatorCode);

public record FunctionDetailDto(
    int Id, string Code, int FunctionTypeId, string TypeName, string? TypeNameTa,
    string Name, string? NameTa, string OwnerName, string? OwnerNameTa, string OwnerMobile, string? OwnerEmail,
    string Location, string? LocationTa, string? Address, string? City,
    DateTime FunctionDate, string? StartTime, string? EndTime, int? ExpectedGuests, string? OtherDetails,
    bool AllowCash, bool AllowUpi, string Status, List<CounterDto> Counters,
    string TenantName, string? TenantNameTa, string? TenantLogo);

public record SaveFunctionRequest(
    int FunctionTypeId, string Name, string? NameTa, string OwnerName, string? OwnerNameTa, string OwnerMobile, string? OwnerEmail,
    string Location, string? LocationTa, string? Address, string? City, DateTime FunctionDate, string? StartTime, string? EndTime,
    int? ExpectedGuests, string? OtherDetails, bool AllowCash, bool AllowUpi, int CounterCount);

public record FunctionQuery(string? Status, string? Search, DateTime? From, DateTime? To, int Take = 200);

/// <summary>Create and manage functions (events) for the current vendor.</summary>
public class FunctionService(IAppDbContext db, ICurrentUser current)
{
    public async Task<List<FunctionListItemDto>> ListAsync(FunctionQuery q, CancellationToken ct = default)
    {
        current.Demand(Permissions.FunctionsView);
        var query = db.Functions.AsQueryable();
        // Least privilege: an operator sees only the functions they are assigned to
        if (current.IsOperator)
            query = query.Where(f => db.OperatorAssignments.Any(a => a.FunctionId == f.Id && a.OperatorId == current.OperatorId));
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<FunctionStatus>(q.Status, true, out var st)) query = query.Where(f => f.Status == st);
        if (q.From != null) query = query.Where(f => f.FunctionDate >= q.From.Value.Date);
        if (q.To != null) query = query.Where(f => f.FunctionDate <= q.To.Value.Date);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();   // case-insensitive
            query = query.Where(f => f.Name.ToLower().Contains(s) || (f.NameTa != null && f.NameTa.Contains(s)) || f.OwnerName.ToLower().Contains(s) ||
                                     f.Code.ToLower().Contains(s) || f.Location.ToLower().Contains(s) || f.OwnerMobile.Contains(s));
        }

        return await query
            .OrderByDescending(f => f.FunctionDate).ThenBy(f => f.Name)
            .Take(Math.Clamp(q.Take, 1, 500))
            .Select(f => new FunctionListItemDto(
                f.Id, f.Code, f.Name, f.NameTa, f.FunctionType!.Name, f.FunctionType.NameTa, f.OwnerName, f.OwnerNameTa,
                f.OwnerMobile, f.Location, f.FunctionDate,
                f.StartTime == null ? null : f.StartTime.Value.ToString(), f.EndTime == null ? null : f.EndTime.Value.ToString(),
                f.Status.ToString(),
                db.Counters.Count(c => c.FunctionId == f.Id),
                db.MoiEntries.Count(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active),
                db.MoiEntries.Where(m => m.FunctionId == f.Id && m.Status == MoiEntryStatus.Active).Sum(m => (decimal?)m.Amount) ?? 0))
            .ToListAsync(ct);
    }

    public async Task<FunctionDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        current.Demand(Permissions.FunctionsView);
        // Operators may open only the functions they are assigned to
        if (current.IsOperator && !await db.OperatorAssignments.AnyAsync(a => a.OperatorId == current.OperatorId && a.FunctionId == id, ct))
            throw new ForbiddenException("You are not assigned to this function.");
        var f = await db.Functions.Include(x => x.FunctionType).Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Function");
        return await ToDetailAsync(f, ct);
    }

    public async Task<FunctionDetailDto> CreateAsync(SaveFunctionRequest r, CancellationToken ct = default)
    {
        EnsureManage();
        await ValidateAsync(r, ct);
        var f = new Function { TenantId = current.TenantId!.Value, Code = await UniqueCodeAsync(ct) };
        Apply(f, r);
        f.Status = FunctionStatus.Scheduled;
        for (var i = 1; i <= r.CounterCount; i++) f.Counters.Add(new Counter { TenantId = f.TenantId, Number = i, Name = $"Counter {i}" });
        db.Functions.Add(f);
        await db.SaveChangesAsync(ct);
        return await GetAsync(f.Id, ct);
    }

    public async Task<FunctionDetailDto> UpdateAsync(int id, SaveFunctionRequest r, CancellationToken ct = default)
    {
        EnsureManage();
        await ValidateAsync(r, ct);
        var f = await db.Functions.Include(x => x.Counters).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Function");
        if (f.Status is FunctionStatus.Closed or FunctionStatus.Cancelled) throw new AppException("A closed function cannot be edited.");
        Apply(f, r);

        // Grow / shrink counters (only empty counters can be removed)
        var counters = f.Counters.OrderBy(c => c.Number).ToList();
        for (var n = counters.Count + 1; n <= r.CounterCount; n++)
            f.Counters.Add(new Counter { TenantId = f.TenantId, Number = n, Name = $"Counter {n}" });
        foreach (var extra in counters.Where(c => c.Number > r.CounterCount))
        {
            var used = await db.MoiEntries.AnyAsync(m => m.CounterId == extra.Id, ct) || await db.OperatorAssignments.AnyAsync(a => a.CounterId == extra.Id && a.IsActive, ct);
            if (used) throw new ValidationException("counterCount", $"{extra.Name} already has entries or an operator, so it cannot be removed.");
            extra.IsDeleted = true;
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<FunctionDetailDto> SetStatusAsync(int id, string status, CancellationToken ct = default)
    {
        EnsureManage();
        if (!Enum.TryParse<FunctionStatus>(status, true, out var st)) throw new ValidationException("status", "Unknown status");
        var f = await db.Functions.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Function");
        f.Status = st;
        if (st is FunctionStatus.Closed or FunctionStatus.Cancelled)
        {
            // Closing a function ends all operator access to it
            var assigns = await db.OperatorAssignments.Where(a => a.FunctionId == id && a.IsActive).ToListAsync(ct);
            assigns.ForEach(a => a.IsActive = false);
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        EnsureManage();
        var f = await db.Functions.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Function");
        if (await db.MoiEntries.AnyAsync(m => m.FunctionId == id, ct))
            throw new AppException("This function has Moi entries. Cancel it instead of deleting.");
        f.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    internal async Task<FunctionDetailDto> ToDetailAsync(Function f, CancellationToken ct)
    {
        var counters = await db.Counters.Where(c => c.FunctionId == f.Id).OrderBy(c => c.Number)
            .Select(c => new CounterDto(c.Id, c.Number, c.Name,
                db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => (int?)a.OperatorId).FirstOrDefault(),
                db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => a.Operator!.Name).FirstOrDefault(),
                db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => a.Operator!.Code).FirstOrDefault()))
            .ToListAsync(ct);
        var tenant = f.Tenant ?? await db.Tenants.FirstAsync(t => t.Id == f.TenantId, ct);
        var type = f.FunctionType ?? await db.FunctionTypes.IgnoreQueryFilters().FirstAsync(t => t.Id == f.FunctionTypeId, ct);
        return new FunctionDetailDto(f.Id, f.Code, f.FunctionTypeId, type.Name, type.NameTa, f.Name, f.NameTa, f.OwnerName, f.OwnerNameTa,
            f.OwnerMobile, f.OwnerEmail, f.Location, f.LocationTa, f.Address, f.City, f.FunctionDate, Hm(f.StartTime), Hm(f.EndTime),
            f.ExpectedGuests, f.OtherDetails, f.AllowCash, f.AllowUpi, f.Status.ToString(), counters, tenant.Name, tenant.NameTa, tenant.LogoPath);
    }

    private void EnsureManage()
    {
        current.Demand(Permissions.FunctionsManage, "Only the vendor owner or manager can change functions.");
    }

    private async Task ValidateAsync(SaveFunctionRequest r, CancellationToken ct)
    {
        var v = new Validator()
            .Require("name", r.Name, "Enter the function name")
            .Require("ownerName", r.OwnerName, "Enter the function owner's name")
            .Check(Helpers.NormalizeMobile(r.OwnerMobile) != null, "ownerMobile", "Enter a valid 10-digit mobile number")
            .Require("location", r.Location, "Enter the location / mandapam")
            .Check(string.IsNullOrWhiteSpace(r.OwnerEmail) || Helpers.IsEmail(r.OwnerEmail), "ownerEmail", "Enter a valid e-mail")
            .Check(r.CounterCount is >= 1 and <= 20, "counterCount", "Counters must be between 1 and 20")
            .Check(r.FunctionDate.Year >= 2000, "functionDate", "Choose the function date")
            .Check(ParseTime(r.StartTime, out _) && ParseTime(r.EndTime, out _), "startTime", "Use HH:mm time format");
        if (!await db.FunctionTypes.AnyAsync(t => t.Id == r.FunctionTypeId, ct)) v.Add("functionTypeId", "Choose a function type");
        v.ThrowIfInvalid();
    }

    private static void Apply(Function f, SaveFunctionRequest r)
    {
        f.FunctionTypeId = r.FunctionTypeId;
        f.Name = r.Name.Trim(); f.NameTa = Helpers.Clean(r.NameTa);
        f.OwnerName = r.OwnerName.Trim(); f.OwnerNameTa = Helpers.Clean(r.OwnerNameTa);
        f.OwnerMobile = Helpers.NormalizeMobile(r.OwnerMobile)!; f.OwnerEmail = Helpers.Clean(r.OwnerEmail)?.ToLower();
        f.Location = r.Location.Trim(); f.LocationTa = Helpers.Clean(r.LocationTa);
        f.Address = Helpers.Clean(r.Address); f.City = Helpers.Clean(r.City);
        f.FunctionDate = r.FunctionDate.Date;
        ParseTime(r.StartTime, out var s); ParseTime(r.EndTime, out var e);
        f.StartTime = s; f.EndTime = e;
        f.ExpectedGuests = r.ExpectedGuests; f.OtherDetails = Helpers.Clean(r.OtherDetails);
        f.AllowCash = r.AllowCash; f.AllowUpi = r.AllowUpi;
    }

    private async Task<string> UniqueCodeAsync(CancellationToken ct)
    {
        string code;
        do code = Helpers.RandomCode(6);
        while (await db.Functions.IgnoreQueryFilters().AnyAsync(f => f.Code == code, ct));
        return code;
    }

    private static bool ParseTime(string? s, out TimeSpan? t)
    {
        t = null;
        if (string.IsNullOrWhiteSpace(s)) return true;
        if (TimeSpan.TryParse(s, out var v)) { t = v; return true; }
        return false;
    }

    private static string? Hm(TimeSpan? t) => t?.ToString(@"hh\:mm");
}
