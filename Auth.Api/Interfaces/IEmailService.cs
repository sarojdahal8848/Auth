using Auth.Api.Dtos;

namespace Auth.Api.Interfaces;

public interface IEmailService
{
    bool SendEmail(EmailRequestDto request);
}