namespace Sql.Baseline.Api.Infrastructure.Email;

public class SmtpEmailSender : ISmtpEmailSender
{
    private readonly IConfiguration _cfg;
    public SmtpEmailSender(IConfiguration cfg) => _cfg = cfg;
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var msg = new MimeKit.MimeMessage();
        msg.From.Add(MimeKit.MailboxAddress.Parse(_cfg["Email:From"]!));
        msg.To.Add(MimeKit.MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new MimeKit.BodyBuilder { HtmlBody = body }.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(_cfg["Email:SmtpHost"], int.Parse(_cfg["Email:Port"]!), MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_cfg["Email:User"], _cfg["Email:Password"], ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
    }
}
