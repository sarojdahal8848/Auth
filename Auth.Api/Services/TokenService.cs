using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Api.Db;
using Auth.Api.Dtos;
using Auth.Api.Interfaces;
using Auth.Api.Models;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Auth.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
    }

    public List<Claim> GetUserClaims(AppUser user)
    {
        return
        [
            new Claim("Id", user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
        ];
    }

    public string GenerateAccessToken(List<Claim> claims)
    {
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(1),
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(AppUser user)
    {
        var existingToken = _context.RefreshTokens.FirstOrDefault(x => x.UserId == user.Id);
        if (existingToken != null)
        {
            existingToken.Token = Guid.NewGuid().ToString();
            existingToken.IsUsed = true;
            _context.RefreshTokens.Update(existingToken);
            await _context.SaveChangesAsync();
            return existingToken.Token;
        }
        var refreshToken = new RefreshToken()
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiryDate = DateTime.Now.AddMinutes(3),
            IsUsed = false,
            IsRevoked = false,
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    public async Task<TokenDto> CreateToken(AppUser user)
    {
        var claims = GetUserClaims(user);

        var accessToken = GenerateAccessToken(claims);
        var refreshToken = await GenerateRefreshToken(user);

        return new TokenDto()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }
}