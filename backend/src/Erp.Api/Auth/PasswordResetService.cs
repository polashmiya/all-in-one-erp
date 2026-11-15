using System.Security.Cryptography;
using Erp.Core.Domain.Identity;
using Erp.Core.Notifications;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Erp.Api.Auth;

public sealed class PasswordResetService(
    ErpDbContext db,
    SignInService signIn,
    IEmailSender email,
    IOptions<AuthOptions> options,
    TimeProvider clock)
{
    private const int CodeDigits = 6;
    private static readonly int CodeLimit = (int)Math.Pow(10, CodeDigits);

    private AuthOptions Options => options.Value;

    public async Task RequestAsync(string surface, string login, string? language, CancellationToken cancellationToken)
    {
        var user = await signIn.FindByLoginAsync(surface, login, cancellationToken);
        if (user is null || !SignInService.CanSignIn(user)) return;

        var now = clock.GetUtcNow();
        var codes = db.PasswordResetCodes.Where(c => c.Surface == surface && c.UserId == user.Id);
        var lastSentAt = await codes.MaxAsync(c => (DateTimeOffset?)c.CreatedAt, cancellationToken);
        if (lastSentAt is not null && now - lastSentAt < Options.ResetCodeResendAfter) return;

        await codes.Where(c => c.UsedAt == null)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.UsedAt, now), cancellationToken);

        var code = RandomNumberGenerator.GetInt32(CodeLimit).ToString($"D{CodeDigits}");
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            Surface = surface,
            UserId = user.Id,
            CodeHash = SignInService.Hash(code),
            ExpiresAt = now + Options.ResetCodeLifetime,
        });
        await db.SaveChangesAsync(cancellationToken);

        await email.SendAsync(
            PasswordResetEmail.Build(user, code, Options.ResetCodeLifetime, language), cancellationToken);
    }

    public async Task<bool> ResetAsync(
        string surface, string login, string code, string newPassword, CancellationToken cancellationToken)
    {
        var user = await signIn.FindByLoginAsync(surface, login, cancellationToken);
        if (user is null || !SignInService.CanSignIn(user)) return false;

        var now = clock.GetUtcNow();
        var entry = await db.PasswordResetCodes
            .Where(c => c.Surface == surface && c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null) return false;

        if (entry.CodeHash != SignInService.Hash(code))
        {
            entry.Attempts++;
            if (entry.Attempts >= Options.ResetCodeAttempts) entry.UsedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        entry.UsedAt = now;
        await signIn.SetPasswordAsync(surface, user, newPassword, mustChange: false, cancellationToken);
        signIn.Record(surface, login, LoginResults.PasswordReset, user.Id);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public static class PasswordResetEmail
{
    public const string Bangla = "bn";

    public static EmailMessage Build(ISignInUser user, string code, TimeSpan lifetime, string? language)
    {
        var minutes = (int)lifetime.TotalMinutes;
        return language == Bangla
            ? new EmailMessage(user.Email, "myErp পাসওয়ার্ড রিসেট কোড", Lines(
                $"প্রিয় {user.FullName},",
                "আপনার পাসওয়ার্ড রিসেট কোড:",
                code,
                $"কোডটি {minutes} মিনিট কাজ করবে। আপনি অনুরোধ না করে থাকলে এই ইমেইল উপেক্ষা করুন।"))
            : new EmailMessage(user.Email, "Your myErp password reset code", Lines(
                $"Hi {user.FullName},",
                "Your password reset code is:",
                code,
                $"It works for {minutes} minutes. If you did not ask for it, ignore this email."));
    }

    private static string Lines(params string[] paragraphs) => string.Join("\r\n\r\n", paragraphs);
}
