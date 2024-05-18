using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public class LoginDto
{
    [Required]
    public string Email { get; set; } = String.Empty;
    [Required]
    public string Password { get; set; } = String.Empty;
}