namespace TodoWebApi.Application.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}