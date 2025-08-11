using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Services;

public class TokenService : ITokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenLifetimeMinutes;
    private readonly int _refreshTokenLifetimeDays;

    public TokenService(IConfiguration config)
    {
        _issuer = config["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer не настроен.");
        _audience = config["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience не настроен.");
        var keyString = config["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key не настроен.");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

        if (!int.TryParse(config["JWT:AccessTokenLifetimeMinutes"], out _accessTokenLifetimeMinutes))
        {
            _accessTokenLifetimeMinutes = 15;
        }

        if (!int.TryParse(config["JWT:RefreshTokenLifetimeDays"], out _refreshTokenLifetimeDays))
        {
            _refreshTokenLifetimeDays = 7;
        }
    }

    public string CreateAccessToken(ApiUser user)
    {
        var claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!)
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string, int) CreateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return (Convert.ToBase64String(randomNumber), _refreshTokenLifetimeDays);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = false,
            ValidIssuer = _issuer,
            ValidAudience = _audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Недопустимый токен");
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}