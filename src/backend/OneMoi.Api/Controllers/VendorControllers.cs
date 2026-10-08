using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneMoi.Api.Security;
using OneMoi.Application.Common.Security;
using OneMoi.Application.Features.Dashboard;
using OneMoi.Application.Features.Functions;
using OneMoi.Application.Features.Masters;
using OneMoi.Application.Features.Moi;
using OneMoi.Application.Features.Operators;
using OneMoi.Application.Features.Reports;
using OneMoi.Application.Features.Tenants;

namespace OneMoi.Api.Controllers;

/*  Every endpoint names the ONE permission it needs.
 *  Tenant isolation is automatic: the database filter limits every query to the caller's vendor.
 *  Services re-check the same permission (defence in depth).                                    */

/// <summary>Vendor company profile, logo, staff logins and dashboard.</summary>
[ApiController, Route("api/vendor")]
public class VendorController(TenantService tenants, DashboardService dashboard) : ControllerBase
{
    [HttpGet("dashboard"), HasPermission(Permissions.TenantDashboard)]
    public Task<VendorDashboardDto> Dashboard(CancellationToken ct) => dashboard.VendorAsync(ct);

    [HttpGet("profile"), HasPermission(Permissions.TenantProfileView)]
    public Task<TenantProfileDto> Profile(CancellationToken ct) => tenants.GetProfileAsync(ct);

    [HttpPut("profile"), HasPermission(Permissions.TenantProfileManage)]
    public Task<TenantProfileDto> Update(UpdateTenantProfileRequest r, CancellationToken ct) => tenants.UpdateProfileAsync(r, ct);

    [HttpPost("logo"), HasPermission(Permissions.TenantProfileManage), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<object> Logo(IFormFile file, CancellationToken ct)
    {
        await using var s = file.OpenReadStream();
        return new { logoPath = await tenants.UploadLogoAsync(s, file.FileName, file.Length, ct) };
    }

    [HttpGet("members"), HasPermission(Permissions.TenantStaffView)]
    public Task<List<TenantMemberDto>> Members(CancellationToken ct) => tenants.MembersAsync(ct);

    [HttpPost("members"), HasPermission(Permissions.TenantStaffManage)]
    public Task<TenantMemberDto> AddMember(AddTenantMemberRequest r, CancellationToken ct) => tenants.AddMemberAsync(r, ct);

    [HttpPut("members/{id:int}"), HasPermission(Permissions.TenantStaffManage)]
    public Task UpdateMember(int id, UpdateTenantMemberRequest r, CancellationToken ct) => tenants.UpdateMemberAsync(id, r, ct);

    [HttpPost("members/{id:int}/reset-password"), HasPermission(Permissions.TenantStaffManage)]
    public Task ResetMemberPassword(int id, ResetMemberPasswordRequest r, CancellationToken ct) => tenants.ResetMemberPasswordAsync(id, r, ct);

    /// <summary>Who can do what (read-only matrix for the "Roles &amp; access" screen).</summary>
    [HttpGet("roles"), HasPermission(Permissions.TenantStaffView)]
    public List<RolePermissionsDto> Roles() => tenants.RoleMatrix();
}

/// <summary>Lookup tables: function-types | moi-categories | gift-item-types | expense-categories, plus denominations.</summary>
[ApiController, Route("api/masters")]
public class MastersController(MasterService masters) : ControllerBase
{
    [HttpGet("denominations"), HasPermission(Permissions.MastersView)]
    public Task<List<DenominationDto>> Denominations(CancellationToken ct) => masters.DenominationsAsync(ct);

    [HttpGet("{kind}"), HasPermission(Permissions.MastersView)]
    public Task<List<MasterItemDto>> List(string kind, [FromQuery] bool activeOnly = false, CancellationToken ct = default) => masters.ListAsync(kind, activeOnly, ct);

    // Vendor owner/manager (MastersManage) or super admin (PlatformMastersManage) — checked in the service
    [HttpPost("{kind}"), Authorize]
    public Task<MasterItemDto> Create(string kind, SaveMasterRequest r, CancellationToken ct) => masters.CreateAsync(kind, r, ct);

    [HttpPut("{kind}/{id:int}"), Authorize]
    public Task<MasterItemDto> Update(string kind, int id, SaveMasterRequest r, CancellationToken ct) => masters.UpdateAsync(kind, id, r, ct);

    [HttpDelete("{kind}/{id:int}"), Authorize]
    public Task Delete(string kind, int id, CancellationToken ct) => masters.DeleteAsync(kind, id, ct);
}

[ApiController, Route("api/functions")]
public class FunctionsController(FunctionService functions) : ControllerBase
{
    [HttpGet, HasPermission(Permissions.FunctionsView)]
    public Task<List<FunctionListItemDto>> List([FromQuery] FunctionQuery q, CancellationToken ct) => functions.ListAsync(q, ct);

    [HttpGet("{id:int}"), HasPermission(Permissions.FunctionsView)]
    public Task<FunctionDetailDto> Get(int id, CancellationToken ct) => functions.GetAsync(id, ct);

    [HttpPost, HasPermission(Permissions.FunctionsManage)]
    public Task<FunctionDetailDto> Create(SaveFunctionRequest r, CancellationToken ct) => functions.CreateAsync(r, ct);

    [HttpPut("{id:int}"), HasPermission(Permissions.FunctionsManage)]
    public Task<FunctionDetailDto> Update(int id, SaveFunctionRequest r, CancellationToken ct) => functions.UpdateAsync(id, r, ct);

