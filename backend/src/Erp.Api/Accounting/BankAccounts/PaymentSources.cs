using Erp.Api.Procurement;
using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting.BankAccounts;

internal static class PaymentSources
{
    public static async Task CheckAsync(
        ErpDbContext db, int? paymentMethodId, int? bankAccountId, int? current, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var type = await TypeAsync(db, paymentMethodId, cancellationToken);
        if (bankAccountId is not { } id) return;
        if (type is null) errors["bankAccountId"] = ["Cash payments have no bank account."];
        else if (!await db.BankAccounts.AnyAsync(b => b.Id == id && b.Type == type && (b.IsActive || b.Id == current), cancellationToken))
            errors["bankAccountId"] = ["Choose an active account for this payment method."];
    }

    public static async Task<JournalEntryLine> CreditAsync(
        ErpDbContext db, int? paymentMethodId, int? bankAccountId, decimal amount, CancellationToken cancellationToken)
    {
        if (bankAccountId is { } id)
        {
            var accountId = await db.BankAccounts.Where(b => b.Id == id).Select(b => b.ChartOfAccountId).SingleAsync(cancellationToken);
            return JournalEntryLine.Account(accountId, -amount);
        }

        var key = await TypeAsync(db, paymentMethodId, cancellationToken) switch
        {
            BankAccountType.Bank => PostingKeys.Bank,
            BankAccountType.Mobile => PostingKeys.MobileBanking,
            _ => PostingKeys.Cash,
        };
        return JournalEntryLine.Credit(key, amount);
    }

    public static Task<JournalEntryLine> DebitAsync(
        ErpDbContext db, int? paymentMethodId, int? bankAccountId, decimal amount, CancellationToken cancellationToken) =>
        CreditAsync(db, paymentMethodId, bankAccountId, -amount, cancellationToken);

    private static async Task<BankAccountType?> TypeAsync(ErpDbContext db, int? paymentMethodId, CancellationToken cancellationToken)
    {
        var method = await db.LookupValues.Where(v => v.Id == paymentMethodId).Select(v => v.Code).FirstOrDefaultAsync(cancellationToken);
        return method switch
        {
            null or ProcurementLookups.Cash => null,
            ProcurementLookups.MobileBanking => BankAccountType.Mobile,
            _ => BankAccountType.Bank,
        };
    }
}
