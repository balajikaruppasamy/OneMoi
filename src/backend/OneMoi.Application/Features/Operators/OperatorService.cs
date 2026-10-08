using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Operators;

public record OperatorDto(int Id, string Code, string Name, string? NameTa, string Mobile, bool IsActive, DateTime? LastLoginAt,
    int TotalEntries, string? TodayAssignment);

public record SaveOperatorRequest(string Name, string? NameTa, string Mobile, bool IsActive = true);

/// <summary>Returned once when an operator is created or the PIN is reset. The PIN is never stored in plain text.</summary>
public record OperatorCredentialsDto(int Id, string TenantCode, string OperatorCode, string Pin);

public record AssignmentBoardDto(DateTime Date, List<BoardFunctionDto> Functions, List<BoardOperatorDto> Operators);
public record BoardFunctionDto(int Id, string Name, string? NameTa, string TypeName, string Location, string? StartTime, string? EndTime, string Status, List<BoardCounterDto> Counters);
public record BoardCounterDto(int Id, int Number, string Name, int? AssignmentId, int? OperatorId, string? OperatorName, string? OperatorCode);
public record BoardOperatorDto(int Id, string Code, string Name, string Mobile, int? AssignedFunctionId);

public record AssignRequest(int FunctionId, int CounterId, int OperatorId);

public record MyAssignmentDto(int AssignmentId, int FunctionId, string FunctionName, string? FunctionNameTa, string Location,
    DateTime FunctionDate, int CounterId, string CounterName, DateTime ValidFrom, DateTime ValidTo, bool IsOpenNow, string Status);

/// <summary>Counter staff and their function/counter assignments.</summary>
public class OperatorService(IAppDbContext db, ICurrentUser current, IPasswordHasher hasher, ISessionStateStore sessions)
{
    private int TenantId => current.TenantId ?? throw new ForbiddenException();

    public async Task<List<OperatorDto>> ListAsync(CancellationToken ct = default)
    {
        current.Demand(Permissions.OperatorsView);
        var today = DateTime.Today;
        return await db.Operators.OrderBy(o => o.Code)
            .Select(o => new OperatorDto(o.Id, o.Code, o.Name, o.NameTa, o.Mobile, o.IsActive, o.LastLoginAt,
                db.MoiEntries.Count(m => m.OperatorId == o.Id),
                db.OperatorAssignments.Where(a => a.OperatorId == o.Id && a.IsActive && a.Function!.FunctionDate == today)
                    .Select(a => a.Function!.Name + " · " + a.Counter!.Name).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<OperatorCredentialsDto> CreateAsync(SaveOperatorRequest r, CancellationToken ct = default)
    {
        EnsureManage();
        var mobile = Validate(r);
        var count = await db.Operators.IgnoreQueryFilters().CountAsync(o => o.TenantId == TenantId, ct);
        string code;
        var n = 101 + count;
        do code = $"OP-{n++}";
        while (await db.Operators.IgnoreQueryFilters().AnyAsync(o => o.TenantId == TenantId && o.Code == code, ct));

        var pin = Helpers.RandomDigits(4);
        var op = new Operator { TenantId = TenantId, Code = code, Name = r.Name.Trim(), NameTa = Helpers.Clean(r.NameTa), Mobile = mobile, PinHash = hasher.Hash(pin), IsActive = r.IsActive };
        db.Operators.Add(op);
        await db.SaveChangesAsync(ct);
        var tenantCode = await db.Tenants.Where(t => t.Id == TenantId).Select(t => t.Code).FirstAsync(ct);
        return new OperatorCredentialsDto(op.Id, tenantCode, code, pin);
    }

    public async Task UpdateAsync(int id, SaveOperatorRequest r, CancellationToken ct = default)
    {
        EnsureManage();
        var mobile = Validate(r);
        var op = await db.Operators.FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException("Operator");
        if (op.IsActive && !r.IsActive) op.SecurityStamp = Guid.NewGuid().ToString("N");   // disabling ends the current session
        op.Name = r.Name.Trim(); op.NameTa = Helpers.Clean(r.NameTa); op.Mobile = mobile; op.IsActive = r.IsActive;
        await db.SaveChangesAsync(ct);
        sessions.InvalidatePrincipal(PrincipalType.Operator, op.Id);
    }

    public async Task<OperatorCredentialsDto> ResetPinAsync(int id, CancellationToken ct = default)
    {
        EnsureManage();
        var op = await db.Operators.FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new NotFoundException("Operator");
        var pin = Helpers.RandomDigits(4);
        op.PinHash = hasher.Hash(pin);
        op.FailedPinCount = 0;                       // reset also unlocks
        op.LockoutUntil = null;
        op.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        sessions.InvalidatePrincipal(PrincipalType.Operator, op.Id);
        var tenantCode = await db.Tenants.Where(t => t.Id == TenantId).Select(t => t.Code).FirstAsync(ct);
        return new OperatorCredentialsDto(op.Id, tenantCode, op.Code, pin);
    }

    // ───────────── Assignment board: operators × functions × counters for one day ─────────────

    public async Task<AssignmentBoardDto> BoardAsync(DateTime date, CancellationToken ct = default)
    {
        current.Demand(Permissions.OperatorsView);
        var d = date.Date;
        var functions = await db.Functions
            .Where(f => f.FunctionDate == d && f.Status != FunctionStatus.Cancelled && f.Status != FunctionStatus.Closed)
            .OrderBy(f => f.StartTime)
            .Select(f => new BoardFunctionDto(f.Id, f.Name, f.NameTa, f.FunctionType!.Name, f.Location,
                f.StartTime == null ? null : f.StartTime.Value.ToString(), f.EndTime == null ? null : f.EndTime.Value.ToString(), f.Status.ToString(),
                db.Counters.Where(c => c.FunctionId == f.Id).OrderBy(c => c.Number)
                    .Select(c => new BoardCounterDto(c.Id, c.Number, c.Name,
                        db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => (int?)a.Id).FirstOrDefault(),
                        db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => (int?)a.OperatorId).FirstOrDefault(),
                        db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => a.Operator!.Name).FirstOrDefault(),
                        db.OperatorAssignments.Where(a => a.CounterId == c.Id && a.IsActive).Select(a => a.Operator!.Code).FirstOrDefault()))
                    .ToList()))
            .ToListAsync(ct);

        var operators = await db.Operators.Where(o => o.IsActive).OrderBy(o => o.Code)
            .Select(o => new BoardOperatorDto(o.Id, o.Code, o.Name, o.Mobile,
                db.OperatorAssignments.Where(a => a.OperatorId == o.Id && a.IsActive && a.Function!.FunctionDate == d)
                    .Select(a => (int?)a.FunctionId).FirstOrDefault()))
            .ToListAsync(ct);

        return new AssignmentBoardDto(d, functions, operators);
    }

