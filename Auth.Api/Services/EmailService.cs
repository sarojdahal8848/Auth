using Auth.Api.Dtos;
using Auth.Api.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Auth.Api.Services;

public class EmailService(IConfiguration config) : IEmailService
{
    public bool SendEmail(EmailRequestDto request)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(config["EmailSettings:MailSenderName"], config["EmailSettings:MailFrom"]));
        email.To.Add(new MailboxAddress(request.Name, request.To));
        email.Subject = request.Subject;
        email.Body = new TextPart(TextFormat.Html) { Text = request.Body };

        try
        {
            using var smtp = new SmtpClient();
            smtp.Connect(config["EmailSettings:MailServer"], 587, SecureSocketOptions.StartTls);
            smtp.Authenticate(config["EmailSettings:MailFrom"], config["EmailSettings:MailSenderPassword"]);
            smtp.Send(email);
            smtp.Disconnect(true);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}