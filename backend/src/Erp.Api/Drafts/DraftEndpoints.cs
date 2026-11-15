using Erp.Api.Auth;
using Erp.Core.Authorization;
using Erp.Core.Domain;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Erp.Api.Drafts;

public sealed class DraftOptions
{
    public const string Section = "Drafts";

    public int MaxDataLength { get; set; } = 200_000;
}

public sealed record DraftRow(Guid Id, string Feature, string Title, string? SavedBy, DateTimeOffset SavedAt);

public sealed record DraftDetail(Guid Id, string Feature, string Title, string Data);

public sealed record SaveDraftRequest(string Feature, string Title, string Data);

public static class DraftsModule
{
    public static IServiceCollection AddDraftsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DraftOptions>(configuration.GetSection(DraftOptions.Section));
        services.AddSingleton<IErpEndpoints, ErpDraftEndpoints>();
        services.AddSingleton<IPanelEndpoints, PanelDraftEndpoints>();
        return services;
    }
}

internal sealed class ErpDraftEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp) => DraftEndpoints.Map(erp, AuthSurfaces.Erp, "Drafts");
}

internal sealed class PanelDraftEndpoints : IPanelEndpoints
{
    public void Map(IEndpointRouteBuilder panel) => DraftEndpoints.Map(panel, AuthSurfaces.Panel, "PanelDrafts");
}

internal static class DraftEndpoints
{
    private const int FeatureMax = 150;
    private const int TitleMax = 200;
    private const string View = "view";
    private const string Create = "create";

    public static void Map(IEndpointRouteBuilder root, string surface, string name)
    {
        var drafts = root.MapGroup("/drafts").WithTags(name);

        drafts.MapGet("", (string feature, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct) =>
                List(surface, feature, current, db, permissions, ct))
            .WithName($"List{name}")
            .WithSummary("Drafts of one screen that the caller may see");

        drafts.MapGet("/{id:guid}", (Guid id, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct) =>
                Get(surface, id, current, db, permissions, ct))
            .WithName($"Get{name.TrimEnd('s')}")
            .ProducesProblem(StatusCodes.Status404NotFound);

        drafts.MapPost("", (SaveDraftRequest request, ICurrentContext current, ErpDbContext db, IPermissionService permissions,
                    IOptions<DraftOptions> options, CancellationToken ct) =>
                Save(surface, null, request, current, db, permissions, options.Value, ct))
            .WithName($"Create{name.TrimEnd('s')}");

        drafts.MapPut("/{id:guid}", (Guid id, SaveDraftRequest request, ICurrentContext current, ErpDbContext db,
                    IPermissionService permissions, IOptions<DraftOptions> options, CancellationToken ct) =>
                Save(surface, id, request, current, db, permissions, options.Value, ct))
            .WithName($"Update{name.TrimEnd('s')}")
            .ProducesProblem(StatusCodes.Status404NotFound);

        drafts.MapDelete("/{id:guid}", (Guid id, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct) =>
                Delete(surface, id, current, db, permissions, ct))
            .WithName($"Delete{name.TrimEnd('s')}")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<List<DraftRow>>, ProblemHttpResult>> List(
        string surface, string feature, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct)
    {
        if (!await MayAsync(surface, feature, View, current, db, permissions, ct)) return Denied();

        var drafts = await Scoped(db, surface, current)
            .Where(d => d.Feature == feature)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Select(d => new { d.Id, d.Feature, d.Title, By = d.UpdatedBy ?? d.CreatedBy, At = d.UpdatedAt ?? d.CreatedAt })
            .ToListAsync(ct);

        var ids = drafts.Where(d => d.By != null).Select(d => d.By!.Value).Distinct().ToList();
        var names = surface == AuthSurfaces.Erp
            ? await db.ErpUsers.AcrossTenants().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct)
            : await db.PanelUsers.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return TypedResults.Ok(drafts
            .Select(d => new DraftRow(d.Id, d.Feature, d.Title, d.By is { } by ? names.GetValueOrDefault(by) : null, d.At))
            .ToList());
    }

    private static async Task<Results<Ok<DraftDetail>, NotFound, ProblemHttpResult>> Get(
        string surface, Guid id, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct)
    {
        var draft = await Scoped(db, surface, current).AsNoTracking().SingleOrDefaultAsync(d => d.Id == id, ct);
        if (draft is null) return TypedResults.NotFound();
        if (!await MayAsync(surface, draft.Feature, View, current, db, permissions, ct)) return Denied();
        return TypedResults.Ok(new DraftDetail(draft.Id, draft.Feature, draft.Title, draft.Data));
    }

    private static async Task<Results<Ok<DraftDetail>, NotFound, ProblemHttpResult, ValidationProblem>> Save(
        string surface,
        Guid? id,
        SaveDraftRequest request,
        ICurrentContext current,
        ErpDbContext db,
        IPermissionService permissions,
        DraftOptions options,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Feature) || request.Feature.Length > FeatureMax) errors["feature"] = ["Choose a screen."];
        if (request.Title.Length > TitleMax) errors["title"] = [$"Use at most {TitleMax} characters."];
        if (string.IsNullOrEmpty(request.Data) || request.Data.Length > options.MaxDataLength) errors["data"] = ["The draft is empty or too large."];
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        if (!await MayAsync(surface, request.Feature, Create, current, db, permissions, ct)) return Denied();

        Draft? draft;
        if (id is { } existing)
        {
            draft = await Scoped(db, surface, current).SingleOrDefaultAsync(d => d.Id == existing && d.Feature == request.Feature, ct);
            if (draft is null) return TypedResults.NotFound();
        }
        else
        {
            draft = new Draft
            {
                Surface = surface,
                AccountId = surface == AuthSurfaces.Erp ? current.AccountId : null,
                BusinessUnitId = surface == AuthSurfaces.Erp ? current.BusinessUnitId : null,
                Feature = request.Feature,
                Title = "",
                Data = "",
            };
            db.Drafts.Add(draft);
        }

        draft.Title = request.Title.Trim();
        draft.Data = request.Data;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new DraftDetail(draft.Id, draft.Feature, draft.Title, draft.Data));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(
        string surface, Guid id, ICurrentContext current, ErpDbContext db, IPermissionService permissions, CancellationToken ct)
    {
        var draft = await Scoped(db, surface, current).SingleOrDefaultAsync(d => d.Id == id, ct);
        if (draft is null) return TypedResults.NotFound();
        if (!await MayAsync(surface, draft.Feature, Create, current, db, permissions, ct)) return Denied();

        db.Drafts.Remove(draft);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static ProblemHttpResult Denied() =>
        TypedResults.Problem(title: "Permission denied", detail: "You cannot use drafts of this screen.", statusCode: StatusCodes.Status403Forbidden);

    private static IQueryable<Draft> Scoped(ErpDbContext db, string surface, ICurrentContext current) =>
        surface == AuthSurfaces.Erp
            ? db.Drafts.Where(d => d.Surface == surface && d.AccountId == current.AccountId && d.BusinessUnitId == current.BusinessUnitId)
            : db.Drafts.Where(d => d.Surface == surface);

    private static async Task<bool> MayAsync(
        string surface, string feature, string action, ICurrentContext current, ErpDbContext db, IPermissionService permissions,
        CancellationToken ct)
    {
        if (surface != AuthSurfaces.Erp) return true;
        var features = await db.UnitFeatureCodesAsync(current.AccountId!.Value, current.BusinessUnitId!.Value, ct);
        return features.Contains(feature) && (await permissions.GetCurrentAsync(ct)).Has($"{feature}.{action}");
    }
}
