using System.Net;
using System.Net.Mail;
using Erp.Core.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Erp.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string Section = "Email";

    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>When set, emails are written to this folder as .eml files instead of being sent (development, E2E).</summary>
    public string? PickupDirectory { get; set; }
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, IHostEnvironment environment) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var mail = new MailMessage(new MailAddress(settings.FromAddress, settings.FromName), new MailAddress(message.To))
        {
            Subject = message.Subject,
            Body = message.Body,
        };
        using var client = new SmtpClient();

        if (!string.IsNullOrWhiteSpace(settings.PickupDirectory))
        {
            var folder = Path.GetFullPath(settings.PickupDirectory, environment.ContentRootPath);
            Directory.CreateDirectory(folder);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = folder;
        }
        else
        {
            client.Host = settings.Host;
            client.Port = settings.Port;
            client.EnableSsl = settings.EnableSsl;
            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
            }
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
