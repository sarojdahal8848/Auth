namespace Auth.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? Token { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.Now;
    public DateTime ExpiryDate { get; set; }
}