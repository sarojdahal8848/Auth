using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public class TokenRequestDto
{
    [Required]
    public string? AccessToken { get; set; }
    [Required]
    public string? RefreshToken { get; set; }
}