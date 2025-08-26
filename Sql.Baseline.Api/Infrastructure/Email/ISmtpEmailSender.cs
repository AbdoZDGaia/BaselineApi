namespace Sql.Baseline.Api.Infrastructure.Email;

public interface ISmtpEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

