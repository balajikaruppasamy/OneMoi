using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Moi;

/// <summary>
/// Recording Moi at the counter. Rules:
///  • Operators can enter only for the function/counter they are assigned to, inside the time window.
///  • Owner / manager can enter for any function of their vendor.
///  • Denominations (500 × 50 …) must add up to the cash amount.
///  • Entries are never deleted — only reversed with a reason.
///  • A mobile number links the entry to the person's global OneMoi identity.
/// </summary>
public class MoiEntryService(IAppDbContext db, ICurrentUser current, AppSettings settings)
{
    public async Task<PersonLookupDto> LookupAsync(string mobileInput, string? name, string? city, CancellationToken ct = default)
    {
        current.Demand(Permissions.MoiEntry);
        var mobile = Helpers.NormalizeMobile(mobileInput);
        var hints = await SameNameHintsAsync(name, city, mobile, ct);
        if (mobile == null) return new PersonLookupDto("none", null, null, null, null, null, null, null, null, null, 0, false, hints);

        // 1) How THIS vendor wrote the name last time (privacy: no amounts, no other vendors)
        var last = await db.MoiEntries.Where(m => m.Mobile == mobile).OrderByDescending(m => m.Id).FirstOrDefaultAsync(ct);
        var count = await db.MoiEntries.CountAsync(m => m.Mobile == mobile && m.Status == MoiEntryStatus.Active, ct);
        var person = await db.Persons.FirstOrDefaultAsync(p => p.Mobile == mobile, ct);

        if (last != null)
            return new PersonLookupDto("tenant", last.Initial, last.Name, last.NameTa, last.SpouseInitial, last.SpouseName, last.SpouseNameTa,
                last.Work, last.City, last.CityTa, count, person?.IsVerified ?? false, hints);

        // 2) Global identity: only the name details, never history
        if (person != null)
            return new PersonLookupDto("global", person.Initial, person.Name, person.NameTa, person.SpouseInitial, person.SpouseName,
                person.SpouseNameTa, person.Work, person.City, person.CityTa, 0, person.IsVerified, hints);

        return new PersonLookupDto("none", null, null, null, null, null, null, null, null, null, 0, false, hints);
    }

    public async Task<MoiEntryDto> CreateAsync(CreateMoiEntryRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.MoiEntry);
        // Idempotency: the same device request twice returns the first result (offline re-sync safe)
        if (!string.IsNullOrWhiteSpace(r.ClientRef))
        {
            var dup = await db.MoiEntries.Where(m => m.ClientRef == r.ClientRef).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
            if (dup != null) return await GetAsync(dup.Value, ct);
        }

        var (function, counterId) = await ResolveFunctionForEntryAsync(r.FunctionId, r.CounterId, ct);

        if (!Enum.TryParse<PaymentMode>(r.PaymentMode, true, out var mode)) mode = PaymentMode.Cash;
        var mobile = Helpers.NormalizeMobile(r.Mobile);
        var denoms = (r.Denominations ?? new()).Where(d => d.Count > 0 && d.NoteValue > 0).ToList();
        var gifts = (r.Gifts ?? new()).Where(g => !string.IsNullOrWhiteSpace(g.Description)).ToList();
        var denomTotal = denoms.Sum(d => (decimal)d.NoteValue * d.Count);

        var v = new Validator()
            .Require("name", r.Name, "Enter the name")
            .Check(string.IsNullOrWhiteSpace(r.Mobile) || mobile != null, "mobile", "Enter a valid 10-digit mobile number")
            .Check(r.Amount >= 0 && r.Amount <= 10_000_000, "amount", "Enter a valid amount")
            .Check(mode == PaymentMode.GiftOnly ? gifts.Count > 0 : r.Amount > 0, "amount", mode == PaymentMode.GiftOnly ? "Add at least one gift" : "Enter the Moi amount")
            .Check(denoms.Count == 0 || denomTotal == r.Amount, "denominations", $"Notes total ₹{denomTotal:N0} does not match amount ₹{r.Amount:N0}")
            .Check(mode != PaymentMode.Cash || function.AllowCash, "paymentMode", "Cash is not enabled for this function")
            .Check(mode != PaymentMode.Upi || function.AllowUpi, "paymentMode", "UPI is not enabled for this function");
        MoiCategory? category = null;
        if (r.MoiCategoryId != null)
        {
            category = await db.MoiCategories.FirstOrDefaultAsync(c => c.Id == r.MoiCategoryId, ct);
            if (category == null) v.Add("moiCategoryId", "Unknown Moi category");
        }
        v.ThrowIfInvalid();

