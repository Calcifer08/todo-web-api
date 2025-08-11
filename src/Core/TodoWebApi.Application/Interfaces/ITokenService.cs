using System.Security.Claims;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(ApiUser user);
    (string, int) CreateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token);
}