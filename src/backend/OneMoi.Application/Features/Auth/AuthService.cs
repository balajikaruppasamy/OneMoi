using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Auth;

/// <summary>
/// AUTHENTICATION — proving who you are.
///  • Mobile / e-mail OTP  → Individuals (auto-created on first login) and vendor staff
///  • E-mail/mobile + password → Super admin and vendor staff (locked after N wrong attempts)
///  • Vendor code + operator code + PIN → Counter operators (locked after N wrong attempts)
/// Sessions = short JWT access token + rotating refresh token (stolen-token reuse is detected).
/// </summary>
public partial class AuthService(
    IAppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    INotificationSender notifier,
    ICurrentUser current,
    ISessionStateStore sessions,
    AppSettings settings)
{
    // ───────────────────────────── OTP ─────────────────────────────

    public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest req, CancellationToken ct = default)
    {
        var (channel, destination) = ParseDestination(req.Destination);
        return await IssueOtpAsync(channel, destination, ParsePurpose(req.Purpose), ct);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest req, CancellationToken ct = default)
    {
        var (channel, destination) = ParseDestination(req.Destination);
        await ConsumeOtpAsync(destination, ParsePurpose(req.Purpose), req.Code, ct);

        var byMobile = channel == OtpChannel.Sms;
        var users = await db.Users.Where(u => byMobile ? u.Mobile == destination : u.Email == destination).ToListAsync(ct);

        AppUser? user;
        if (req.LoginAs.Equals("tenant", StringComparison.OrdinalIgnoreCase))
        {
            user = users.FirstOrDefault(u => u.UserType == UserType.TenantUser)
                   ?? throw new AppException("No Moi vendor account found for this number. Please register as a vendor.", 404);
        }
        else
        {
            user = users.FirstOrDefault(u => u.UserType == UserType.Individual);
            if (user == null)
            {
                if (!byMobile) throw new AppException("No account found for this e-mail. Please login with your mobile number.", 404);
                user = await CreateIndividualAsync(destination, null, null, null, null, null, ct);   // first login = free sign-up
            }
        }

        EnsureUserCanLogin(user, checkLockout: false);   // OTP proves the phone; lockout applies to passwords
        if (byMobile) user.IsMobileVerified = true; else user.IsEmailVerified = true;
        if (user.UserType == UserType.Individual) await ClaimPersonAsync(user, ct);

        return await IssueForUserAsync(user, "LOGIN_OTP", null, ct);
    }

    // ─────────────────────────── Password ──────────────────────────

    public async Task<AuthResponse> PasswordLoginAsync(PasswordLoginRequest req, CancellationToken ct = default)
    {
        var user = await FindPasswordUserAsync(req.Login, ct);
        if (user == null)
        {
            await AuditAsync("LOGIN_FAILED", null, null, $"unknown login={req.Login?.Trim()}", ct);
            throw new AppException("Incorrect login or password.", 401);
        }
        EnsureUserCanLogin(user, checkLockout: true);

        if (!hasher.Verify(req.Password ?? "", user.PasswordHash!))
        {
            user.FailedLoginCount++;
            var locked = user.FailedLoginCount >= settings.MaxFailedLogins;
            if (locked) user.LockoutUntil = DateTime.UtcNow.AddMinutes(settings.LockoutMinutes);
            await AuditAsync(locked ? "ACCOUNT_LOCKED" : "LOGIN_FAILED", null, (PrincipalType.User, user.Id), $"attempt {user.FailedLoginCount}", ct);
            throw new AppException(locked
                ? $"Too many wrong passwords. Your account is locked for {settings.LockoutMinutes} minutes. Use \"Forgot password\" to unlock now."
                : $"Incorrect login or password. {settings.MaxFailedLogins - user.FailedLoginCount} attempt(s) left.", 401);
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        return await IssueForUserAsync(user, "LOGIN_PASSWORD", null, ct);
    }

    public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest req, CancellationToken ct = default)
    {
        current.Demand(Permissions.AccountManage);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.UserId, ct) ?? throw new AppException("Session expired.", 401);
        if (user.PasswordHash == null) throw new AppException("This account logs in with OTP and has no password.");
        if (!hasher.Verify(req.CurrentPassword ?? "", user.PasswordHash))
            throw new ValidationException("currentPassword", "Current password is incorrect.");
        ValidatePassword(req.NewPassword, "newPassword");
        if (hasher.Verify(req.NewPassword, user.PasswordHash))
            throw new ValidationException("newPassword", "New password must be different from the current one.");

        SetPassword(user, req.NewPassword);
        await RevokeAllTokensAsync(PrincipalType.User, user.Id, "password-changed", ct);
        // Other devices are now logged out; this device gets a fresh session.
        return await IssueForUserAsync(user, "PASSWORD_CHANGED", null, ct);
    }

    /// <summary>Always answers the same way, so nobody can find out which logins exist.</summary>
    public async Task<SendOtpResponse> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default)
    {
        var user = await FindPasswordUserAsync(req.Login, ct);
        var (channel, destination) = ResetDestination(req.Login, user);
        if (user == null || destination == null)
            return new SendOtpResponse(Mask(req.Login?.Trim() ?? ""), "Sms", settings.OtpExpiryMinutes * 60, 30, null);
        return await IssueOtpAsync(channel, destination, OtpPurpose.ResetPassword, ct);
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        ValidatePassword(req.NewPassword, "newPassword");
        var user = await FindPasswordUserAsync(req.Login, ct) ?? throw new AppException("This code has expired. Please request a new OTP.", 400);
        var (_, destination) = ResetDestination(req.Login, user);
        await ConsumeOtpAsync(destination!, OtpPurpose.ResetPassword, req.Code, ct);
        EnsureUserCanLogin(user, checkLockout: false);

        SetPassword(user, req.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;                    // reset also unlocks
        await RevokeAllTokensAsync(PrincipalType.User, user.Id, "password-reset", ct);
        return await IssueForUserAsync(user, "PASSWORD_RESET", null, ct);
    }

    // ─────────────────────────── Operator ──────────────────────────

    public async Task<AuthResponse> OperatorLoginAsync(OperatorLoginRequest req, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Code == (req.TenantCode ?? "").Trim().ToUpper(), ct)
                     ?? throw new AppException("Incorrect vendor code, operator ID or PIN.", 401);
        if (tenant.Status != TenantStatus.Active) throw new AppException("This vendor account is not active yet.", 403);

        var code = (req.OperatorCode ?? "").Trim().ToUpper();
        var op = await db.Operators.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.TenantId == tenant.Id && o.Code == code && !o.IsDeleted, ct);
        if (op == null) throw new AppException("Incorrect vendor code, operator ID or PIN.", 401);
        if (!op.IsActive) throw new AppException("This operator login is disabled. Contact your vendor.", 403);
        if (op.LockoutUntil > DateTime.UtcNow)
            throw new AppException($"Too many wrong PINs. Try again after {ToIst(op.LockoutUntil!.Value):h:mm tt} or ask your vendor to reset the PIN.", 423);

        if (!hasher.Verify(req.Pin ?? "", op.PinHash))
        {
            op.FailedPinCount++;
            var locked = op.FailedPinCount >= settings.MaxFailedLogins;
            if (locked) op.LockoutUntil = DateTime.UtcNow.AddMinutes(settings.LockoutMinutes);
            await AuditAsync(locked ? "OPERATOR_LOCKED" : "OPERATOR_LOGIN_FAILED", tenant.Id, (PrincipalType.Operator, op.Id), $"attempt {op.FailedPinCount}", ct);
            throw new AppException(locked
                ? $"Too many wrong PINs. Locked for {settings.LockoutMinutes} minutes."
                : $"Incorrect PIN. {settings.MaxFailedLogins - op.FailedPinCount} attempt(s) left.", 401);
        }

        op.FailedPinCount = 0;
        op.LockoutUntil = null;
        op.LastLoginAt = DateTime.UtcNow;
        await AuditAsync("LOGIN_OPERATOR", tenant.Id, (PrincipalType.Operator, op.Id), null, ct);
        return await IssueAsync(OperatorSubject(op), OperatorProfile(op, tenant), null, ct);
    }

    // ─────────────────────────── Register ──────────────────────────

    public async Task<SendOtpResponse> RegisterIndividualAsync(RegisterIndividualRequest req, CancellationToken ct = default)
    {
        var mobile = Helpers.NormalizeMobile(req.Mobile);
        new Validator()
            .Require("name", req.Name, "Enter your name")
            .Check(mobile != null, "mobile", "Enter a valid 10-digit mobile number")
            .Check(string.IsNullOrWhiteSpace(req.Email) || Helpers.IsEmail(req.Email), "email", "Enter a valid e-mail")
            .Check(req.Consent, "consent", "Please accept the terms to continue")
            .ThrowIfInvalid();

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Mobile == mobile && u.UserType == UserType.Individual, ct);
        if (existing is { IsMobileVerified: true })
            throw new ValidationException("mobile", "This mobile number is already registered. Please login.");

        if (existing == null)
            await CreateIndividualAsync(mobile!, req.Name.Trim(), Helpers.Clean(req.NameTa), Helpers.Clean(req.Email)?.ToLower(), Helpers.Clean(req.City), Helpers.Clean(req.Work), ct);
        else
        {
            existing.FullName = req.Name.Trim();
            existing.FullNameTa = Helpers.Clean(req.NameTa);
            existing.Email = Helpers.Clean(req.Email)?.ToLower();
            await db.SaveChangesAsync(ct);
        }
        return await IssueOtpAsync(OtpChannel.Sms, mobile!, OtpPurpose.Register, ct);
    }

    public async Task<SendOtpResponse> RegisterVendorAsync(RegisterVendorRequest req, CancellationToken ct = default)
    {
        var mobile = Helpers.NormalizeMobile(req.Mobile);
        new Validator()
            .Require("businessName", req.BusinessName, "Enter your business name")
            .Require("ownerName", req.OwnerName, "Enter the owner's name")
            .Check(mobile != null, "mobile", "Enter a valid 10-digit mobile number")
            .Check(string.IsNullOrWhiteSpace(req.Email) || Helpers.IsEmail(req.Email), "email", "Enter a valid e-mail")
            .Check(req.Consent, "consent", "Please accept the terms to continue")
            .ThrowIfInvalid();
        ValidatePassword(req.Password, "password");

        if (await db.Users.AnyAsync(u => u.Mobile == mobile && u.UserType == UserType.TenantUser, ct))
            throw new ValidationException("mobile", "A vendor account already exists for this mobile. Please login.");
        var email = Helpers.Clean(req.Email)?.ToLower();
        if (email != null && await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ValidationException("email", "This e-mail is already used by another account.");

        var tenant = new Tenant
        {
            Code = await UniqueTenantCodeAsync(req.BusinessName, ct), Name = req.BusinessName.Trim(), NameTa = Helpers.Clean(req.BusinessNameTa),
            OwnerName = req.OwnerName.Trim(), Mobile = mobile!, Email = email, City = Helpers.Clean(req.City), Status = TenantStatus.Pending,
            PlanId = await db.Plans.OrderBy(p => p.Id).Select(p => (int?)p.Id).FirstOrDefaultAsync(ct), TrialEndsAt = DateTime.UtcNow.AddDays(14)
        };
        var user = new AppUser { FullName = req.OwnerName.Trim(), Mobile = mobile, Email = email, UserType = UserType.TenantUser };
        SetPassword(user, req.Password);
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.TenantMembers.Add(new TenantMember { Tenant = tenant, User = user, Role = TenantRole.Owner });
        await db.SaveChangesAsync(ct);

        return await IssueOtpAsync(OtpChannel.Sms, mobile!, OtpPurpose.Register, ct);
    }

    // ───────────────────────── Tokens / session ────────────────────────

    /// <summary>
    /// Rotating refresh tokens. Each refresh token works ONCE. If an already-used token comes back,
    /// someone copied it → every session from that login is revoked.
    /// </summary>
    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var hash = tokens.HashToken(req.RefreshToken ?? "");
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct)
                 ?? throw new AppException("Session expired. Please login again.", 401);

        if (rt.RevokedAt != null)
        {
            if (rt.RevokedReason == "rotated")
            {
                var family = await db.RefreshTokens.Where(r => r.FamilyId == rt.FamilyId && r.RevokedAt == null).ToListAsync(ct);
                family.ForEach(r => { r.RevokedAt = DateTime.UtcNow; r.RevokedReason = "reuse-detected"; });
                await AuditAsync("TOKEN_REUSE_DETECTED", null, (rt.PrincipalType, rt.PrincipalId), $"family {rt.FamilyId}", ct);
            }
            throw new AppException("Session expired. Please login again.", 401);
        }
        if (rt.ExpiresAt < DateTime.UtcNow) throw new AppException("Session expired. Please login again.", 401);

        AuthResponse result;
        if (rt.PrincipalType == PrincipalType.Operator)
        {
            var op = await db.Operators.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == rt.PrincipalId && !o.IsDeleted, ct);
            var tenant = op == null ? null : await db.Tenants.FirstOrDefaultAsync(t => t.Id == op.TenantId, ct);
            if (op == null || !op.IsActive || tenant?.Status != TenantStatus.Active) throw new AppException("Your login is no longer active.", 401);
            result = await IssueAsync(OperatorSubject(op), OperatorProfile(op, tenant), rt.FamilyId, ct, beforeSave: newHash => Rotate(rt, newHash));
        }
        else
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == rt.PrincipalId, ct) ?? throw new AppException("Session expired.", 401);
            EnsureUserCanLogin(user, checkLockout: false);
            result = await IssueForUserAsync(user, null, rt.FamilyId, ct, beforeSave: newHash => Rotate(rt, newHash));
        }
        return result;
    }

    public async Task LogoutAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var hash = tokens.HashToken(req.RefreshToken ?? "");
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (rt == null || rt.RevokedAt != null) return;
        var family = await db.RefreshTokens.Where(r => r.FamilyId == rt.FamilyId && r.RevokedAt == null).ToListAsync(ct);
        family.ForEach(r => { r.RevokedAt = DateTime.UtcNow; r.RevokedReason = "logout"; });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Signs out every device: new security stamp + all refresh tokens revoked.</summary>
    public async Task LogoutAllAsync(CancellationToken ct = default)
    {
        if (current.IsOperator)
        {
            var op = await db.Operators.FirstAsync(o => o.Id == current.OperatorId, ct);
            op.SecurityStamp = NewStamp();
            await RevokeAllTokensAsync(PrincipalType.Operator, op.Id, "logout-all", ct);
            sessions.InvalidatePrincipal(PrincipalType.Operator, op.Id);
        }
        else
        {
            var user = await db.Users.FirstAsync(u => u.Id == current.UserId, ct);
            user.SecurityStamp = NewStamp();
            await RevokeAllTokensAsync(PrincipalType.User, user.Id, "logout-all", ct);
            sessions.InvalidatePrincipal(PrincipalType.User, user.Id);
        }
        await AuditAsync("LOGOUT_ALL", current.TenantId, (current.PrincipalType!.Value, current.OperatorId ?? current.UserId!.Value), null, ct);
    }

    public async Task<SessionProfile> MeAsync(CancellationToken ct = default)
    {
        if (current.IsOperator)
        {
            var op = await db.Operators.IgnoreQueryFilters().FirstAsync(o => o.Id == current.OperatorId, ct);
            var t = await db.Tenants.FirstAsync(x => x.Id == op.TenantId, ct);
            return OperatorProfile(op, t);
        }
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.UserId, ct) ?? throw new AppException("Session expired.", 401);
        return (await BuildUserSessionAsync(user, ct)).profile;
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private void EnsureUserCanLogin(AppUser user, bool checkLockout)
    {
        if (!user.IsActive) throw new AppException("This account is disabled. Please contact support.", 403);
        if (checkLockout && user.LockoutUntil > DateTime.UtcNow)
            throw new AppException($"Account locked after too many wrong passwords. Try again after {ToIst(user.LockoutUntil!.Value):h:mm tt}, or use \"Forgot password\".", 423);
    }

    private async Task<AuthResponse> IssueForUserAsync(AppUser user, string? auditAction, string? familyId, CancellationToken ct, Action<string>? beforeSave = null)
    {
        user.LastLoginAt = DateTime.UtcNow;
        var (subject, profile) = await BuildUserSessionAsync(user, ct);
        if (auditAction != null) await AuditAsync(auditAction, subject.TenantId, (PrincipalType.User, user.Id), null, ct);
        return await IssueAsync(subject, profile, familyId, ct, beforeSave);
    }

    private async Task<(TokenSubject subject, SessionProfile profile)> BuildUserSessionAsync(AppUser user, CancellationToken ct)
    {
        int? tenantId = null; TenantRole? role = null; Tenant? tenant = null;
        if (user.UserType == UserType.TenantUser)
        {
            var m = await db.TenantMembers.IgnoreQueryFilters().Include(x => x.Tenant)
                .Where(x => x.UserId == user.Id && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Role).FirstOrDefaultAsync(ct)
                ?? throw new AppException("Your account is not linked to any Moi vendor, or your access was removed.", 403);
            tenantId = m.TenantId; role = m.Role; tenant = m.Tenant;
            if (tenant!.Status == TenantStatus.Suspended) throw new AppException("This vendor account is suspended. Please contact OneMoi support.", 403);
        }

        var isHost = user.UserType == UserType.Individual && user.Mobile != null &&
                     await db.Functions.IgnoreQueryFilters().AnyAsync(f => f.OwnerMobile == user.Mobile && !f.IsDeleted, ct);

        var home = user.UserType switch { UserType.SuperAdmin => "/admin/dashboard", UserType.TenantUser => "/vendor/dashboard", _ => "/me/moi" };
        var roleKey = RolePermissions.RoleKey(PrincipalType.User, user.UserType, role);
        var subject = new TokenSubject(PrincipalType.User, user.Id, user.FullName, user.UserType, tenantId, role, user.PersonId, user.Mobile, user.SecurityStamp);
        var profile = new SessionProfile("User", user.Id, user.FullName, user.FullNameTa, user.UserType.ToString(), role?.ToString(),
            tenantId, tenant?.Code, tenant?.Name, tenant?.LogoPath, tenant?.Status.ToString(), user.PersonId, user.Mobile, user.Email, isHost, home,
            user.PasswordHash != null, RolePermissions.For(roleKey).OrderBy(p => p).ToList());
        return (subject, profile);
    }

    private static TokenSubject OperatorSubject(Operator op) =>
        new(PrincipalType.Operator, op.Id, op.Name, null, op.TenantId, null, null, op.Mobile, op.SecurityStamp);

    private static SessionProfile OperatorProfile(Operator op, Tenant t) =>
        new("Operator", op.Id, op.Name, op.NameTa, null, "Operator", t.Id, t.Code, t.Name, t.LogoPath, t.Status.ToString(), null, op.Mobile, null,
            false, "/operator/home", false, RolePermissions.For(RolePermissions.Operator).OrderBy(p => p).ToList());

    private async Task<AuthResponse> IssueAsync(TokenSubject subject, SessionProfile profile, string? familyId, CancellationToken ct, Action<string>? beforeSave = null)
    {
        var (access, exp) = tokens.CreateAccessToken(subject);
        var refresh = tokens.CreateRefreshToken();
        var refreshHash = tokens.HashToken(refresh);
        db.RefreshTokens.Add(new RefreshToken
        {
            PrincipalType = subject.PrincipalType, PrincipalId = subject.PrincipalId, TokenHash = refreshHash,
            FamilyId = familyId ?? Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(settings.RefreshTokenDays), CreatedByIp = current.IpAddress
        });
        beforeSave?.Invoke(refreshHash);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(access, exp, refresh, profile);
    }

    private static void Rotate(RefreshToken old, string newHash)
    {
        old.RevokedAt = DateTime.UtcNow;
        old.RevokedReason = "rotated";
        old.ReplacedByHash = newHash;
    }

    private async Task RevokeAllTokensAsync(PrincipalType type, int id, string reason, CancellationToken ct)
    {
        var all = await db.RefreshTokens.Where(r => r.PrincipalType == type && r.PrincipalId == id && r.RevokedAt == null).ToListAsync(ct);
        all.ForEach(r => { r.RevokedAt = DateTime.UtcNow; r.RevokedReason = reason; });
        await db.SaveChangesAsync(ct);
    }

    private void SetPassword(AppUser user, string password)
    {
        user.PasswordHash = hasher.Hash(password);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.SecurityStamp = NewStamp();                // existing access tokens stop working
        if (user.Id != 0) sessions.InvalidatePrincipal(PrincipalType.User, user.Id);
    }

    /// <summary>Password rule: 8+ characters with at least one letter and one number.</summary>
    public static void ValidatePassword(string? password, string field)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8 || !LetterRegex().IsMatch(password) || !DigitRegex().IsMatch(password))
            throw new ValidationException(field, "Password must have at least 8 characters, with letters and numbers.");
    }

    private async Task<AppUser?> FindPasswordUserAsync(string? loginInput, CancellationToken ct)
    {
        var login = (loginInput ?? "").Trim().ToLowerInvariant();
        if (login.Length == 0) return null;
        var mobile = Helpers.NormalizeMobile(login);
        return await db.Users.FirstOrDefaultAsync(u => u.PasswordHash != null && u.UserType != UserType.Individual &&
                                                        (u.Email == login || (mobile != null && u.Mobile == mobile)), ct);
    }

    /// <summary>Reset code goes to the same channel the user typed (e-mail → e-mail, mobile → SMS).</summary>
    private static (OtpChannel, string?) ResetDestination(string? login, AppUser? user)
    {
        if (user == null) return (OtpChannel.Sms, null);
        return Helpers.IsEmail(login) ? (OtpChannel.Email, user.Email) : (OtpChannel.Sms, user.Mobile);
    }

    private async Task<SendOtpResponse> IssueOtpAsync(OtpChannel channel, string destination, OtpPurpose purpose, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-1);
        if (await db.OtpRequests.CountAsync(o => o.Destination == destination && o.CreatedAt > since, ct) >= settings.OtpMaxPerHour)
            throw new AppException("Too many OTP requests. Please try again after some time.", 429);

        var code = Helpers.RandomDigits(6);
        db.OtpRequests.Add(new OtpRequest
        {
            Channel = channel, Destination = destination, Purpose = purpose, CodeHash = HashOtp(destination, code),
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(settings.OtpExpiryMinutes), IpAddress = current.IpAddress
        });
        await db.SaveChangesAsync(ct);

        var what = purpose == OtpPurpose.ResetPassword ? "password reset code" : "verification code";
        var text = $"{code} is your OneMoi {what}. Valid for {settings.OtpExpiryMinutes} minutes. Do not share it with anyone.";
        await notifier.SendAsync(channel, destination, channel == OtpChannel.Email ? $"Your OneMoi {what}" : null, text, text.Replace(code, "******"), null, ct);

        return new SendOtpResponse(Mask(destination), channel.ToString(), settings.OtpExpiryMinutes * 60, 30,
            settings.ExposeOtpInResponse ? code : null);
    }

    private async Task ConsumeOtpAsync(string destination, OtpPurpose purpose, string? code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var otp = await db.OtpRequests
            .Where(o => o.Destination == destination && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > now)
            .OrderByDescending(o => o.Id).FirstOrDefaultAsync(ct)
            ?? throw new AppException("This code has expired. Please request a new OTP.", 400);

        if (otp.Attempts >= otp.MaxAttempts) throw new AppException("Too many wrong attempts. Please request a new OTP.", 429);

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(otp.CodeHash), Encoding.UTF8.GetBytes(HashOtp(destination, (code ?? "").Trim()))))
        {
            otp.Attempts++;
            await db.SaveChangesAsync(ct);
            var left = otp.MaxAttempts - otp.Attempts;
            throw new AppException(left > 0 ? $"Incorrect code. {left} attempt(s) left." : "Too many wrong attempts. Please request a new OTP.", 400);
        }
        otp.IsUsed = true;
        otp.UsedAt = now;
    }

    private async Task<AppUser> CreateIndividualAsync(string mobile, string? name, string? nameTa, string? email, string? city, string? work, CancellationToken ct)
    {
        var user = new AppUser { FullName = name ?? "OneMoi user", FullNameTa = nameTa, Mobile = mobile, Email = email, UserType = UserType.Individual };
        db.Users.Add(user);
        var person = await db.Persons.FirstOrDefaultAsync(p => p.Mobile == mobile, ct);
        if (person == null)
        {
            person = new Person { Mobile = mobile, Name = name ?? "", NameTa = nameTa, City = city, Work = work };
            db.Persons.Add(person);
        }
        user.Person = person;
        await db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>On first verified login, the person record created by vendors becomes "owned" by this user.</summary>
    private async Task ClaimPersonAsync(AppUser user, CancellationToken ct)
    {
        var person = user.PersonId != null
            ? await db.Persons.FirstOrDefaultAsync(p => p.Id == user.PersonId, ct)
            : await db.Persons.FirstOrDefaultAsync(p => p.Mobile == user.Mobile, ct);
        if (person == null) return;
        user.PersonId = person.Id;
        person.IsVerified = true;
        if (user.FullName == "OneMoi user" && !string.IsNullOrEmpty(person.Name))
        {
            user.FullName = (person.Initial != null ? person.Initial + " " : "") + person.Name;
            user.FullNameTa ??= person.NameTa;
        }
    }

    private async Task<string> UniqueTenantCodeAsync(string businessName, CancellationToken ct)
    {
        var baseCode = new string(businessName.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(6).ToArray());
        if (baseCode.Length < 3) baseCode = "MOI" + baseCode;
        var code = baseCode;
        for (var i = 2; await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Code == code, ct); i++) code = baseCode + i;
        return code;
    }

    private async Task AuditAsync(string action, int? tenantId, (PrincipalType type, int id)? who, string? details, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId, PrincipalType = who?.type, PrincipalId = who?.id, Action = action,
            Details = details, IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private string HashOtp(string destination, string code)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(settings.OtpSecret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes($"{destination}:{code}")));
    }

    private static (OtpChannel, string) ParseDestination(string input)
    {
        if (Helpers.IsEmail(input)) return (OtpChannel.Email, input.Trim().ToLowerInvariant());
        var mobile = Helpers.NormalizeMobile(input) ?? throw new ValidationException("destination", "Enter a valid 10-digit mobile number or e-mail");
        return (OtpChannel.Sms, mobile);
    }

    private static OtpPurpose ParsePurpose(string? p) => (p ?? "login").ToLowerInvariant() switch
    {
        "register" => OtpPurpose.Register,
        "reset" => OtpPurpose.ResetPassword,
        _ => OtpPurpose.Login
    };

    private static string Mask(string destination)
    {
        if (Helpers.IsEmail(destination))
        {
            var at = destination.IndexOf('@');
            return at <= 2 ? destination : destination[..2] + new string('•', Math.Max(1, at - 2)) + destination[at..];
        }
        var m = Helpers.NormalizeMobile(destination);
        return m == null ? "your registered mobile" : "+91 " + Helpers.MaskMobile(m);
    }

    private static string NewStamp() => Guid.NewGuid().ToString("N");
    private static DateTime ToIst(DateTime utc) => utc.AddHours(5.5);

    [GeneratedRegex("[A-Za-z]")] private static partial Regex LetterRegex();
    [GeneratedRegex("[0-9]")] private static partial Regex DigitRegex();
}
