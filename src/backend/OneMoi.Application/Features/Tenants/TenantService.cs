using Microsoft.EntityFrameworkCore;
using OneMoi.Application.Common;
using OneMoi.Application.Common.Security;
using OneMoi.Application.Features.Auth;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Application.Features.Tenants;

public record TenantProfileDto(
    int Id, string Code, string Name, string? NameTa, string? Tagline, string OwnerName, string? Email, string Mobile,
    string? AltMobile, string? AddressLine, string? City, string? District, string State, string? Pincode, string? Gstin,
    string? LogoPath, string? PrimaryColor, string? ReceiptHeader, string? ReceiptFooter, string Status, string? PlanName, DateTime? TrialEndsAt);

public record UpdateTenantProfileRequest(
    string Name, string? NameTa, string? Tagline, string OwnerName, string? Email, string Mobile, string? AltMobile,
    string? AddressLine, string? City, string? District, string? State, string? Pincode, string? Gstin,
    string? PrimaryColor, string? ReceiptHeader, string? ReceiptFooter);

public record TenantMemberDto(int Id, int UserId, string Name, string? Mobile, string? Email, string Role, bool IsActive, DateTime? LastLoginAt, bool IsLocked, bool IsYou);

public record AddTenantMemberRequest(string Name, string Mobile, string? Email, string Role, string Password);

/// <summary>Change a staff login's role and/or enable/disable it.</summary>
public record UpdateTenantMemberRequest(string Role, bool IsActive);

public record ResetMemberPasswordRequest(string NewPassword);

public record RolePermissionsDto(string Role, IReadOnlyCollection<string> Permissions);

/// <summary>Vendor company profile, branding (logo) and staff logins (owner manages roles).</summary>
public class TenantService(IAppDbContext db, ICurrentUser current, IFileStorage files, IPasswordHasher hasher, ISessionStateStore sessions)
{
    private int TenantId => current.TenantId ?? throw new ForbiddenException();