        var person = mobile == null ? null : await UpsertPersonAsync(mobile, r, ct);

        var serial = (await db.MoiEntries.IgnoreQueryFilters().Where(m => m.FunctionId == function.Id).MaxAsync(m => (int?)m.SerialNo, ct) ?? 0) + 1;
        var tenantCode = await db.Tenants.Where(t => t.Id == function.TenantId).Select(t => t.Code).FirstAsync(ct);

        var entry = new MoiEntry
        {
            TenantId = function.TenantId,
            FunctionId = function.Id,
            CounterId = counterId,
            OperatorId = current.OperatorId,
            Person = person,
            SerialNo = serial,
            ReceiptNo = $"{tenantCode}-{function.Code}-{serial:0000}",
            Mobile = mobile,
            Initial = Helpers.NormalizeInitial(r.Initial),
            Name = r.Name.Trim(),
            NameTa = Helpers.Clean(r.NameTa),
            SpouseInitial = Helpers.NormalizeInitial(r.SpouseInitial),
            SpouseName = Helpers.Clean(r.SpouseName),
            SpouseNameTa = Helpers.Clean(r.SpouseNameTa),
            Work = Helpers.Clean(r.Work),
            City = Helpers.Clean(r.City),
            CityTa = Helpers.Clean(r.CityTa),
            MoiCategoryId = category?.Id,
            IsHighlighted = category?.IsHighlighted ?? false,
            Amount = r.Amount,
            PaymentMode = mode,
            PaymentRef = Helpers.Clean(r.PaymentRef),
            Notes = Helpers.Clean(r.Notes),
            EntryAt = DateTime.UtcNow,
            ClientRef = Helpers.Clean(r.ClientRef)
        };
        foreach (var d in denoms)
            entry.Denominations.Add(new MoiEntryDenomination { NoteValue = d.NoteValue, Count = d.Count, Total = (decimal)d.NoteValue * d.Count });
        foreach (var g in gifts)
            entry.Gifts.Add(new MoiEntryGift { GiftItemTypeId = g.GiftItemTypeId, Description = g.Description.Trim(), DescriptionTa = Helpers.Clean(g.DescriptionTa), Quantity = g.Quantity <= 0 ? 1 : g.Quantity, EstimatedValue = g.EstimatedValue });

