using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApiUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApiUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var user = new ApiUser() { UserName = registerDto.Email, Email = registerDto.Email };
        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var accessToken = _tokenService.CreateAccessToken(user);
        (string refreshToken, int lifetime) = _tokenService.CreateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(lifetime);
        await _userManager.UpdateAsync(user);

        var responseDto = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return Ok(responseDto);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            return Unauthorized();
        }

        var accessToken = _tokenService.CreateAccessToken(user);
        (string refreshToken, int lifetime) = _tokenService.CreateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(lifetime);
        await _userManager.UpdateAsync(user);

        var responseDto = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return Ok(responseDto);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        user.RefreshToken = null;

        await _userManager.UpdateAsync(user);

        return Ok(new { Message = "Успешный выход" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        if (refreshTokenDto is null)
        {
            return BadRequest("Неверный запрос клиента");
        }

        var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            return BadRequest("Недопустимый токен доступа");
        }

        var userId = userIdClaim.Value;

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.RefreshToken != refreshTokenDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return BadRequest("Недопустимый токен доступа или обновления");
        }

        var newAccessToken = _tokenService.CreateAccessToken(user);
        (string newRefreshToken, int lifetime) = _tokenService.CreateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(lifetime);
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
}