    public async Task<TenantProfileDto> GetProfileAsync(CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantProfileView);
        return ToDto(await db.Tenants.Include(t => t.Plan).FirstOrDefaultAsync(t => t.Id == TenantId, ct) ?? throw new NotFoundException("Vendor"));
    }

    public async Task<TenantProfileDto> UpdateProfileAsync(UpdateTenantProfileRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantProfileManage, "Only the vendor owner can change company details.");
        var mobile = Helpers.NormalizeMobile(r.Mobile);
        new Validator()
            .Require("name", r.Name, "Enter the company name")
            .Require("ownerName", r.OwnerName, "Enter the owner name")
            .Check(mobile != null, "mobile", "Enter a valid mobile number")
            .Check(string.IsNullOrWhiteSpace(r.Email) || Helpers.IsEmail(r.Email), "email", "Enter a valid e-mail")
            .Check(string.IsNullOrWhiteSpace(r.Gstin) || r.Gstin.Trim().Length == 15, "gstin", "GSTIN must have 15 characters")
            .ThrowIfInvalid();

        var t = await db.Tenants.Include(x => x.Plan).FirstAsync(x => x.Id == TenantId, ct);
        t.Name = r.Name.Trim(); t.NameTa = Helpers.Clean(r.NameTa); t.Tagline = Helpers.Clean(r.Tagline);
        t.OwnerName = r.OwnerName.Trim(); t.Email = Helpers.Clean(r.Email)?.ToLower(); t.Mobile = mobile!;
        t.AltMobile = Helpers.NormalizeMobile(r.AltMobile); t.AddressLine = Helpers.Clean(r.AddressLine);
        t.City = Helpers.Clean(r.City); t.District = Helpers.Clean(r.District); t.State = Helpers.Clean(r.State) ?? "Tamil Nadu";
        t.Pincode = Helpers.Clean(r.Pincode); t.Gstin = Helpers.Clean(r.Gstin)?.ToUpper();
        t.PrimaryColor = Helpers.Clean(r.PrimaryColor); t.ReceiptHeader = Helpers.Clean(r.ReceiptHeader); t.ReceiptFooter = Helpers.Clean(r.ReceiptFooter);
        await db.SaveChangesAsync(ct);
        return ToDto(t);
    }

    public async Task<string> UploadLogoAsync(Stream content, string fileName, long length, CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantProfileManage, "Only the vendor owner can change the logo.");
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) throw new ValidationException("logo", "Upload a PNG, JPG or WEBP image");
        if (length > 2 * 1024 * 1024) throw new ValidationException("logo", "Logo must be smaller than 2 MB");

        var url = await files.SaveAsync(content, $"tenants/{TenantId}", $"logo-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}", ct);
        var t = await db.Tenants.FirstAsync(x => x.Id == TenantId, ct);
        t.LogoPath = url;
        await db.SaveChangesAsync(ct);
        return url;
    }

    // ───────────────────────────── Staff logins ─────────────────────────────

    public async Task<List<TenantMemberDto>> MembersAsync(CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantStaffView);
        var now = DateTime.UtcNow;
        return await db.TenantMembers.Include(m => m.User).OrderBy(m => m.Role).ThenBy(m => m.User!.FullName)
            .Select(m => new TenantMemberDto(m.Id, m.UserId, m.User!.FullName, m.User.Mobile, m.User.Email, m.Role.ToString(),
                m.IsActive && m.User.IsActive, m.User.LastLoginAt, m.User.LockoutUntil != null && m.User.LockoutUntil > now, m.UserId == current.UserId))
            .ToListAsync(ct);
    }

    public async Task<TenantMemberDto> AddMemberAsync(AddTenantMemberRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantStaffManage, "Only the vendor owner can add staff logins.");
        var mobile = Helpers.NormalizeMobile(r.Mobile);
        var role = ParseRole(r.Role);
        new Validator()
            .Require("name", r.Name, "Enter a name")
            .Check(mobile != null, "mobile", "Enter a valid mobile number")
            .Check(string.IsNullOrWhiteSpace(r.Email) || Helpers.IsEmail(r.Email), "email", "Enter a valid e-mail")
            .ThrowIfInvalid();
        AuthService.ValidatePassword(r.Password, "password");
        if (await db.Users.AnyAsync(u => u.Mobile == mobile && u.UserType == UserType.TenantUser, ct))
            throw new ValidationException("mobile", "A vendor login already exists for this mobile.");
        var email = Helpers.Clean(r.Email)?.ToLower();
        if (email != null && await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ValidationException("email", "This e-mail is already used by another account.");

        var user = new AppUser { FullName = r.Name.Trim(), Mobile = mobile, Email = email, PasswordHash = hasher.Hash(r.Password), UserType = UserType.TenantUser, PasswordChangedAt = DateTime.UtcNow };
        var m = new TenantMember { TenantId = TenantId, User = user, Role = role };
        db.Users.Add(user); db.TenantMembers.Add(m);
        await AuditAsync("STAFF_ADDED", $"{user.FullName} as {role}", ct);
        return new TenantMemberDto(m.Id, user.Id, user.FullName, user.Mobile, user.Email, role.ToString(), true, null, false, false);
    }

    /// <summary>Owner changes a staff member's role or disables the login. Takes effect on their next request.</summary>
    public async Task UpdateMemberAsync(int memberId, UpdateTenantMemberRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantStaffManage, "Only the vendor owner can change staff access.");
        var role = ParseRole(r.Role);
        var m = await db.TenantMembers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == memberId, ct) ?? throw new NotFoundException("Staff login");
        if (m.UserId == current.UserId) throw new AppException("You cannot change your own role or disable yourself.");

        var removingOwner = m.Role == TenantRole.Owner && m.IsActive && (role != TenantRole.Owner || !r.IsActive);
        if (removingOwner && await db.TenantMembers.CountAsync(x => x.Role == TenantRole.Owner && x.IsActive, ct) <= 1)
            throw new AppException("A vendor must always have at least one active owner.");

        var changed = m.Role != role || m.IsActive != r.IsActive;
        m.Role = role;
        m.IsActive = r.IsActive;
        if (changed) m.User!.SecurityStamp = Guid.NewGuid().ToString("N");     // their current session ends
        await AuditAsync("STAFF_CHANGED", $"{m.User!.FullName}: {role}, {(r.IsActive ? "active" : "disabled")}", ct);
        sessions.InvalidatePrincipal(PrincipalType.User, m.UserId);
    }

    /// <summary>Owner sets a new password for a staff member (also unlocks the account).</summary>
    public async Task ResetMemberPasswordAsync(int memberId, ResetMemberPasswordRequest r, CancellationToken ct = default)
    {
        current.Demand(Permissions.TenantStaffManage, "Only the vendor owner can reset staff passwords.");
        AuthService.ValidatePassword(r.NewPassword, "newPassword");
        var m = await db.TenantMembers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == memberId, ct) ?? throw new NotFoundException("Staff login");
        var u = m.User!;
        u.PasswordHash = hasher.Hash(r.NewPassword);
        u.PasswordChangedAt = DateTime.UtcNow;
        u.FailedLoginCount = 0;
        u.LockoutUntil = null;
        u.SecurityStamp = Guid.NewGuid().ToString("N");
        var tokens = await db.RefreshTokens.Where(t => t.PrincipalType == PrincipalType.User && t.PrincipalId == u.Id && t.RevokedAt == null).ToListAsync(ct);
        tokens.ForEach(t => { t.RevokedAt = DateTime.UtcNow; t.RevokedReason = "password-reset-by-owner"; });
        await AuditAsync("STAFF_PASSWORD_RESET", u.FullName, ct);
        sessions.InvalidatePrincipal(PrincipalType.User, u.Id);
    }

    /// <summary>Read-only view of the role → permission matrix (for the "Roles & access" screen).</summary>
    public List<RolePermissionsDto> RoleMatrix()
    {
        current.Demand(Permissions.TenantStaffView);
        return new[] { RolePermissions.Owner, RolePermissions.Manager, RolePermissions.Accountant, RolePermissions.Operator }
            .Select(r => new RolePermissionsDto(r, RolePermissions.For(r).OrderBy(p => p).ToList())).ToList();
    }

    private static TenantRole ParseRole(string? role) =>
        Enum.TryParse<TenantRole>(role, true, out var r) && Enum.IsDefined(r) ? r : throw new ValidationException("role", "Choose Owner, Manager or Accountant");

    private async Task AuditAsync(string action, string details, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { TenantId = TenantId, PrincipalType = PrincipalType.User, PrincipalId = current.UserId, Action = action,
            Details = details, IpAddress = current.IpAddress, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public static TenantProfileDto ToDto(Tenant t) => new(
        t.Id, t.Code, t.Name, t.NameTa, t.Tagline, t.OwnerName, t.Email, t.Mobile, t.AltMobile, t.AddressLine, t.City, t.District,
        t.State, t.Pincode, t.Gstin, t.LogoPath, t.PrimaryColor, t.ReceiptHeader, t.ReceiptFooter, t.Status.ToString(), t.Plan?.Name, t.TrialEndsAt);
}
