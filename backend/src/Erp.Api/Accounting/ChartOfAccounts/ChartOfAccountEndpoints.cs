using System.Text.Json;
using Erp.Api.Documents;
using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting.ChartOfAccounts;

internal static class ChartOfAccountEndpoints
{
    private const string Feature = AccountingMenu.ChartOfAccounts;

    public static void Map(IEndpointRouteBuilder group)
    {
        var accounts = group.MapGroup("/chart-of-accounts");

        accounts.MapGet("", List)
            .WithName("ListChartOfAccounts")
            .WithSummary("Accounts of the account holder, shared by all its business units")
            .RequirePermission($"{Feature}.view");

        accounts.MapGet("/options", Options)
            .WithName("GetChartOfAccountOptions")
            .WithSummary("Every account, for dropdowns")
            .RequireAnyPermission(AccountingModule.Readers);

        accounts.MapPost("", Create)
            .WithName("CreateChartOfAccount")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveChartOfAccountRequest>();

        accounts.MapPut("/{id:int}", Update)
            .WithName("UpdateChartOfAccount")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveChartOfAccountRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        accounts.MapPost("/status", SetStatus)
            .WithName("SetChartOfAccountsStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<ChartOfAccountStatusRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        var posting = group.MapGroup("/posting-accounts");

        posting.MapGet("", Posting)
            .WithName("ListPostingAccounts")
            .WithSummary("The account every automatic posting uses")
            .RequirePermission($"{AccountingMenu.PostingAccounts}.view");

        posting.MapPut("", SavePosting)
            .WithName("SavePostingAccounts")
            .RequirePermission($"{AccountingMenu.PostingAccounts}.edit")
            .Validate<SavePostingAccountsRequest>();
    }

    public static string TypeKey(AccountType type) => JsonNamingPolicy.CamelCase.ConvertName(type.ToString());

    private static async Task<ChartOfAccountList> List(
        string? search,
        string? status,
        string? type,
        int? parentId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var accounts = db.ChartOfAccounts.AsNoTracking();
        if (DocumentData.Trimmed(search) is { } term) accounts = accounts.Where(a => a.Code.Contains(term) || a.Name.Contains(term));
        if (Enum.TryParse<AccountType>(type, true, out var only)) accounts = accounts.Where(a => a.Type == only);
        if (parentId is not null) accounts = accounts.Where(a => a.ParentId == parentId);

        var all = await accounts.CountAsync(cancellationToken);
        var active = await accounts.CountAsync(a => a.IsActive, cancellationToken);
        var filtered = status?.ToLowerInvariant() switch
        {
            "active" => accounts.Where(a => a.IsActive),
            "inactive" => accounts.Where(a => !a.IsActive),
            _ => accounts,
        };
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "-code" => filtered.OrderByDescending(a => a.Code),
            "name" => filtered.OrderBy(a => a.Name),
            "-name" => filtered.OrderByDescending(a => a.Name),
            _ => filtered.OrderBy(a => a.Code),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize).Include(a => a.Parent).ToListAsync(cancellationToken);
        return new ChartOfAccountList([.. rows.Select(Row)], total, new ChartOfAccountCounts(all, active, all - active));
    }

    private static async Task<List<ChartOfAccountOption>> Options(ErpDbContext db, CancellationToken cancellationToken)
    {
        var keys = await db.PostingAccounts.AsNoTracking()
            .Select(p => new { p.ChartOfAccountId, p.Key })
            .ToListAsync(cancellationToken);
        var accounts = await db.ChartOfAccounts.AsNoTracking().OrderBy(a => a.Code).ToListAsync(cancellationToken);
        return [.. accounts.Select(a => new ChartOfAccountOption(
            a.Id, a.Code, a.Name, TypeKey(a.Type), a.IsGroup, a.ParentId, a.IsActive,
            [.. keys.Where(k => k.ChartOfAccountId == a.Id).Select(k => k.Key)]))];
    }

    private static Task<Results<Ok<ChartOfAccountRow>, NotFound, ValidationProblem>> Create(
        SaveChartOfAccountRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken) =>
        SaveAsync(
            new ChartOfAccount { AccountId = current.AccountId!.Value, Code = "", Name = "" },
            request, current, db, configs, cancellationToken);

