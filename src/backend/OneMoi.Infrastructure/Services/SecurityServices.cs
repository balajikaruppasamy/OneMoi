using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneMoi.Application.Common;

namespace OneMoi.Infrastructure.Services;

/// <summary>BCrypt for passwords and operator PINs.</summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string secret) => BCrypt.Net.BCrypt.HashPassword(secret, workFactor: 11);
    public bool Verify(string secret, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(secret, hash); } catch { return false; }
    }
}

/// <summary>Custom claim names placed inside the JWT.</summary>
public static class OneMoiClaims
{
    public const string PrincipalType = "ptype";
    public const string UserType = "utype";
    public const string TenantId = "tid";
    public const string TenantRole = "trole";
    public const string PersonId = "pid";
    public const string Mobile = "mobile";
    public const string SecurityStamp = "sstamp";   // compared with the database on every request
}

public class JwtTokenService(IConfiguration config) : ITokenService
{
    public (string token, DateTime expiresAt) CreateAccessToken(TokenSubject s)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:AccessTokenMinutes"] ?? "120"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, s.PrincipalId.ToString()),
            new(JwtRegisteredClaimNames.Name, s.Name),
            new(OneMoiClaims.PrincipalType, s.PrincipalType.ToString()),
            new(OneMoiClaims.SecurityStamp, s.SecurityStamp)
        };
        if (s.UserType != null) claims.Add(new(OneMoiClaims.UserType, s.UserType.ToString()!));
        if (s.TenantId != null) claims.Add(new(OneMoiClaims.TenantId, s.TenantId.ToString()!));
        if (s.TenantRole != null) claims.Add(new(OneMoiClaims.TenantRole, s.TenantRole.ToString()!));
        if (s.PersonId != null) claims.Add(new(OneMoiClaims.PersonId, s.PersonId.ToString()!));
        if (s.Mobile != null) claims.Add(new(OneMoiClaims.Mobile, s.Mobile));
        // Role claim drives [Authorize(Roles = ...)] / policies
        claims.Add(new(ClaimTypes.Role, s.PrincipalType == Domain.Enums.PrincipalType.Operator ? "Operator" : s.UserType.ToString()!));

        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, DateTime.UtcNow, expires,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
