using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bevera.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;

    public SmtpEmailSender(IOptions<SmtpOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlMessage
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_opt.Host, _opt.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_opt.Username, _opt.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
