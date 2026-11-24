using Erp.Api.Accounting.ChartOfAccounts;
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

namespace Erp.Api.Accounting.BankAccounts;

internal static class BankAccountEndpoints
{
    private const string Feature = AccountingMenu.BankAccounts;

    public static void Map(IEndpointRouteBuilder group)
    {
        var banks = group.MapGroup("/bank-accounts");

        banks.MapGet("", List)
            .WithName("ListBankAccounts")
            .WithSummary("Bank and mobile banking accounts of the selected business unit")
            .RequirePermission($"{Feature}.view");

        banks.MapGet("/options", Options)
            .WithName("GetBankAccountOptions")
            .WithSummary("Every bank account of the selected business unit, for dropdowns")
            .RequireAnyPermission(AccountingModule.BankReaders);

        banks.MapPost("", Create)
            .WithName("CreateBankAccount")
            .WithSummary("Create a bank account; without an account in the books it gets a new one under Cash and bank")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveBankAccountRequest>();

        banks.MapPut("/{id:int}", Update)
            .WithName("UpdateBankAccount")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveBankAccountRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        banks.MapPost("/status", SetStatus)
            .WithName("SetBankAccountsStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<ChartOfAccountStatusRequest>();
    }

    private static string TypeKey(BankAccountType type) => type.ToString().ToLowerInvariant();

    private static async Task<BankAccountList> List(
        string? search, string? status, string? type, string? sort, int? page, int? pageSize, ErpDbContext db, CancellationToken cancellationToken)
    {
        var banks = db.BankAccounts.AsNoTracking();
        if (DocumentData.Trimmed(search) is { } term)
        {
            banks = banks.Where(b => b.Name.Contains(term) || b.AccountNo.Contains(term) || (b.BankName != null && b.BankName.Contains(term)));
        }

        if (Enum.TryParse<BankAccountType>(type, true, out var only)) banks = banks.Where(b => b.Type == only);

        var all = await banks.CountAsync(cancellationToken);
        var active = await banks.CountAsync(b => b.IsActive, cancellationToken);
        var filtered = status?.ToLowerInvariant() switch
        {
            "active" => banks.Where(b => b.IsActive),
            "inactive" => banks.Where(b => !b.IsActive),
            _ => banks,
        };
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort == "-name" ? filtered.OrderByDescending(b => b.Name) : filtered.OrderBy(b => b.Name);
        var rows = await DocumentData.Page(sorted, page, pageSize).Include(b => b.ChartOfAccount).ToListAsync(cancellationToken);
        return new BankAccountList([.. rows.Select(Row)], total, new ChartOfAccountCounts(all, active, all - active));
    }

    private static Task<List<BankAccountOption>> Options(ErpDbContext db, CancellationToken cancellationToken) =>
        db.BankAccounts.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BankAccountOption(b.Id, b.AccountNo, b.Name, b.Type == BankAccountType.Bank ? "bank" : "mobile", b.IsActive))
            .ToListAsync(cancellationToken);

    private static Task<Results<Ok<BankAccountRow>, NotFound, ValidationProblem>> Create(
        SaveBankAccountRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var bank = new BankAccount
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Name = "",
            AccountNo = "",
        };
        return SaveAsync(bank, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<BankAccountRow>, NotFound, ValidationProblem>> Update(
        int id, SaveBankAccountRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken) =>
        await db.BankAccounts.Include(b => b.ChartOfAccount).SingleOrDefaultAsync(b => b.Id == id, cancellationToken) is { } bank
            ? await SaveAsync(bank, request, current, db, configs, cancellationToken)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> SetStatus(
        ChartOfAccountStatusRequest request, ErpDbContext db, CancellationToken cancellationToken)
    {
        var banks = await db.BankAccounts.Where(b => request.Ids.Contains(b.Id)).ToListAsync(cancellationToken);
        if (banks.Count != request.Ids.Distinct().Count()) return TypedResults.NotFound();
        foreach (var bank in banks) bank.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<BankAccountRow>, NotFound, ValidationProblem>> SaveAsync(
        BankAccount bank, SaveBankAccountRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = bank.Id > 0 ? Row(bank) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);
        var name = request.Name.Trim();
        if (await db.BankAccounts.AnyAsync(b => b.Id != bank.Id && b.Name == name, cancellationToken))
            errors["name"] = ["Another bank account already has this title."];

        ChartOfAccount? account = null;
        if ((request.ChartOfAccountId ?? existing?.ChartOfAccountId) is { } accountId)
        {
            account = await db.ChartOfAccounts.SingleOrDefaultAsync(
                a => a.Id == accountId && a.IsActive && !a.IsGroup && a.Type == AccountType.CurrentAsset, cancellationToken);
            if (account is null) errors["chartOfAccountId"] = ["Choose an active current asset account."];
            else if (await db.BankAccounts.AcrossTenants().AnyAsync(b => b.Id != bank.Id && b.ChartOfAccountId == accountId, cancellationToken))
                errors["chartOfAccountId"] = ["Another bank account already uses this account."];
        }
        else if (await db.ChartOfAccounts.AnyAsync(a => a.Name == name, cancellationToken))
        {
            errors["chartOfAccountId"] = ["An account with this name exists. Choose it or use another title."];
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        bank.Name = name;
        bank.Type = Enum.Parse<BankAccountType>(request.Type, true);
        bank.BankName = DocumentData.Trimmed(request.BankName);
        bank.BranchName = DocumentData.Trimmed(request.BranchName);
        bank.AccountNo = request.AccountNo.Trim();
        bank.RoutingNo = DocumentData.Trimmed(request.RoutingNo);
        bank.ChartOfAccount = account ?? await NewAccountAsync(db, bank, cancellationToken);
        bank.IsActive = request.IsActive;
        bank.CustomData = CustomFieldData.ToJson(request.CustomData);
        if (bank.Id == 0) db.BankAccounts.Add(bank);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(Row(bank));
    }

    private static async Task<ChartOfAccount> NewAccountAsync(ErpDbContext db, BankAccount bank, CancellationToken cancellationToken)
    {
        var key = bank.Type == BankAccountType.Bank ? PostingKeys.Bank : PostingKeys.MobileBanking;
        var sibling = await db.PostingAccounts.Where(p => p.Key == key).Select(p => p.ChartOfAccount!).SingleAsync(cancellationToken);
        var codes = await db.ChartOfAccounts.Select(a => a.Code).ToListAsync(cancellationToken);
        var number = 1;
        while (codes.Contains(Code(number))) number++;
        return new ChartOfAccount
        {
            AccountId = bank.AccountId,
            Code = Code(number),
            Name = bank.Name,
            Type = sibling.Type,
            ParentId = sibling.ParentId,
        };

        string Code(int n) => $"{sibling.Code}-{n:00}";
    }

    private static BankAccountRow Row(BankAccount b) => new(
        b.Id, b.Name, TypeKey(b.Type), b.BankName, b.BranchName, b.AccountNo, b.RoutingNo, b.ChartOfAccountId, b.ChartOfAccount!.Name,
        b.IsActive, b.CreatedAt, b.ModifiedAt, CustomFieldData.Parse(b.CustomData));
}