        db.MoiEntries.Add(entry);
        if (function.Status == FunctionStatus.Scheduled) function.Status = FunctionStatus.Live;   // first entry makes it live
        await db.SaveChangesAsync(ct);
        return await GetAsync(entry.Id, ct);
    }

    public async Task<MoiEntryDto> GetAsync(int id, CancellationToken ct = default)
    {
        current.Demand(Permissions.MoiView);
        var e = await Project(db.MoiEntries.Where(m => m.Id == id && (!current.IsOperator || m.OperatorId == current.OperatorId)))
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Moi entry");
        return e;
    }

    public async Task<List<MoiEntryDto>> ListAsync(MoiListQuery q, CancellationToken ct = default)
    {
        current.Demand(Permissions.MoiView);
        await EnsureFunctionVisibleAsync(q.FunctionId, ct);
        var query = db.MoiEntries.Where(m => m.FunctionId == q.FunctionId);
        if (!q.IncludeReversed) query = query.Where(m => m.Status == MoiEntryStatus.Active);
        if (q.CounterId != null) query = query.Where(m => m.CounterId == q.CounterId);
        if (q.MoiCategoryId != null) query = query.Where(m => m.MoiCategoryId == q.MoiCategoryId);
        if (q.HighlightedOnly) query = query.Where(m => m.IsHighlighted);
        if (current.IsOperator) query = query.Where(m => m.OperatorId == current.OperatorId);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            // ToLower() = case-insensitive search ("ram" finds "Ram")
            s = s.ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(s) || (m.NameTa != null && m.NameTa.Contains(s)) || (m.Mobile != null && m.Mobile.Contains(s)) ||
                                     (m.City != null && m.City.ToLower().Contains(s)) || m.ReceiptNo.ToLower().Contains(s) || (m.SpouseName != null && m.SpouseName.ToLower().Contains(s)));
        }
        return await Project(query.OrderByDescending(m => m.SerialNo).Take(Math.Clamp(q.Take, 1, 5000))).ToListAsync(ct);
    }

    public async Task<MoiEntryDto> ReverseAsync(int id, ReverseRequest r, CancellationToken ct = default)
    {
        new Validator().Require("reason", r.Reason, "Enter the reason for correction").ThrowIfInvalid();
        var e = await db.MoiEntries.FirstOrDefaultAsync(m => m.Id == id, ct) ?? throw new NotFoundException("Moi entry");
        if (e.Status == MoiEntryStatus.Reversed) throw new AppException("This entry is already reversed.");

        if (current.Can(Permissions.MoiReverseAny)) { /* owner / manager: any entry */ }
        else if (current.Can(Permissions.MoiReverseOwn))
        {
            var withinWindow = e.OperatorId == current.OperatorId && e.EntryAt > DateTime.UtcNow.AddMinutes(-settings.CorrectionWindowMinutes);
            if (!withinWindow) throw new ForbiddenException($"Operators can correct only their own entries within {settings.CorrectionWindowMinutes} minutes. Ask your manager.");
        }
        else throw new ForbiddenException("You do not have permission to correct Moi entries.");

        e.Status = MoiEntryStatus.Reversed;
        e.ReversalReason = r.Reason.Trim();
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = e.TenantId, PrincipalType = current.PrincipalType, PrincipalId = current.OperatorId ?? current.UserId,
            Action = "MOI_REVERSED", EntityName = nameof(MoiEntry), EntityId = e.Id.ToString(),
            Details = $"{e.ReceiptNo} ₹{e.Amount:N0} — {e.ReversalReason}", IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    // ───────────────────────── Function expenses ─────────────────────────

    public async Task<ExpenseDto> AddExpenseAsync(CreateExpenseRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.ExpensesCreate);
        var (function, _) = await ResolveFunctionForEntryAsync(r.FunctionId, null, ct);
        if (!Enum.TryParse<PaymentMode>(r.PaymentMode, true, out var mode)) mode = PaymentMode.Cash;
        new Validator()
            .Require("takenByName", r.TakenByName, "Enter who took the money")
            .Require("purpose", r.Purpose, "Enter the purpose")
            .Check(r.Amount > 0, "amount", "Enter the amount")
            .Check(string.IsNullOrWhiteSpace(r.TakenByMobile) || Helpers.NormalizeMobile(r.TakenByMobile) != null, "takenByMobile", "Enter a valid mobile number")
            .ThrowIfInvalid();

        var x = new FunctionExpense
        {
            TenantId = function.TenantId, FunctionId = function.Id, ExpenseCategoryId = r.ExpenseCategoryId,
            TakenByName = r.TakenByName.Trim(), TakenByNameTa = Helpers.Clean(r.TakenByNameTa), Relation = Helpers.Clean(r.Relation),
            TakenByMobile = Helpers.NormalizeMobile(r.TakenByMobile), Purpose = r.Purpose.Trim(), Amount = r.Amount, PaymentMode = mode,
            EntryAt = DateTime.UtcNow, OperatorId = current.OperatorId, Notes = Helpers.Clean(r.Notes)
        };
        db.FunctionExpenses.Add(x);
        await db.SaveChangesAsync(ct);
        return (await ExpensesAsync(function.Id, ct)).First(e => e.Id == x.Id);
    }

    public async Task<List<ExpenseDto>> ExpensesAsync(int functionId, CancellationToken ct = default)
    {
        current.Demand(Permissions.ExpensesView);
        await EnsureFunctionVisibleAsync(functionId, ct);
        return await db.FunctionExpenses.Where(e => e.FunctionId == functionId).OrderByDescending(e => e.EntryAt)
            .Select(e => new ExpenseDto(e.Id, e.EntryAt, e.ExpenseCategory!.Name, e.ExpenseCategory.NameTa, e.TakenByName, e.TakenByNameTa,
                e.Relation, e.TakenByMobile, e.Purpose, e.Amount, e.PaymentMode.ToString(), e.Notes,
                e.OperatorId == null ? "Vendor staff" : db.Operators.Where(o => o.Id == e.OperatorId).Select(o => o.Name).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task DeleteExpenseAsync(int id, CancellationToken ct = default)
    {
        current.Demand(Permissions.ExpensesDelete, "Only the owner or manager can remove an expense.");
        var e = await db.FunctionExpenses.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Expense");
        e.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    // ───────────────────────────── Summary ─────────────────────────────

    public async Task<FunctionSummaryDto> SummaryAsync(int functionId, CancellationToken ct = default)
    {
        current.Demand(Permissions.MoiView);
        var f = await EnsureFunctionVisibleAsync(functionId, ct);
        var active = db.MoiEntries.Where(m => m.FunctionId == functionId && m.Status == MoiEntryStatus.Active);

        var totals = await active.GroupBy(m => m.PaymentMode).Select(g => new { Mode = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);
        var reversed = await db.MoiEntries.CountAsync(m => m.FunctionId == functionId && m.Status == MoiEntryStatus.Reversed, ct);
        var giftCount = await db.MoiEntryGifts.CountAsync(g => active.Select(m => m.Id).Contains(g.MoiEntryId), ct);

        var expenses = await db.FunctionExpenses.Where(e => e.FunctionId == functionId)
            .GroupBy(e => e.PaymentMode).Select(g => new { Mode = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync(ct);

        var byCounter = await active.GroupBy(m => m.Counter!.Name)
            .Select(g => new NameAmountDto(g.Key ?? "Vendor desk", null, g.Count(), g.Sum(x => x.Amount))).ToListAsync(ct);
        var byCategory = await active.GroupBy(m => new { m.MoiCategory!.Name, m.MoiCategory.NameTa })
            .Select(g => new NameAmountDto(g.Key.Name ?? "General", g.Key.NameTa, g.Count(), g.Sum(x => x.Amount))).ToListAsync(ct);
        var notes = await db.MoiEntryDenominations.Where(d => active.Select(m => m.Id).Contains(d.MoiEntryId))
            .GroupBy(d => d.NoteValue).Select(g => new DenominationLineDto(g.Key, g.Sum(x => x.Count))).ToListAsync(ct);
        var highlighted = await active.Where(m => m.IsHighlighted).OrderBy(m => m.SerialNo)
            .Select(m => new HighlightRowDto(m.MoiCategory!.Name, m.MoiCategory.NameTa, m.MoiCategory.Color,
                (m.Initial != null ? m.Initial + " " : "") + m.Name, m.NameTa, m.Amount)).ToListAsync(ct);

        decimal Sum(PaymentMode p) => totals.Where(t => t.Mode == p).Sum(t => t.Amount);
        var total = totals.Sum(t => t.Amount);
        var cashExp = expenses.Where(e => e.Mode == PaymentMode.Cash).Sum(e => e.Amount);
        return new FunctionSummaryDto(f.Id, f.Name, f.NameTa, totals.Sum(t => t.Count), reversed, total, Sum(PaymentMode.Cash), Sum(PaymentMode.Upi),
            total - Sum(PaymentMode.Cash) - Sum(PaymentMode.Upi), giftCount, expenses.Sum(e => e.Amount), cashExp, Sum(PaymentMode.Cash) - cashExp,
            byCounter.OrderBy(c => c.Name).ToList(), byCategory.OrderByDescending(c => c.Amount).ToList(),
            notes.OrderByDescending(n => n.NoteValue).ToList(), highlighted);
    }

    // ───────────────────────────── helpers ─────────────────────────────

    /// <summary>Checks the caller may write to this function and returns the counter to use.</summary>
    private async Task<(Function function, int? counterId)> ResolveFunctionForEntryAsync(int functionId, int? counterId, CancellationToken ct)
    {
        var f = await db.Functions.FirstOrDefaultAsync(x => x.Id == functionId, ct) ?? throw new NotFoundException("Function");
        if (f.Status is FunctionStatus.Closed or FunctionStatus.Cancelled) throw new AppException("This function is closed. No more entries can be added.");

        if (current.IsOperator)
        {
            var now = DateTime.Now;
            var a = await db.OperatorAssignments.FirstOrDefaultAsync(x => x.OperatorId == current.OperatorId && x.FunctionId == functionId && x.IsActive, ct)
                    ?? throw new ForbiddenException("You are not assigned to this function.");
            if (now < a.ValidFrom || now > a.ValidTo)
                throw new ForbiddenException($"Your counter is open only between {a.ValidFrom:dd MMM hh:mm tt} and {a.ValidTo:dd MMM hh:mm tt}.");
            return (f, a.CounterId);
        }
        if (current.TenantId == null) throw new ForbiddenException();
        if (counterId != null && !await db.Counters.AnyAsync(c => c.Id == counterId && c.FunctionId == functionId, ct)) counterId = null;
        return (f, counterId);
    }

    private async Task<Function> EnsureFunctionVisibleAsync(int functionId, CancellationToken ct)
    {
        var f = await db.Functions.FirstOrDefaultAsync(x => x.Id == functionId, ct) ?? throw new NotFoundException("Function");
        if (current.IsOperator && !await db.OperatorAssignments.AnyAsync(a => a.OperatorId == current.OperatorId && a.FunctionId == functionId, ct))
            throw new ForbiddenException("You are not assigned to this function.");
        return f;
    }

    private async Task<Person> UpsertPersonAsync(string mobile, CreateMoiEntryRequest r, CancellationToken ct)
    {
        var p = await db.Persons.FirstOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (p == null)
        {
            p = new Person { Mobile = mobile, Name = r.Name.Trim() };
            db.Persons.Add(p);
        }
        if (!p.IsVerified)   // a verified person controls their own profile; vendors only fill blanks
        {
            p.Name = string.IsNullOrWhiteSpace(p.Name) ? r.Name.Trim() : p.Name;
            p.Initial ??= Helpers.NormalizeInitial(r.Initial);
            p.NameTa ??= Helpers.Clean(r.NameTa);
            p.SpouseInitial ??= Helpers.NormalizeInitial(r.SpouseInitial);
            p.SpouseName ??= Helpers.Clean(r.SpouseName);
            p.SpouseNameTa ??= Helpers.Clean(r.SpouseNameTa);
            p.Work ??= Helpers.Clean(r.Work);
            p.City ??= Helpers.Clean(r.City);
            p.CityTa ??= Helpers.Clean(r.CityTa);
        }
        return p;
    }

    private async Task<List<SameNameHintDto>> SameNameHintsAsync(string? name, string? city, string? exceptMobile, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(city)) return new();
        var n = name.Trim().ToLower(); var c = city.Trim().ToLower();
        var rows = await db.MoiEntries
            .Where(m => m.Name.ToLower() == n && m.City != null && m.City.ToLower() == c && (exceptMobile == null || m.Mobile != exceptMobile))
            .Select(m => new { m.Initial, m.Name, m.SpouseName, m.Mobile })
            .Distinct().Take(5).ToListAsync(ct);
        return rows.Select(x => new SameNameHintDto(x.Initial, x.Name, x.SpouseName, x.Mobile == null ? null : Helpers.MaskMobile(x.Mobile))).ToList();
    }

    private IQueryable<MoiEntryDto> Project(IQueryable<MoiEntry> q) => q.Select(m => new MoiEntryDto(
        m.Id, m.SerialNo, m.ReceiptNo, m.EntryAt, m.Mobile, m.Initial, m.Name, m.NameTa, m.SpouseInitial, m.SpouseName, m.SpouseNameTa,
        m.Work, m.City, m.CityTa, m.MoiCategoryId, m.MoiCategory!.Name, m.MoiCategory.NameTa, m.IsHighlighted, m.MoiCategory.Color,
        m.Amount, m.PaymentMode.ToString(), m.PaymentRef, m.Notes, m.Status.ToString(), m.ReversalReason,
        m.Counter!.Name, m.Operator!.Name,
        m.Denominations.OrderByDescending(d => d.NoteValue).Select(d => new DenominationLineDto(d.NoteValue, d.Count)).ToList(),
        m.Gifts.Select(g => new GiftLineDto(g.GiftItemTypeId, g.Description, g.DescriptionTa, g.Quantity, g.EstimatedValue)).ToList()));
}