    public async Task AssignAsync(AssignRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.OperatorsAssign);
        var f = await db.Functions.FirstOrDefaultAsync(x => x.Id == r.FunctionId, ct) ?? throw new NotFoundException("Function");
        var counter = await db.Counters.FirstOrDefaultAsync(c => c.Id == r.CounterId && c.FunctionId == f.Id, ct) ?? throw new NotFoundException("Counter");
        var op = await db.Operators.FirstOrDefaultAsync(o => o.Id == r.OperatorId && o.IsActive, ct) ?? throw new NotFoundException("Operator");

        if (await db.OperatorAssignments.AnyAsync(a => a.CounterId == counter.Id && a.IsActive, ct))
            throw new AppException($"{counter.Name} already has an operator. Remove them first.");

        // Working window: function date + start/end time (default whole day), with 1 hour buffer before
        var from = f.FunctionDate.Add(f.StartTime ?? TimeSpan.FromHours(5)).AddHours(-1);
        var to = f.FunctionDate.Add(f.EndTime ?? new TimeSpan(23, 59, 0)).AddHours(2);

        var clash = await db.OperatorAssignments.Include(a => a.Function)
            .FirstOrDefaultAsync(a => a.OperatorId == op.Id && a.IsActive && a.ValidFrom < to && a.ValidTo > from, ct);
        if (clash != null) throw new AppException($"{op.Name} is already assigned to {clash.Function!.Name} at that time.");

        db.OperatorAssignments.Add(new OperatorAssignment
        {
            TenantId = TenantId, FunctionId = f.Id, CounterId = counter.Id, OperatorId = op.Id, ValidFrom = from, ValidTo = to
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UnassignAsync(int assignmentId, CancellationToken ct = default)
    {
        current.Demand(Permissions.OperatorsAssign);
        var a = await db.OperatorAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, ct) ?? throw new NotFoundException("Assignment");
        a.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>For the logged-in operator: where am I working?</summary>
    public async Task<List<MyAssignmentDto>> MyAssignmentsAsync(CancellationToken ct = default)
    {
        if (!current.IsOperator) throw new ForbiddenException();
        var now = DateTime.Now;
        var fromDay = DateTime.Today.AddDays(-1);
        var rows = await db.OperatorAssignments
            .Where(a => a.OperatorId == current.OperatorId && a.IsActive && a.Function!.FunctionDate >= fromDay)
            .OrderBy(a => a.ValidFrom)
            .Select(a => new { a.Id, a.FunctionId, a.Function!.Name, a.Function.NameTa, a.Function.Location, a.Function.FunctionDate, a.CounterId, CounterName = a.Counter!.Name, a.ValidFrom, a.ValidTo, a.Function.Status })
            .ToListAsync(ct);
        return rows.Select(a => new MyAssignmentDto(a.Id, a.FunctionId, a.Name, a.NameTa, a.Location, a.FunctionDate, a.CounterId, a.CounterName,
            a.ValidFrom, a.ValidTo, a.ValidFrom <= now && a.ValidTo >= now && a.Status != FunctionStatus.Closed, a.Status.ToString())).ToList();
    }

    private void EnsureManage()
    {
        current.Demand(Permissions.OperatorsManage, "Only the vendor owner or manager can manage operators.");
    }

    private static string Validate(SaveOperatorRequest r)
    {
        var mobile = Helpers.NormalizeMobile(r.Mobile);
        new Validator().Require("name", r.Name, "Enter the operator's name")
            .Check(mobile != null, "mobile", "Enter a valid mobile number").ThrowIfInvalid();
        return mobile!;
    }
}
