using Erp.Core.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Erp.Api.Auth;

public static class AuthSetup
{
    /// <summary>
    /// Two bearer schemes, one per surface. Each validates only its own audience, so a Panel token presented to
    /// /api/erp fails authentication (401) instead of being accepted with the wrong privileges.
    /// </summary>
    public static IServiceCollection AddErpAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));

        services.AddAuthentication()
            .AddJwtBearer(AuthSurfaces.Panel)
            .AddJwtBearer(AuthSurfaces.Erp);

        foreach (var surface in new[] { AuthSurfaces.Panel, AuthSurfaces.Erp })
        {
            services.AddOptions<JwtBearerOptions>(surface)
                .Configure<IOptions<JwtOptions>>((bearer, jwt) => ConfigureBearer(bearer, jwt.Value, audience: surface));
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthSurfaces.Panel, policy => policy.AddAuthenticationSchemes(AuthSurfaces.Panel).RequireAuthenticatedUser())
            .AddPolicy(AuthSurfaces.Erp, policy => policy
                .AddAuthenticationSchemes(AuthSurfaces.Erp)
                .RequireAuthenticatedUser()
                .RequireClaim(ErpClaimTypes.AccountId));

        services.AddSingleton<TokenService>();
        services.AddSingleton<SessionCheck>();
        services.AddScoped<SignInService>();
        services.AddScoped<PasswordResetService>();
        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, JwtOptions jwt, string audience)
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = audience,
            IssuerSigningKey = jwt.CreateSigningKey(),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ErpClaimTypes.Name,
            RoleClaimType = ErpClaimTypes.Role,
        };
        bearer.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
                context.HttpContext.RequestServices.GetRequiredService<SessionCheck>().ValidateAsync(context, audience),
        };
    }
}
