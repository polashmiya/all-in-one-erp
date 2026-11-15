using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Core.Tenancy;
using Erp.Api.Hosting;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Auth;

public sealed record RefreshRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequest(string Login, string? Language);

public sealed record ResetPasswordRequest(string Login, string Code, string NewPassword);

internal sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Language).MaximumLength(8);
    }
}

internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Enter the 6-digit code.");
        RuleFor(x => x.NewPassword).NotEmpty().StrongPassword();
    }
}

internal sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(100);
    }
}

internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(x => x.NewPassword).NotEmpty().StrongPassword();
    }
}

public static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder auth, string surface, string namePrefix)
    {
        auth.MapPost("/refresh", (RefreshRequest request, SignInService signIn, CancellationToken cancellationToken) =>
                Refresh(surface, request, signIn, cancellationToken))
            .WithName($"{namePrefix}Refresh")
            .WithSummary("Exchange a refresh token for a new access and refresh token")
            .AllowAnonymous()
            .Validate<RefreshRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", Logout)
            .WithName($"{namePrefix}Logout")
            .WithSummary("End this session")
            .AllowBeforePasswordChange();

        auth.MapPost("/change-password", (
                    ChangePasswordRequest request,
                    ICurrentContext current,
                    SignInService signIn,
                    ErpDbContext db,
                    CancellationToken cancellationToken) =>
                ChangePassword(surface, request, current, signIn, db, cancellationToken))
            .WithName($"{namePrefix}ChangePassword")
            .WithSummary("Change the signed-in user's password; ends every other session")
            .AllowBeforePasswordChange()
            .Validate<ChangePasswordRequest>();

        auth.MapPost("/forgot-password", async (
                    ForgotPasswordRequest request, PasswordResetService resets, CancellationToken cancellationToken) =>
                {
                    await resets.RequestAsync(surface, request.Login, request.Language, cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName($"{namePrefix}ForgotPassword")
            .WithSummary("Email a password reset code; always answers the same, whether the user exists or not")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitSetup.LoginPolicy)
            .Validate<ForgotPasswordRequest>();

        auth.MapPost("/reset-password", async Task<Results<NoContent, ValidationProblem>> (
                    ResetPasswordRequest request, PasswordResetService resets, CancellationToken cancellationToken) =>
                await resets.ResetAsync(surface, request.Login, request.Code, request.NewPassword, cancellationToken)
                    ? TypedResults.NoContent()
                    : FieldError("code", "The code is wrong or has expired."))
            .WithName($"{namePrefix}ResetPassword")
            .WithSummary("Set a new password with the emailed code; ends every session of the user")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitSetup.LoginPolicy)
            .Validate<ResetPasswordRequest>();

        return auth;
    }

    private static async Task<Results<Ok<SessionTokens>, ProblemHttpResult>> Refresh(
        string surface, RefreshRequest request, SignInService signIn, CancellationToken cancellationToken)
    {
        var tokens = await signIn.RefreshAsync(surface, request.RefreshToken, cancellationToken);
        return tokens is null ? AuthProblems.SessionEnded() : TypedResults.Ok(tokens);
    }

    private static async Task<NoContent> Logout(HttpContext http, SignInService signIn, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(http.User.FindFirst(ErpClaimTypes.SessionId)?.Value, out var familyId))
        {
            await signIn.EndSessionAsync(familyId, cancellationToken);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<SessionTokens>, ValidationProblem, UnauthorizedHttpResult>> ChangePassword(
        string surface,
        ChangePasswordRequest request,
        ICurrentContext current,
        SignInService signIn,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (current.UserId is not { } userId) return TypedResults.Unauthorized();
        var user = await signIn.FindAsync(surface, userId, cancellationToken);
        if (user is null) return TypedResults.Unauthorized();

        if (signIn.CheckCurrentPassword(user, request.CurrentPassword) != LoginResults.SignedIn)
        {
            await db.SaveChangesAsync(cancellationToken);
            return FieldError("currentPassword", "The current password is wrong.");
        }

        if (request.NewPassword == request.CurrentPassword)
        {
            return FieldError("newPassword", "Choose a password different from the current one.");
        }

        await signIn.SetPasswordAsync(surface, user, request.NewPassword, mustChange: false, cancellationToken);
        signIn.Record(surface, user.Email, LoginResults.PasswordChanged, user.Id);
        var tokens = signIn.StartSession(surface, user);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(tokens);
    }

    private static ValidationProblem FieldError(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
