using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Entities;

namespace OneMoi.Application.Features.Masters;

public record MasterItemDto(
    int Id, string Name, string? NameTa, int SortOrder, bool IsActive, bool IsSystem,
    bool IsHighlighted = false, string? Color = null, string? Unit = null);

public record SaveMasterRequest(
    string Name, string? NameTa, int SortOrder, bool IsActive = true,
    bool IsHighlighted = false, string? Color = null, string? Unit = null);

public record DenominationDto(int Id, int Value, bool IsCoin, int SortOrder);

/// <summary>
/// Lookup tables. Rows with TenantId = null are system defaults (managed by super admin);
/// each vendor can add its own extra rows.
/// kinds: function-types | moi-categories | gift-item-types | expense-categories
/// </summary>
public class MasterService(IAppDbContext db, ICurrentUser current)
{
    public static readonly string[] Kinds = { "function-types", "moi-categories", "gift-item-types", "expense-categories" };

    public async Task<List<MasterItemDto>> ListAsync(string kind, bool activeOnly, CancellationToken ct = default)
    {
        current.Demand(Permissions.MastersView);
        var q = Query(kind);
        if (activeOnly) q = q.Where(x => x.IsActive);
        var rows = await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<MasterItemDto> CreateAsync(string kind, SaveMasterRequest req, CancellationToken ct = default)
    {
        EnsureCanEdit(null);
        Validate(req);
        MasterEntity e = kind switch
        {
            "function-types" => new FunctionType(),
            "moi-categories" => new MoiCategory(),
            "gift-item-types" => new GiftItemType(),
            "expense-categories" => new ExpenseCategory(),
            _ => throw new NotFoundException("Master type")
        };
        e.TenantId = current.Can(Permissions.PlatformMastersManage) ? null : current.TenantId;   // super admin creates system defaults
        Apply(e, req);
        switch (e)
        {
            case FunctionType f: db.FunctionTypes.Add(f); break;
            case MoiCategory m: db.MoiCategories.Add(m); break;
            case GiftItemType g: db.GiftItemTypes.Add(g); break;
            case ExpenseCategory x: db.ExpenseCategories.Add(x); break;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<MasterItemDto> UpdateAsync(string kind, int id, SaveMasterRequest req, CancellationToken ct = default)
    {
        var e = await Query(kind).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Item");
        EnsureCanEdit(e);
        Validate(req);
        Apply(e, req);
        await db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task DeleteAsync(string kind, int id, CancellationToken ct = default)
    {
        var e = await Query(kind).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Item");
        EnsureCanEdit(e);
        e.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public Task<List<DenominationDto>> DenominationsAsync(CancellationToken ct = default) =>
        db.Denominations.Where(d => d.IsActive).OrderBy(d => d.SortOrder)
            .Select(d => new DenominationDto(d.Id, d.Value, d.IsCoin, d.SortOrder)).ToListAsync(ct);

    // Query filters already limit rows to (system + my tenant)
    private IQueryable<MasterEntity> Query(string kind) => kind switch
    {
        "function-types" => db.FunctionTypes,
        "moi-categories" => db.MoiCategories,
        "gift-item-types" => db.GiftItemTypes,
        "expense-categories" => db.ExpenseCategories,
        _ => throw new NotFoundException("Master type")
    };

    private void EnsureCanEdit(MasterEntity? e)
    {
        if (current.Can(Permissions.PlatformMastersManage)) return;          // super admin edits system rows
        current.Demand(Permissions.MastersManage, "Only the vendor owner or manager can change masters.");
        if (e != null && e.TenantId == null) throw new ForbiddenException("System default items cannot be changed. Add your own item instead.");
    }

    private static void Validate(SaveMasterRequest r) =>
        new Validator().Require("name", r.Name, "Enter a name").ThrowIfInvalid();

    private static void Apply(MasterEntity e, SaveMasterRequest r)
    {
        e.Name = r.Name.Trim();
        e.NameTa = Helpers.Clean(r.NameTa);
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
        if (e is MoiCategory m) { m.IsHighlighted = r.IsHighlighted; m.Color = Helpers.Clean(r.Color); }
        if (e is GiftItemType g) g.Unit = Helpers.Clean(r.Unit);
    }

    private static MasterItemDto ToDto(MasterEntity e) => new(
        e.Id, e.Name, e.NameTa, e.SortOrder, e.IsActive, e.TenantId == null,
        (e as MoiCategory)?.IsHighlighted ?? false, (e as MoiCategory)?.Color, (e as GiftItemType)?.Unit);
}
