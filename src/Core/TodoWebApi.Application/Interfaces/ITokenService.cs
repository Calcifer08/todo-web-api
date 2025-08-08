using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(ApiUser user);
}