    private static async Task<Results<Ok<ChartOfAccountRow>, NotFound, ValidationProblem>> Update(
        int id, SaveChartOfAccountRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken) =>
        await db.ChartOfAccounts.Include(a => a.Parent).SingleOrDefaultAsync(a => a.Id == id, cancellationToken) is { } account
            ? await SaveAsync(account, request, current, db, configs, cancellationToken)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> SetStatus(
        ChartOfAccountStatusRequest request, ErpDbContext db, CancellationToken cancellationToken)
    {
        var accounts = await db.ChartOfAccounts.Where(a => request.Ids.Contains(a.Id)).ToListAsync(cancellationToken);
        if (accounts.Count != request.Ids.Distinct().Count()) return TypedResults.NotFound();
        if (!request.IsActive && accounts.Any(a => a.ParentId is null))
        {
            return TypedResults.Problem(
                title: "Account in use", detail: "A main group stays active.", statusCode: StatusCodes.Status409Conflict);
        }

        if (!request.IsActive && await db.PostingAccounts.AnyAsync(p => request.Ids.Contains(p.ChartOfAccountId), cancellationToken))
        {
            return TypedResults.Problem(
                title: "Account in use",
                detail: "Automatic postings use this account. Choose another one in Posting Accounts first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        foreach (var account in accounts) account.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ChartOfAccountRow>, NotFound, ValidationProblem>> SaveAsync(
        ChartOfAccount account, SaveChartOfAccountRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = account.Id > 0 ? Row(account) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);
        var code = request.Code.Trim();
        var name = request.Name.Trim();
        var type = Enum.Parse<AccountType>(request.Type, true);

        if (await db.ChartOfAccounts.AnyAsync(a => a.Id != account.Id && a.Code == code, cancellationToken))
            errors["code"] = ["Another account already uses this code."];
        if (await db.ChartOfAccounts.AnyAsync(a => a.Id != account.Id && a.Name == name, cancellationToken))
            errors["name"] = ["Another account already has this name."];

        var parent = request.ParentId is { } parentId
            ? await db.ChartOfAccounts.SingleOrDefaultAsync(a => a.Id == parentId, cancellationToken)
            : null;
        var isRoot = account is { Id: > 0, ParentId: null };
        if (!isRoot && (parent is null || !parent.IsGroup || parent.Type != type ||
            await IsUnderAsync(db, parent, account.Id, cancellationToken)))
        {
            errors["parentId"] = ["Choose a group of the same type."];
        }

        if (account.Id > 0)
        {
            var posted = await db.JournalLines.AnyAsync(l => l.ChartOfAccountId == account.Id, cancellationToken);
            var hasChildren = await db.ChartOfAccounts.AnyAsync(a => a.ParentId == account.Id, cancellationToken);
            var mapped = await db.PostingAccounts.AnyAsync(p => p.ChartOfAccountId == account.Id, cancellationToken);
            if (type != account.Type && (posted || hasChildren || mapped)) errors["type"] = ["The type cannot change once the account is used."];
            if (request.IsGroup && !account.IsGroup && (posted || mapped)) errors["isGroup"] = ["An account in use cannot become a group."];
            if (!request.IsGroup && account.IsGroup && hasChildren) errors["isGroup"] = ["Move the accounts under this group first."];
            if (isRoot && (!request.IsGroup || !request.IsActive || type != account.Type)) errors["isGroup"] = ["A main group stays an active group of its type."];
            if (!request.IsActive && mapped) errors["isActive"] = ["Automatic postings use this account."];
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        account.Code = code;
        account.Name = name;
        account.Type = type;
        account.IsGroup = request.IsGroup;
        account.Parent = isRoot ? null : parent;
        account.ParentId = account.Parent?.Id;
        account.Description = DocumentData.Trimmed(request.Description);
        account.IsActive = request.IsActive;
        account.CustomData = CustomFieldData.ToJson(request.CustomData);
        if (account.Id == 0) db.ChartOfAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(Row(account));
    }

    private static async Task<bool> IsUnderAsync(ErpDbContext db, ChartOfAccount account, int ancestorId, CancellationToken cancellationToken)
    {
        if (ancestorId == 0) return false;
        for (int? id = account.Id; id is not null;)
        {
            if (id == ancestorId) return true;
            var currentId = id;
            id = await db.ChartOfAccounts.Where(a => a.Id == currentId).Select(a => a.ParentId).SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    private static async Task<List<PostingAccountRow>> Posting(ErpDbContext db, CancellationToken cancellationToken)
    {
        var accounts = await db.PostingAccounts.AsNoTracking().ToDictionaryAsync(p => p.Key, p => p.ChartOfAccountId, cancellationToken);
        return [.. PostingKeys.All.Select(key => new PostingAccountRow(key, accounts.TryGetValue(key, out var id) ? id : null))];
    }

    private static async Task<Results<Ok<List<PostingAccountRow>>, ValidationProblem>> SavePosting(
        SavePostingAccountsRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var sent = request.Items.Where(i => i.ChartOfAccountId is not null).Select(i => i.ChartOfAccountId!.Value).Distinct().ToList();
        var accounts = await db.ChartOfAccounts.Where(a => sent.Contains(a.Id) && a.IsActive && !a.IsGroup).Select(a => a.Id).ToListAsync(cancellationToken);
        var errors = request.Items
            .Where(i => !PostingKeys.All.Contains(i.Key) || i.ChartOfAccountId is not { } id || !accounts.Contains(id))
            .ToDictionary(i => i.Key, _ => new[] { "Choose an active account that is not a group." });
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var saved = await db.PostingAccounts.ToDictionaryAsync(p => p.Key, cancellationToken);
        foreach (var item in request.Items)
        {
            if (saved.TryGetValue(item.Key, out var mapping)) mapping.ChartOfAccountId = item.ChartOfAccountId!.Value;
            else db.PostingAccounts.Add(new PostingAccount { AccountId = current.AccountId!.Value, Key = item.Key, ChartOfAccountId = item.ChartOfAccountId!.Value });
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(await Posting(db, cancellationToken));
    }

    private static ChartOfAccountRow Row(ChartOfAccount a) => new(
        a.Id, a.Code, a.Name, TypeKey(a.Type), a.IsGroup, a.ParentId, a.Parent?.Name, a.Description, a.IsActive, a.CreatedAt, a.ModifiedAt,
        CustomFieldData.Parse(a.CustomData));
}
