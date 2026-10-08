using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OneMoi.Application.Common;
using OneMoi.Domain.Enums;
using OneMoi.Infrastructure.Services;

namespace OneMoi.Api.Infrastructure;

/// <summary>Reads the logged-in principal from the JWT claims of the current request.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    private string? Claim(string type) => User?.FindFirst(type)?.Value;
    private int? IntClaim(string type) => int.TryParse(Claim(type), out var v) ? v : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public PrincipalType? PrincipalType => Enum.TryParse<PrincipalType>(Claim(OneMoiClaims.PrincipalType), out var p) ? p : null;
    public int? UserId => PrincipalType == Domain.Enums.PrincipalType.User ? IntClaim(JwtRegisteredClaimNames.Sub) ?? IntClaim(ClaimTypes.NameIdentifier) : null;
    public int? OperatorId => PrincipalType == Domain.Enums.PrincipalType.Operator ? IntClaim(JwtRegisteredClaimNames.Sub) ?? IntClaim(ClaimTypes.NameIdentifier) : null;
    public int? TenantId => IntClaim(OneMoiClaims.TenantId);
    public UserType? UserType => Enum.TryParse<UserType>(Claim(OneMoiClaims.UserType), out var u) ? u : null;
    public TenantRole? TenantRole => Enum.TryParse<TenantRole>(Claim(OneMoiClaims.TenantRole), out var r) ? r : null;
    public int? PersonId => IntClaim(OneMoiClaims.PersonId);
    public string? Mobile => Claim(OneMoiClaims.Mobile);
    public string? Name => Claim(JwtRegisteredClaimNames.Name) ?? Claim(ClaimTypes.Name);
    public string? SecurityStamp => Claim(OneMoiClaims.SecurityStamp);
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
