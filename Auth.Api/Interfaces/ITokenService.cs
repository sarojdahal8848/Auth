using System.Security.Claims;
using Auth.Api.Dtos;
using Auth.Api.Models;

namespace Auth.Api.Interfaces;

public interface ITokenService
{
    List<Claim> GetUserClaims(AppUser user);
    string GenerateAccessToken(List<Claim> claims);
    Task<string> GenerateRefreshToken(AppUser user);
    Task<TokenDto> CreateToken(AppUser user);
    
}