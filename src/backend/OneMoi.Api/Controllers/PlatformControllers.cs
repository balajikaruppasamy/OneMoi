using Microsoft.AspNetCore.Mvc;
using OneMoi.Api.Security;
using OneMoi.Application.Common.Security;
using OneMoi.Application.Features.Dashboard;
using OneMoi.Application.Features.Individuals;
using OneMoi.Application.Features.Tenants;

namespace OneMoi.Api.Controllers;

/// <summary>Individual (guest / function host) — own data only.</summary>
[ApiController, Route("api/me")]
public class MeController(MyMoiService me) : ControllerBase
{
    [HttpGet("moi"), HasPermission(Permissions.MeMoiView)]
    public Task<MyMoiResponse> Moi([FromQuery] int? year, CancellationToken ct) => me.MyMoiAsync(year, ct);

    [HttpGet("profile"), HasPermission(Permissions.MeProfileManage)]
    public Task<MyProfileDto> Profile(CancellationToken ct) => me.ProfileAsync(ct);

    [HttpPut("profile"), HasPermission(Permissions.MeProfileManage)]
    public Task<MyProfileDto> Update(UpdateMyProfileRequest r, CancellationToken ct) => me.UpdateProfileAsync(r, ct);

    [HttpGet("hosted-functions"), HasPermission(Permissions.MeHostedView)]
    public Task<List<HostedFunctionDto>> Hosted(CancellationToken ct) => me.HostedFunctionsAsync(ct);
}

/// <summary>OneMoi platform owner console.</summary>
[ApiController, Route("api/admin")]
public class AdminController(DashboardService admin) : ControllerBase
{
    [HttpGet("dashboard"), HasPermission(Permissions.PlatformDashboard)]
    public Task<AdminDashboardDto> Dashboard(CancellationToken ct) => admin.AdminAsync(ct);

    [HttpGet("tenants"), HasPermission(Permissions.PlatformTenantsView)]
    public Task<List<AdminTenantDto>> Tenants(CancellationToken ct) => admin.TenantsAsync(ct);

    [HttpPut("tenants/{id:int}/status/{status}"), HasPermission(Permissions.PlatformTenantsManage)]
    public Task<TenantProfileDto> SetStatus(int id, string status, CancellationToken ct) => admin.SetTenantStatusAsync(id, status, ct);

    [HttpGet("users"), HasPermission(Permissions.PlatformUsersView)]
    public Task<List<AdminUserDto>> Users(CancellationToken ct) => admin.UsersAsync(ct);

    [HttpPut("users/{id:int}/active/{active:bool}"), HasPermission(Permissions.PlatformUsersManage)]
    public Task SetActive(int id, bool active, CancellationToken ct) => admin.SetUserActiveAsync(id, active, ct);

    [HttpPost("users/{id:int}/unlock"), HasPermission(Permissions.PlatformUsersManage)]
    public Task Unlock(int id, CancellationToken ct) => admin.UnlockUserAsync(id, ct);

    [HttpGet("otp-requests"), HasPermission(Permissions.PlatformLogsView)]
    public Task<List<OtpLogDto>> Otps(CancellationToken ct) => admin.OtpLogAsync(ct);

    [HttpGet("notifications"), HasPermission(Permissions.PlatformLogsView)]
    public Task<List<NotificationLogDto>> Notifications(CancellationToken ct) => admin.NotificationsAsync(ct);

    [HttpGet("audit"), HasPermission(Permissions.PlatformLogsView)]
    public Task<List<AuditLogDto>> Audit(CancellationToken ct) => admin.AuditAsync(ct);
}

public static class RateLimits
{
    public const string Login = "login";   // 30 attempts / minute / IP (a mandapam Wi-Fi shares one IP)
    public const string Otp = "otp";       // 10 OTP sends / minute / IP
}
