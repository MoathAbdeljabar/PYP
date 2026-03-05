namespace Identity.Application.Identity.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}