    [HttpPut("{id:int}/status/{status}"), HasPermission(Permissions.FunctionsManage)]
    public Task<FunctionDetailDto> Status(int id, string status, CancellationToken ct) => functions.SetStatusAsync(id, status, ct);

    [HttpDelete("{id:int}"), HasPermission(Permissions.FunctionsManage)]
    public Task Delete(int id, CancellationToken ct) => functions.DeleteAsync(id, ct);
}

[ApiController, Route("api/operators")]
public class OperatorsController(OperatorService operators) : ControllerBase
{
    [HttpGet, HasPermission(Permissions.OperatorsView)]
    public Task<List<OperatorDto>> List(CancellationToken ct) => operators.ListAsync(ct);

    [HttpPost, HasPermission(Permissions.OperatorsManage)]
    public Task<OperatorCredentialsDto> Create(SaveOperatorRequest r, CancellationToken ct) => operators.CreateAsync(r, ct);

    [HttpPut("{id:int}"), HasPermission(Permissions.OperatorsManage)]
    public Task Update(int id, SaveOperatorRequest r, CancellationToken ct) => operators.UpdateAsync(id, r, ct);

    [HttpPost("{id:int}/reset-pin"), HasPermission(Permissions.OperatorsManage)]
    public Task<OperatorCredentialsDto> ResetPin(int id, CancellationToken ct) => operators.ResetPinAsync(id, ct);

    [HttpGet("board"), HasPermission(Permissions.OperatorsView)]
    public Task<AssignmentBoardDto> Board([FromQuery] DateTime? date, CancellationToken ct) => operators.BoardAsync(date ?? DateTime.Today, ct);

    [HttpPost("assignments"), HasPermission(Permissions.OperatorsAssign)]
    public Task Assign(AssignRequest r, CancellationToken ct) => operators.AssignAsync(r, ct);

    [HttpDelete("assignments/{id:int}"), HasPermission(Permissions.OperatorsAssign)]
    public Task Unassign(int id, CancellationToken ct) => operators.UnassignAsync(id, ct);
}

/// <summary>The logged-in counter operator's own assignments.</summary>
[ApiController, Route("api/operator"), Authorize(Roles = "Operator")]
public class OperatorMeController(OperatorService operators) : ControllerBase
{
    [HttpGet("assignments")] public Task<List<MyAssignmentDto>> Mine(CancellationToken ct) => operators.MyAssignmentsAsync(ct);
}

/// <summary>Moi entry, corrections, expenses and live summary (vendor staff and operators).</summary>
[ApiController, Route("api/moi")]
public class MoiController(MoiEntryService moi) : ControllerBase
{
    [HttpGet("lookup"), HasPermission(Permissions.MoiEntry)]
    public Task<PersonLookupDto> Lookup([FromQuery] string? mobile, [FromQuery] string? name, [FromQuery] string? city, CancellationToken ct) =>
        moi.LookupAsync(mobile ?? "", name, city, ct);

    [HttpPost("entries"), HasPermission(Permissions.MoiEntry)]
    public Task<MoiEntryDto> Create(CreateMoiEntryRequest r, CancellationToken ct) => moi.CreateAsync(r, ct);

    [HttpGet("entries"), HasPermission(Permissions.MoiView)]
    public Task<List<MoiEntryDto>> List([FromQuery] MoiListQuery q, CancellationToken ct) => moi.ListAsync(q, ct);

    [HttpGet("entries/{id:int}"), HasPermission(Permissions.MoiView)]
    public Task<MoiEntryDto> Get(int id, CancellationToken ct) => moi.GetAsync(id, ct);

    // MoiReverseAny (owner/manager) or MoiReverseOwn (operator, own entry, 10 min) — decided in the service
    [HttpPost("entries/{id:int}/reverse"), Authorize(Roles = "TenantUser,Operator")]
    public Task<MoiEntryDto> Reverse(int id, ReverseRequest r, CancellationToken ct) => moi.ReverseAsync(id, r, ct);

    [HttpGet("functions/{functionId:int}/summary"), HasPermission(Permissions.MoiView)]
    public Task<FunctionSummaryDto> Summary(int functionId, CancellationToken ct) => moi.SummaryAsync(functionId, ct);

    [HttpGet("functions/{functionId:int}/expenses"), HasPermission(Permissions.ExpensesView)]
    public Task<List<ExpenseDto>> Expenses(int functionId, CancellationToken ct) => moi.ExpensesAsync(functionId, ct);

    [HttpPost("expenses"), HasPermission(Permissions.ExpensesCreate)]
    public Task<ExpenseDto> AddExpense(CreateExpenseRequest r, CancellationToken ct) => moi.AddExpenseAsync(r, ct);

    [HttpDelete("expenses/{id:int}"), HasPermission(Permissions.ExpensesDelete)]
    public Task DeleteExpense(int id, CancellationToken ct) => moi.DeleteExpenseAsync(id, ct);
}

/// <summary>Function Moi book in English or Tamil (vendor staff with report permission + the function host).</summary>
[ApiController, Route("api/reports")]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("functions/{id:int}"), HasPermission(Permissions.ReportsView)]
    public Task<FunctionReportDto> Function(int id, CancellationToken ct) => reports.FunctionReportAsync(id, maskMobiles: false, ct);

    [HttpGet("functions/{id:int}/csv"), HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> Csv(int id, [FromQuery] string lang = "en", CancellationToken ct = default)
    {
        var (bytes, name) = await reports.FunctionCsvAsync(id, lang, ct);
        return File(bytes, "text/csv; charset=utf-8", name);
    }
}
