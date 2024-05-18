namespace Auth.Api.Dtos;

public class EmailRequestDto
{
    public string To { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
    public string Subject { get; set; } = String.Empty;
    public string Body { get; set; } = String.Empty;
}