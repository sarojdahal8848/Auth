using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }  
    [Required]
    public string? Password { get; set; }
}