namespace OneMoi.Application.Features.Auth;

/// <summary>purpose: "login" | "register"</summary>
public record SendOtpRequest(string Destination, string Purpose = "login");

public record SendOtpResponse(string SentTo, string Channel, int ExpiresInSeconds, int ResendAfterSeconds, string? DevOtp);

/// <summary>loginAs: "individual" | "tenant" (which account to open for this mobile/e-mail)</summary>
public record VerifyOtpRequest(string Destination, string Code, string Purpose = "login", string LoginAs = "individual");

public record PasswordLoginRequest(string Login, string Password);

public record OperatorLoginRequest(string TenantCode, string OperatorCode, string Pin);

public record RefreshRequest(string RefreshToken);

public record RegisterIndividualRequest(
    string Name, string? NameTa, string Mobile, string? Email, string? City, string? Work, bool Consent);

public record RegisterVendorRequest(
    string BusinessName, string? BusinessNameTa, string OwnerName, string Mobile, string? Email,
    string? City, string Password, bool Consent);

public record SessionProfile(
    string PrincipalType,          // User | Operator
    int Id,
    string Name,
    string? NameTa,
    string? UserType,              // SuperAdmin | TenantUser | Individual (null for operators)
    string? Role,                  // Owner | Manager | Accountant | Operator
    int? TenantId,
    string? TenantCode,
    string? TenantName,
    string? TenantLogo,
    string? TenantStatus,
    int? PersonId,
    string? Mobile,
    string? Email,
    bool IsHost,                   // individual who owns at least one function
    string Home,                   // suggested landing route for the UI
    bool HasPassword,              // false for OTP-only individuals (no change-password screen)
    IReadOnlyCollection<string> Permissions);   // what this login may do (UI hides the rest)

public record AuthResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken, SessionProfile Profile);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Step 1 of "forgot password": e-mail or mobile of a password account.</summary>
public record ForgotPasswordRequest(string Login);

/// <summary>Step 2: the OTP received + the new password.</summary>
public record ResetPasswordRequest(string Login, string Code, string NewPassword);
