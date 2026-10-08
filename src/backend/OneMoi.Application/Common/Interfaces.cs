using Microsoft.EntityFrameworkCore;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Common;

/// <summary>
/// The database as seen by business logic. Implemented by Infrastructure (EF Core),
/// so services never know whether it is SQL Server or PostgreSQL.
/// </summary>
public interface IAppDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<OtpRequest> OtpRequests { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Plan> Plans { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantMember> TenantMembers { get; }
    DbSet<Operator> Operators { get; }

    DbSet<FunctionType> FunctionTypes { get; }
    DbSet<MoiCategory> MoiCategories { get; }
    DbSet<GiftItemType> GiftItemTypes { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Denomination> Denominations { get; }

    DbSet<Function> Functions { get; }
    DbSet<Counter> Counters { get; }
    DbSet<OperatorAssignment> OperatorAssignments { get; }

    DbSet<Person> Persons { get; }
    DbSet<MoiEntry> MoiEntries { get; }
    DbSet<MoiEntryDenomination> MoiEntryDenominations { get; }
    DbSet<MoiEntryGift> MoiEntryGifts { get; }
    DbSet<FunctionExpense> FunctionExpenses { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Who is calling the API (read from the JWT).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    PrincipalType? PrincipalType { get; }
    int? UserId { get; }
    int? OperatorId { get; }
    int? TenantId { get; }
    UserType? UserType { get; }
    TenantRole? TenantRole { get; }
    int? PersonId { get; }
    string? Mobile { get; }
    string? Name { get; }
    string? SecurityStamp { get; }
    string? IpAddress { get; }
    bool IsSuperAdmin => UserType == Domain.Enums.UserType.SuperAdmin;
    bool IsOperator => PrincipalType == Domain.Enums.PrincipalType.Operator;
}

public interface IPasswordHasher
{
    string Hash(string secret);
    bool Verify(string secret, string hash);
}

public record TokenPair(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

public record TokenSubject(
    PrincipalType PrincipalType, int PrincipalId, string Name, UserType? UserType,
    int? TenantId, TenantRole? TenantRole, int? PersonId, string? Mobile, string SecurityStamp);

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateAccessToken(TokenSubject subject);
    string CreateRefreshToken();
    string HashToken(string token);
}

/// <summary>Sends SMS / e-mail. Development implementation only writes to NotificationLogs.</summary>
public interface INotificationSender
{
    Task SendAsync(OtpChannel channel, string recipient, string? subject, string body, string logBody, int? tenantId = null, CancellationToken ct = default);
}

public record TransliterationResult(string Input, string Tamil, IReadOnlyList<WordSuggestions> Words, string Source);
public record WordSuggestions(string Word, IReadOnlyList<string> Options);

/// <summary>English (Tanglish) → Tamil script. "Ram illa villa" → "ராம் இல்லா வில்லா".</summary>
public interface ITransliterationService
{
    Task<TransliterationResult> ToTamilAsync(string text, int maxOptions = 4, CancellationToken ct = default);
}

public interface IFileStorage
{
    /// <returns>Public relative URL, e.g. /uploads/tenants/1/logo.png</returns>
    Task<string> SaveAsync(Stream content, string folder, string fileName, CancellationToken ct = default);
}

public record AppSettings
{
    public bool ExposeOtpInResponse { get; init; }     // DEV ONLY: return OTP to the UI for testing
    public int OtpExpiryMinutes { get; init; } = 5;
    public int OtpMaxPerHour { get; init; } = 5;
    public string OtpSecret { get; init; } = "change-me";
    public int RefreshTokenDays { get; init; } = 30;
    public int CorrectionWindowMinutes { get; init; } = 10;
    public int MaxFailedLogins { get; init; } = 5;        // wrong password / PIN attempts before lockout
    public int LockoutMinutes { get; init; } = 15;
}

/// <summary>What the server currently knows about a logged-in principal (checked on every request).</summary>
public record PrincipalState(bool IsActive, string SecurityStamp, DateTime? LockoutUntil, int? TenantId, TenantRole? TenantRole);

/// <summary>
/// Short-lived cache of account / vendor status so every request can be validated cheaply.
/// Call Invalidate… after changing a user, operator or vendor so it takes effect immediately.
/// </summary>
public interface ISessionStateStore
{
    Task<PrincipalState?> GetPrincipalAsync(PrincipalType type, int id, CancellationToken ct = default);
    Task<TenantStatus?> GetTenantStatusAsync(int tenantId, CancellationToken ct = default);
    void InvalidatePrincipal(PrincipalType type, int id);
    void InvalidateTenant(int tenantId);
}
