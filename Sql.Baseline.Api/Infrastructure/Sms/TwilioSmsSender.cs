using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;


namespace Sql.Baseline.Api.Infrastructure.Sms;
public class TwilioSmsSender : ITwilioSmsSender
{
    private readonly string? _from;
    public TwilioSmsSender(IConfiguration cfg)
    {
        TwilioClient.Init(cfg["Twilio:AccountSid"], cfg["Twilio:AuthToken"]);
        _from = cfg["Twilio:FromNumber"];
    }

    public async Task SendAsync(string to, string body, CancellationToken ct = default)
    {
        var options = new CreateMessageOptions(new PhoneNumber(to))
        {
            From = new PhoneNumber(_from),
            Body = body
        };

        // Most Twilio SDK versions:
        await MessageResource.CreateAsync(options);

        // If we upgrade to a version that supports CancellationToken:
        // await MessageResource.CreateAsync(options, ct);
    }
}
