using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public class RefreshTokenRequestDto
{
    [Required]
    public string? RefreshToken { get; set; }
}