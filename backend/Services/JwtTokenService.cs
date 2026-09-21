using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "MilkHisab";
    public string Audience { get; set; } = "MilkHisab.Spa";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}

public interface IJwtTokenService
{
    (string accessToken, DateTimeOffset accessExpires) CreateAccessToken(LedgerUser user);
    string CreateRefreshToken(LedgerUser user);
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _opt = options.Value;

    public (string accessToken, DateTimeOffset accessExpires) CreateAccessToken(LedgerUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(Math.Max(5, _opt.AccessTokenMinutes));
        return (CreateToken(user, expires.UtcDateTime, "access"), expires);
    }

    public string CreateRefreshToken(LedgerUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddDays(Math.Max(1, _opt.RefreshTokenDays)).UtcDateTime;
        return CreateToken(user, expires, "refresh");
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(refreshToken, CreateValidationParameters(_opt), out var securityToken);
            if (securityToken is not JwtSecurityToken jwt ||
                !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                return null;
            if (!string.Equals(principal.FindFirst("token_use")?.Value, "refresh", StringComparison.Ordinal))
                return null;
            return principal;
        }
        catch { return null; }
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions opt)
    {
        if (string.IsNullOrWhiteSpace(opt.Key) || opt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be set (at least 32 characters) in appsettings.Secrets.json.");

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opt.Issuer,
            ValidateAudience = true,
            ValidAudience = opt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }

    private string CreateToken(LedgerUser user, DateTime expiresUtc, string tokenUse)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Name),
            new Claim("token_use", tokenUse),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: expiresUtc,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}