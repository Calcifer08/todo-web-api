using Microsoft.AspNetCore.Identity;

namespace TodoWebApi.Domain.Entities;

public class ApiUser : IdentityUser
{
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}