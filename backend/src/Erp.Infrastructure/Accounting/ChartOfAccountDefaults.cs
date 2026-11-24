using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Accounting;

public sealed class ChartOfAccountDefaults(ErpDbContext db)
{
    private sealed record Default(string Code, string Name, AccountType Type, string? Parent = null, bool IsGroup = false, string? Key = null);

    private static readonly Default[] Defaults =
    [
        new("1000", "Assets", AccountType.Asset, IsGroup: true),
        new("1100", "Cash and bank", AccountType.Asset, "1000", true),
        new("1110", "Cash in hand", AccountType.Asset, "1100", Key: PostingKeys.Cash),
        new("1120", "Bank", AccountType.Asset, "1100", Key: PostingKeys.Bank),
        new("1130", "Mobile banking", AccountType.Asset, "1100", Key: PostingKeys.MobileBanking),
        new("1200", "Accounts receivable", AccountType.Asset, "1000"),
        new("1300", "Inventory", AccountType.Asset, "1000", Key: PostingKeys.Inventory),
        new("1400", "VAT receivable", AccountType.Asset, "1000", Key: PostingKeys.VatReceivable),
        new("2000", "Liabilities", AccountType.Liability, IsGroup: true),
        new("2100", "Accounts payable", AccountType.Liability, "2000", Key: PostingKeys.AccountsPayable),
        new("2200", "VAT payable", AccountType.Liability, "2000"),
        new("2210", "VDS payable", AccountType.Liability, "2000", Key: PostingKeys.VdsPayable),
        new("2220", "TDS payable", AccountType.Liability, "2000", Key: PostingKeys.TdsPayable),
        new("3000", "Equity", AccountType.Equity, IsGroup: true),
        new("3100", "Capital", AccountType.Equity, "3000"),
        new("3200", "Retained earnings", AccountType.Equity, "3000"),
        new("3300", "Opening balance", AccountType.Equity, "3000", Key: PostingKeys.OpeningBalance),
        new("4000", "Income", AccountType.Income, IsGroup: true),
        new("4100", "Sales", AccountType.Income, "4000"),
        new("4200", "Purchase discount received", AccountType.Income, "4000", Key: PostingKeys.PurchaseDiscount),
        new("4300", "Stock gain", AccountType.Income, "4000", Key: PostingKeys.StockGain),
        new("5000", "Expenses", AccountType.Expense, IsGroup: true),
        new("5100", "Cost of goods sold", AccountType.Expense, "5000"),
        new("5200", "Stock loss", AccountType.Expense, "5000", Key: PostingKeys.StockLoss),
        new("5300", "Purchase price difference", AccountType.Expense, "5000", Key: PostingKeys.PriceDifference),
        new("5400", "Operating expenses", AccountType.Expense, "5000"),
    ];

    public async Task AddMissingAsync(Guid accountId, CancellationToken ct)
    {
        var accounts = await db.ChartOfAccounts.AcrossTenants().Where(a => a.AccountId == accountId).ToDictionaryAsync(a => a.Code, ct);
        var mapped = await db.PostingAccounts.AcrossTenants().Where(p => p.AccountId == accountId).Select(p => p.Key).ToListAsync(ct);
        var isNew = accounts.Count == 0;

        foreach (var item in Defaults)
        {
            var needsMapping = item.Key is not null && !mapped.Contains(item.Key);
            if (!isNew && !needsMapping) continue;

            if (!accounts.TryGetValue(item.Code, out var account))
            {
                account = new ChartOfAccount
                {
                    AccountId = accountId,
                    Code = item.Code,
                    Name = item.Name,
                    Type = item.Type,
                    IsGroup = item.IsGroup,
                    Parent = item.Parent is not null ? accounts.GetValueOrDefault(item.Parent) : null,
                };
                db.ChartOfAccounts.Add(account);
                accounts.Add(item.Code, account);
            }

            if (needsMapping) db.PostingAccounts.Add(new PostingAccount { AccountId = accountId, Key = item.Key!, ChartOfAccount = account });
        }
    }
}
