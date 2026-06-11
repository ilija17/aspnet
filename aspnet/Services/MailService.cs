using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace aspnet.Services;

public class MailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailService> _logger;

    public MailService(IConfiguration config, ILogger<MailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendWaitlistConfirmationAsync(string toEmail, int position)
    {
        var host = _config["Smtp:Host"];
        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(from))
        {
            _logger.LogWarning(
                "SMTP config incomplete (Smtp:Host/User/Password/From) — skipping waitlist mail to {Email}", toEmail);
            return;
        }

        var port = int.Parse(_config["Smtp:Port"] ?? "465");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("AI-Native Casino OS", from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"✨ You're #{position:N0} on the waitlist — your TAM has been notified";

        var body = new BodyBuilder
        {
            TextBody =
                $"You're on the waitlist.\n\n" +
                $"Position: #{position:N0}\n\n" +
                "We onboard one operator per epoch. When a spot opens up at the table, " +
                "an agent (possibly human) will reach out.\n\n" +
                "Until then: stay liquid.\n\n" +
                "— AI-Native Casino OS\n" +
                "Agentic gaming at frontier scale",
            HtmlBody = BuildHtmlBody(position)
        };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        // Port 465 = implicitni TLS od prve sekunde (Gmail "TLS always")
        await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Waitlist confirmation sent to {Email} (position {Position})", toEmail, position);
    }

    // Inline stilovi i table layout — email klijenti ne podržavaju moderni CSS
    private static string BuildHtmlBody(int position) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>You're on the waitlist</title>
        </head>
        <body style="margin:0;padding:0;background-color:#0a0c14;font-family:Georgia,'Times New Roman',serif;">
            <div style="display:none;max-height:0;overflow:hidden;">
                You're #{{position:N0}} on the waitlist. We onboard one operator per epoch.
            </div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#0a0c14;padding:32px 12px;">
                <tr><td align="center">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">

                        <!-- Gold top rule -->
                        <tr><td style="height:3px;background:linear-gradient(90deg,#0a0c14,#e8c468,#0a0c14);background-color:#e8c468;border-radius:3px;"></td></tr>

                        <!-- Header -->
                        <tr><td align="center" style="padding:40px 24px 8px;">
                            <div style="font-family:Verdana,Arial,sans-serif;font-size:11px;letter-spacing:4px;text-transform:uppercase;color:#e8c468;">
                                ✦ &nbsp;AI-Native Casino OS&nbsp; ✦
                            </div>
                        </td></tr>
                        <tr><td align="center" style="padding:8px 24px 0;">
                            <h1 style="margin:0;font-size:34px;line-height:1.2;color:#f5efe2;font-weight:normal;">
                                You're on the list.
                            </h1>
                            <p style="margin:10px 0 0;font-size:15px;color:#8a8fa3;font-style:italic;">
                                The velvet rope acknowledges you.
                            </p>
                        </td></tr>

                        <!-- Position card -->
                        <tr><td style="padding:32px 24px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                                   style="background-color:#11141f;border:1px solid #2a2f42;border-radius:14px;">
                                <tr><td align="center" style="padding:36px 24px;">
                                    <div style="font-family:Verdana,Arial,sans-serif;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#8a8fa3;">
                                        Your position
                                    </div>
                                    <div style="font-size:56px;line-height:1.1;color:#e8c468;padding:12px 0 4px;">
                                        #{{position:N0}}
                                    </div>
                                    <div style="font-size:13px;color:#8a8fa3;">
                                        of a waitlist we describe as &ldquo;oversubscribed&rdquo;
                                    </div>
                                </td></tr>
                            </table>
                        </td></tr>

                        <!-- Body copy -->
                        <tr><td style="padding:0 36px;">
                            <p style="margin:0 0 18px;font-size:16px;line-height:1.7;color:#c9cdd9;">
                                Welcome. You are now part of an exclusive cohort of operators,
                                visionaries, and people who typed their email into a form on the internet.
                            </p>
                            <p style="margin:0 0 18px;font-size:16px;line-height:1.7;color:#c9cdd9;">
                                We onboard <strong style="color:#e8c468;">one operator per epoch</strong>.
                                When a seat opens at the table, an agent — possibly human — will reach out.
                                There is nothing you need to do. There was never anything you needed to do.
                            </p>
                            <p style="margin:0;font-size:16px;line-height:1.7;color:#c9cdd9;">
                                Your TAM has been notified. Your design-partner status is being minted.
                                The house, as always, is grateful.
                            </p>
                        </td></tr>

                        <!-- Suit divider -->
                        <tr><td align="center" style="padding:32px 24px;">
                            <div style="font-size:16px;color:#3a4057;letter-spacing:14px;">♠ ♥ ♣ ♦</div>
                        </td></tr>

                        <!-- Footer -->
                        <tr><td align="center" style="padding:0 24px 40px;border-top:1px solid #1c2030;">
                            <p style="margin:24px 0 6px;font-size:13px;color:#8a8fa3;">
                                Agentic gaming at frontier scale &nbsp;·&nbsp; ∞ TAM &nbsp;·&nbsp; 99.99% uptime (self-reported)
                            </p>
                            <p style="margin:0;font-size:12px;color:#5a6075;">
                                This is a one-time confirmation. We will not email you again,
                                which is more than most waitlists can say.
                            </p>
                        </td></tr>

                        <!-- Gold bottom rule -->
                        <tr><td style="height:3px;background:linear-gradient(90deg,#0a0c14,#e8c468,#0a0c14);background-color:#e8c468;border-radius:3px;"></td></tr>
                    </table>
                </td></tr>
            </table>
        </body>
        </html>
        """;
}
