using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Accounting;

public sealed class ChartOfAccountDefaults(ErpDbContext db)
{
    private sealed record Default(string Code, string Name, AccountType Type, string? Parent = null, bool IsGroup = false, string? Key = null);

    private static readonly Default[] Groups =
    [
        new("1", "Current Assets", AccountType.CurrentAsset, IsGroup: true),
        new("2", "Non-Current Assets", AccountType.NonCurrentAsset, IsGroup: true),
        new("3", "Current Liabilities", AccountType.CurrentLiability, IsGroup: true),
        new("4", "Non-Current Liabilities", AccountType.NonCurrentLiability, IsGroup: true),
        new("5", "Equity", AccountType.Equity, IsGroup: true),
        new("6", "Income", AccountType.Income, IsGroup: true),
        new("7", "Cost Of Sales", AccountType.CostOfSales, IsGroup: true),
        new("8", "Expense", AccountType.Expense, IsGroup: true),
    ];

    private static readonly Default[] Accounts =
    [
        new("1100", "Cash and bank", AccountType.CurrentAsset, "1", true),
        new("1110", "Cash in hand", AccountType.CurrentAsset, "1100", Key: PostingKeys.Cash),
        new("1120", "Bank", AccountType.CurrentAsset, "1100", Key: PostingKeys.Bank),
        new("1130", "Mobile banking", AccountType.CurrentAsset, "1100", Key: PostingKeys.MobileBanking),
        new("1200", "Accounts receivable", AccountType.CurrentAsset, "1"),
        new("1250", "Advance to suppliers", AccountType.CurrentAsset, "1", Key: PostingKeys.SupplierAdvance),
        new("1300", "Inventory", AccountType.CurrentAsset, "1", Key: PostingKeys.Inventory),
        new("1400", "VAT receivable", AccountType.CurrentAsset, "1", Key: PostingKeys.VatReceivable),
        new("1450", "Security deposits", AccountType.CurrentAsset, "1"),
        new("2100", "Furniture and fixtures", AccountType.NonCurrentAsset, "2"),
        new("2200", "Office equipment", AccountType.NonCurrentAsset, "2"),
        new("2300", "Vehicles", AccountType.NonCurrentAsset, "2"),
        new("2900", "Accumulated depreciation", AccountType.NonCurrentAsset, "2"),
        new("3100", "Accounts payable", AccountType.CurrentLiability, "3", Key: PostingKeys.AccountsPayable),
        new("3200", "VAT payable", AccountType.CurrentLiability, "3"),
        new("3210", "VDS payable", AccountType.CurrentLiability, "3", Key: PostingKeys.VdsPayable),
        new("3220", "TDS payable", AccountType.CurrentLiability, "3", Key: PostingKeys.TdsPayable),
        new("3300", "Salary payable", AccountType.CurrentLiability, "3"),
        new("4100", "Bank loan", AccountType.NonCurrentLiability, "4"),
        new("5100", "Capital", AccountType.Equity, "5"),
        new("5200", "Retained earnings", AccountType.Equity, "5", Key: PostingKeys.RetainedEarnings),
        new("5300", "Opening balance", AccountType.Equity, "5", Key: PostingKeys.OpeningBalance),
        new("6100", "Sales", AccountType.Income, "6"),
        new("6200", "Purchase discount received", AccountType.Income, "6", Key: PostingKeys.PurchaseDiscount),
        new("6300", "Stock gain", AccountType.Income, "6", Key: PostingKeys.StockGain),
        new("6400", "Other income", AccountType.Income, "6"),
        new("7100", "Cost of goods sold", AccountType.CostOfSales, "7"),
        new("8100", "Stock loss", AccountType.Expense, "8", Key: PostingKeys.StockLoss),
        new("8200", "Purchase price difference", AccountType.Expense, "8", Key: PostingKeys.PriceDifference),
        new("8300", "Operating expenses", AccountType.Expense, "8"),
        new("8310", "Salary and wages", AccountType.Expense, "8"),
        new("8320", "Rent", AccountType.Expense, "8"),
        new("8330", "Utilities", AccountType.Expense, "8"),
        new("8340", "Transport", AccountType.Expense, "8"),
        new("8350", "Bank charges", AccountType.Expense, "8"),
        new("8360", "Depreciation", AccountType.Expense, "8"),
    ];

    public async Task AddMissingAsync(Guid accountId, CancellationToken ct)
    {
        var chart = await db.ChartOfAccounts.AcrossTenants().Where(a => a.AccountId == accountId).ToListAsync(ct);
        var mapped = await db.PostingAccounts.AcrossTenants().Where(p => p.AccountId == accountId).Select(p => p.Key).ToListAsync(ct);
        var complete = Groups.All(g => Root(chart, g.Type) is not null);

        foreach (var group in Groups)
        {
            if (Root(chart, group.Type) is null) Add(chart, accountId, group, null);
        }

        foreach (var item in Accounts)
        {
            var needsMapping = item.Key is not null && !mapped.Contains(item.Key);
            if (complete && !needsMapping) continue;

            var account = chart.Find(a => a.Name == item.Name) ?? chart.Find(a => a.Code == item.Code && a.Type == item.Type);
            if (account is null || (needsMapping && account.IsGroup))
            {
                var parent = chart.Find(a => a.Code == item.Parent && a.IsGroup && a.Type == item.Type) ?? Root(chart, item.Type);
                account = Add(chart, accountId, item, parent);
            }

            if (needsMapping) db.PostingAccounts.Add(new PostingAccount { AccountId = accountId, Key = item.Key!, ChartOfAccount = account });
        }

        foreach (var account in chart)
        {
            var root = Root(chart, account.Type)!;
            var parent = account.Parent ?? chart.Find(a => a.Id == account.ParentId && a.Id != 0);
            if (account != root && parent?.Type != account.Type) account.Parent = root;
        }
    }

    private ChartOfAccount Add(List<ChartOfAccount> chart, Guid accountId, Default item, ChartOfAccount? parent)
    {
        var account = new ChartOfAccount
        {
            AccountId = accountId,
            Code = Free(item.Code, chart.Select(a => a.Code)),
            Name = Free(item.Name, chart.Select(a => a.Name)),
            Type = item.Type,
            IsGroup = item.IsGroup,
            Parent = parent,
        };
        db.ChartOfAccounts.Add(account);
        chart.Add(account);
        return account;
    }

    private static ChartOfAccount? Root(List<ChartOfAccount> chart, AccountType type) =>
        chart.Find(a => a.Type == type && a.IsGroup && a.ParentId is null && a.Parent is null);

    private static string Free(string wanted, IEnumerable<string> used)
    {
        var taken = used.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var value = wanted;
        for (var n = 2; taken.Contains(value); n++) value = $"{wanted}-{n}";
        return value;
    }
}
