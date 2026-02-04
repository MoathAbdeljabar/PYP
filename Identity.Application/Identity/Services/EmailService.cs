using Identity.Application.Identity.Interfaces;
using Identity.Application.Identity.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using static System.Net.Mime.MediaTypeNames;

namespace Identity.Application.Identity.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
     //HTML Email = Formatted, styled emails(colors, fonts, layouts, buttons) 
     //Text Email = Plain text only(no formatting)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
            bodyBuilder.HtmlBody = body;
        else
            bodyBuilder.TextBody = body;

        email.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            /*
             SMTP Client is the software component that:
                Connects to an email server (like Gmail, Outlook, etc.)
                Handshakes with the server (authentication)
                Delivers your email to the server
                Disconnects when done
             */

            //await smtp.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);
            await smtp.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex) { 
        Console.WriteLine(ex.ToString());
            return false;
        }
        
    }
}