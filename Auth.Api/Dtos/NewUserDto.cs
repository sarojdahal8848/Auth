namespace Auth.Api.Dtos;

public class NewUserDto
{
    public string Email { get; set; } = String.Empty;
    public string Token { get; set; } = String.Empty;
}