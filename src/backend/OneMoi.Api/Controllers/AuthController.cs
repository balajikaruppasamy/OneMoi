using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OneMoi.Api.Security;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Application.Features.Auth;

namespace OneMoi.Api.Controllers;

/// <summary>Login, registration, OTP, password and session endpoints. Login endpoints are rate-limited per IP.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("otp/send"), EnableRateLimiting(RateLimits.Otp)]
    public Task<SendOtpResponse> SendOtp(SendOtpRequest req, CancellationToken ct) => auth.SendOtpAsync(req, ct);

    [HttpPost("otp/verify"), EnableRateLimiting(RateLimits.Login)]
    public Task<AuthResponse> VerifyOtp(VerifyOtpRequest req, CancellationToken ct) => auth.VerifyOtpAsync(req, ct);

    [HttpPost("login/password"), EnableRateLimiting(RateLimits.Login)]
    public Task<AuthResponse> PasswordLogin(PasswordLoginRequest req, CancellationToken ct) => auth.PasswordLoginAsync(req, ct);

    [HttpPost("login/operator"), EnableRateLimiting(RateLimits.Login)]
    public Task<AuthResponse> OperatorLogin(OperatorLoginRequest req, CancellationToken ct) => auth.OperatorLoginAsync(req, ct);

    [HttpPost("register/individual"), EnableRateLimiting(RateLimits.Otp)]
    public Task<SendOtpResponse> RegisterIndividual(RegisterIndividualRequest req, CancellationToken ct) => auth.RegisterIndividualAsync(req, ct);

    [HttpPost("register/vendor"), EnableRateLimiting(RateLimits.Otp)]
    public Task<SendOtpResponse> RegisterVendor(RegisterVendorRequest req, CancellationToken ct) => auth.RegisterVendorAsync(req, ct);

    /// <summary>Forgot password — step 1: send a reset code to the account's mobile / e-mail.</summary>
    [HttpPost("password/forgot"), EnableRateLimiting(RateLimits.Otp)]
    public Task<SendOtpResponse> Forgot(ForgotPasswordRequest req, CancellationToken ct) => auth.ForgotPasswordAsync(req, ct);

    /// <summary>Forgot password — step 2: code + new password. Also unlocks a locked account.</summary>
    [HttpPost("password/reset"), EnableRateLimiting(RateLimits.Login)]
    public Task<AuthResponse> Reset(ResetPasswordRequest req, CancellationToken ct) => auth.ResetPasswordAsync(req, ct);

    /// <summary>Logged-in user changes their password. Other devices are signed out.</summary>
    [HttpPost("password/change"), HasPermission(Permissions.AccountManage)]
    public Task<AuthResponse> Change(ChangePasswordRequest req, CancellationToken ct) => auth.ChangePasswordAsync(req, ct);

    [HttpPost("refresh"), EnableRateLimiting(RateLimits.Login)]
    public Task<AuthResponse> Refresh(RefreshRequest req, CancellationToken ct) => auth.RefreshAsync(req, ct);

    [HttpPost("logout")]
    public Task Logout(RefreshRequest req, CancellationToken ct) => auth.LogoutAsync(req, ct);

    /// <summary>Sign out from every device (new security stamp, all refresh tokens revoked).</summary>
    [HttpPost("logout-all"), Authorize]
    public Task LogoutAll(CancellationToken ct) => auth.LogoutAllAsync(ct);

    [HttpGet("me"), Authorize]
    public Task<SessionProfile> Me(CancellationToken ct) => auth.MeAsync(ct);
}

[ApiController]
[Route("api/tools")]
public class ToolsController(ITransliterationService tl) : ControllerBase
{
    /// <summary>English → Tamil. "Ram illa villa" → "ராம் இல்லா வில்லா" with alternatives per word.</summary>
    [Authorize, HttpGet("transliterate")]
    public Task<TransliterationResult> Transliterate([FromQuery] string text, [FromQuery] int max = 4, CancellationToken ct = default) =>
        tl.ToTamilAsync(text, max, ct);
}
