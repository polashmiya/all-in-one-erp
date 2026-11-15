using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Erp.Api.Auth;

public sealed record SessionTokens(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    bool MustChangePassword);

public sealed class SignInService(
    ErpDbContext db,
    IPasswordHasher hasher,
    TokenService tokens,
    SessionCheck sessionCheck,
    IOptions<AuthOptions> options,
    TimeProvider clock,
    IHttpContextAccessor http)
{
    private const int LoginMaxLength = 256;
    private const int UserAgentMaxLength = 500;

    private AuthOptions Options => options.Value;

    public async Task<ISignInUser?> FindAsync(string surface, Guid userId, CancellationToken cancellationToken)
    {
        if (surface == AuthSurfaces.Erp)
        {
            return await db.ErpUsers.AcrossTenants().Include(u => u.Account)
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        return await db.PanelUsers.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<Guid?> ActiveStampAsync(string surface, Guid userId, CancellationToken cancellationToken)
    {
        if (surface == AuthSurfaces.Erp)
        {
            return await db.ErpUsers.AcrossTenants()
                .Where(u => u.Id == userId && u.IsActive && u.Account!.IsActive)
                .Select(u => (Guid?)u.SecurityStamp)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await db.PanelUsers
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => (Guid?)u.SecurityStamp)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ISignInUser?> FindByLoginAsync(string surface, string login, CancellationToken cancellationToken)
    {
        var value = UserNames.Normalize(login);
        var byEmail = UserNames.IsEmail(value);
        if (surface == AuthSurfaces.Erp)
        {
            return await db.ErpUsers.AcrossTenants().Include(u => u.Account)
                .SingleOrDefaultAsync(u => byEmail ? u.Email == value : u.Username == value, cancellationToken);
        }

        return await db.PanelUsers.SingleOrDefaultAsync(u => u.Email == value, cancellationToken);
    }

    public static bool CanSignIn(ISignInUser user) =>
        user.IsActive && (user is not ErpUser erp || erp.Account?.IsActive == true);

    public async Task<bool> TryPasswordAsync(
        string surface, string login, ISignInUser? user, string password, CancellationToken cancellationToken)
    {
        var result = Check(user, password);
        if (result != LoginResults.SignedIn)
        {
            Record(surface, login, result, user?.Id);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        return true;
    }

    public string CheckCurrentPassword(ISignInUser user, string password) => Check(user, password);

    public SessionTokens SignIn(string surface, ISignInUser user, string login)
    {
        user.LastLoginAt = clock.GetUtcNow();
        Record(surface, login, LoginResults.SignedIn, user.Id);
        return StartSession(surface, user);
    }

    public SessionTokens StartSession(string surface, ISignInUser user) => Issue(surface, user, Guid.NewGuid());

    public async Task<SessionTokens?> RefreshAsync(string surface, string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        var now = clock.GetUtcNow();
        var session = await db.UserSessions
            .SingleOrDefaultAsync(s => s.TokenHash == hash && s.Surface == surface, cancellationToken);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now) return null;

        if (session.RotatedAt is { } rotatedAt && now - rotatedAt > Options.RefreshReuseGrace)
        {
            await RevokeAsync(db.UserSessions.Where(s => s.FamilyId == session.FamilyId), cancellationToken);
            return null;
        }

        var user = await FindAsync(surface, session.UserId, cancellationToken);
        if (user is null || !CanSignIn(user)) return null;

        session.RotatedAt ??= now;
        var next = Issue(surface, user, session.FamilyId);
        await db.SaveChangesAsync(cancellationToken);
        return next;
    }

    public Task EndSessionAsync(Guid familyId, CancellationToken cancellationToken) =>
        RevokeAsync(db.UserSessions.Where(s => s.FamilyId == familyId), cancellationToken);

    public async Task EndAllSessionsAsync(string surface, ISignInUser user, CancellationToken cancellationToken)
    {
        user.SecurityStamp = Guid.NewGuid();
        await RevokeAsync(db.UserSessions.Where(s => s.Surface == surface && s.UserId == user.Id), cancellationToken);
        sessionCheck.Forget(surface, user.Id);
    }

    public async Task SetPasswordAsync(
        string surface, ISignInUser user, string password, bool mustChange, CancellationToken cancellationToken)
    {
        user.PasswordHash = hasher.Hash(password);
        user.MustChangePassword = mustChange;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        await EndAllSessionsAsync(surface, user, cancellationToken);
    }

    public void Record(string surface, string login, string result, Guid? userId = null)
    {
        db.LoginEvents.Add(new LoginEvent
        {
            Surface = surface,
            UserId = userId,
            Login = Truncate(login, LoginMaxLength),
            Result = result,
            Succeeded = result is LoginResults.SignedIn or LoginResults.PasswordChanged or LoginResults.PasswordReset,
            IpAddress = RemoteIp,
            UserAgent = UserAgent,
        });
    }

    private string Check(ISignInUser? user, string password)
    {
        if (user is null)
        {
            hasher.SimulateVerify();
            return LoginResults.UnknownUser;
        }

        var now = clock.GetUtcNow();
        if (user.LockedUntil > now)
        {
            hasher.SimulateVerify();
            return LoginResults.Locked;
        }

        if (!hasher.Verify(password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount < Options.LockoutAttempts) return LoginResults.WrongPassword;

            user.FailedLoginCount = 0;
            user.LockedUntil = now + Options.LockoutDuration;
            return LoginResults.Locked;
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        return CanSignIn(user) ? LoginResults.SignedIn : LoginResults.Inactive;
    }

    private SessionTokens Issue(string surface, ISignInUser user, Guid familyId)
    {
        var refreshToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var refreshExpiresAt = clock.GetUtcNow() + Options.RefreshTokenLifetime;

        db.UserSessions.Add(new UserSession
        {
            Surface = surface,
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = Hash(refreshToken),
            ExpiresAt = refreshExpiresAt,
            IpAddress = RemoteIp,
            UserAgent = UserAgent,
        });

        var access = tokens.Issue(surface, Claims(user, familyId));
        return new SessionTokens(access.AccessToken, access.ExpiresAt, refreshToken, refreshExpiresAt, user.MustChangePassword);
    }

    private static List<Claim> Claims(ISignInUser user, Guid familyId)
    {
        List<Claim> claims =
        [
            new(ErpClaimTypes.Subject, user.Id.ToString()),
            new(ErpClaimTypes.Email, user.Email),
            new(ErpClaimTypes.Name, user.FullName),
            new(ErpClaimTypes.SecurityStamp, user.SecurityStamp.ToString()),
            new(ErpClaimTypes.SessionId, familyId.ToString()),
        ];

        if (user.MustChangePassword) claims.Add(new(ErpClaimTypes.MustChangePassword, "true"));

        switch (user)
        {
            case ErpUser erp:
                claims.Add(new(ErpClaimTypes.AccountId, erp.AccountId.ToString()));
                claims.Add(new(ErpClaimTypes.AccountAdmin, erp.IsAccountAdmin ? "true" : "false"));
                break;
            case PanelUser panel:
                claims.Add(new(ErpClaimTypes.Role, panel.Role.ToString()));
                break;
        }

        return claims;
    }

    private Task RevokeAsync(IQueryable<UserSession> sessions, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return sessions.Where(s => s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string? RemoteIp => http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent => Truncate(http.HttpContext?.Request.Headers.UserAgent.ToString(), UserAgentMaxLength);

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max];
}
