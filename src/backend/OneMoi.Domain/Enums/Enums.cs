namespace OneMoi.Domain.Enums;

/// <summary>What kind of login an account uses.</summary>
public enum UserType
{
    SuperAdmin = 1,   // OneMoi platform owner
    TenantUser = 2,   // Moi vendor staff (owner / manager / accountant)
    Individual = 3    // A person who gives Moi (also function hosts)
}

/// <summary>Role of a user inside a tenant (Moi vendor).</summary>
public enum TenantRole
{
    Owner = 1,
    Manager = 2,
    Accountant = 3
}

public enum TenantStatus
{
    Pending = 1,     // registered, waiting for platform approval
    Active = 2,
    Suspended = 3
}

public enum FunctionStatus
{
    Draft = 1,
    Scheduled = 2,
    Live = 3,
    Closed = 4,
    Cancelled = 5
}

public enum PaymentMode
{
    Cash = 1,
    Upi = 2,
    Card = 3,
    Cheque = 4,
    GiftOnly = 5     // only a gift item, no money
}

public enum MoiEntryStatus
{
    Active = 1,
    Reversed = 2     // never deleted, reversed with a reason
}

public enum OtpChannel
{
    Sms = 1,
    Email = 2
}

public enum OtpPurpose
{
    Login = 1,
    Register = 2,
    ResetPassword = 3,
    VerifyEmail = 4
}

public enum NotificationStatus
{
    Queued = 1,
    Sent = 2,
    Failed = 3,
    Simulated = 4    // development mode: not actually sent
}

/// <summary>Who a token / audit row belongs to.</summary>
public enum PrincipalType
{
    User = 1,
    Operator = 2
}
