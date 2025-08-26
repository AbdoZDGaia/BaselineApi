namespace Sql.Baseline.Api.Infrastructure.Sms;

public interface ITwilioSmsSender { Task SendAsync(string to, string body, CancellationToken ct = default); }

