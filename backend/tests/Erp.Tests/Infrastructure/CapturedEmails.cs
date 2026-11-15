using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Erp.Core.Notifications;

namespace Erp.Tests.Infrastructure;

public sealed partial class CapturedEmails : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public int CountFor(string to) => _messages.Count(m => m.To == to);

    public string LastCodeFor(string to) =>
        Code().Match(_messages.Last(m => m.To == to).Body).Value;

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex Code();
}
