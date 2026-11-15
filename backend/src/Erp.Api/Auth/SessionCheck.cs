using Erp.Core.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Erp.Api.Auth;

public sealed class SessionCheck(IMemoryCache cache, IOptions<AuthOptions> options)
{
    public async Task ValidateAsync(TokenValidatedContext context, string surface)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirst(ErpClaimTypes.Subject)?.Value, out var userId) ||
            !Guid.TryParse(principal?.FindFirst(ErpClaimTypes.SecurityStamp)?.Value, out var stamp))
        {
            context.Fail("The session is no longer valid.");
            return;
        }

        var current = await cache.GetOrCreateAsync(Key(surface, userId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = options.Value.SessionCheckCache;
            var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInService>();
            return signIn.ActiveStampAsync(surface, userId, context.HttpContext.RequestAborted);
        });

        if (current != stamp) context.Fail("The session is no longer valid.");
    }

    public void Forget(string surface, Guid userId) => cache.Remove(Key(surface, userId));

    private static string Key(string surface, Guid userId) => $"session:{surface}:{userId}";
}
