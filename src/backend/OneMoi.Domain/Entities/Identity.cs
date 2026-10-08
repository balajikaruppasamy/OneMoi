using OneMoi.Domain.Common;
using OneMoi.Domain.Enums;

namespace OneMoi.Domain.Entities;

/// <summary>Login account. Super admins, vendor staff and individuals all live here.</summary>
public class AppUser : AuditableEntity
{
    public string FullName { get; set; } = "";
    public string? FullNameTa { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? PasswordHash { get; set; }          // null = OTP-only account
    public UserType UserType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsMobileVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ── Security ──
    /// <summary>Wrong password count since the last successful login.</summary>
    public int FailedLoginCount { get; set; }
    /// <summary>Locked until this time (UTC) after too many wrong passwords.</summary>
    public DateTime? LockoutUntil { get; set; }
    /// <summary>Changes on password change / deactivation / role change / "logout all devices".
    /// Tokens carrying an old stamp are rejected on the next request.</summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>Individuals are linked to their global Moi identity.</summary>
    public int? PersonId { get; set; }
    public Person? Person { get; set; }

    public ICollection<TenantMember> Memberships { get; set; } = new List<TenantMember>();
}

/// <summary>One OTP sent by SMS or e-mail. Only a hash of the code is stored.</summary>
public class OtpRequest : BaseEntity
{
    public OtpChannel Channel { get; set; }
    public string Destination { get; set; } = "";      // mobile or e-mail
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>Long-lived token used to get a new access token without logging in again.</summary>
public class RefreshToken : BaseEntity
{
    public PrincipalType PrincipalType { get; set; }
    public int PrincipalId { get; set; }               // AppUser.Id or Operator.Id
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }          // rotated | logout | logout-all | reuse-detected
    public string? CreatedByIp { get; set; }
    /// <summary>All tokens from one login share a family. If an already-used token is presented again
    /// (stolen copy), the whole family is revoked.</summary>
    public string FamilyId { get; set; } = "";
    public string? ReplacedByHash { get; set; }
}

/// <summary>Every SMS / e-mail / WhatsApp the system sends (OTP codes are masked).</summary>
public class NotificationLog : BaseEntity
{
    public int? TenantId { get; set; }
    public OtpChannel Channel { get; set; }
    public string Recipient { get; set; } = "";
    public string? Subject { get; set; }
    public string Body { get; set; } = "";
    public NotificationStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Who did what. Logins, approvals, corrections, etc.</summary>
public class AuditLog : BaseEntity
{
    public int? TenantId { get; set; }
    public PrincipalType? PrincipalType { get; set; }
    public int? PrincipalId { get; set; }
    public string Action { get; set; } = "";           // e.g. LOGIN_OTP, MOI_REVERSED
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
