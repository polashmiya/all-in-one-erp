using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Inventory;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Sales;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Sales;

internal static class SalesPosting
{
    public static async Task<ProblemHttpResult?> OrderAsync(
        ErpDbContext db, ICurrentContext current, SalesOrder order, CancellationToken cancellationToken)
    {
        if (order.AdvanceAmount == 0) return null;
        var (_, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            SalesMenu.SalesOrder, order.Id, order.Code, order.OrderDate, $"Customer advance {order.Code}",
            [
                await PaymentSources.DebitAsync(db, order.PaymentMethodId, order.BankAccountId, order.AdvanceAmount, cancellationToken),
                ToCustomer(PostingKeys.CustomerAdvance, -order.AdvanceAmount, order),
            ]), cancellationToken);
        return problem;
    }

    public static async Task<ProblemHttpResult?> DeliveryAsync(
        ErpDbContext db, ICurrentContext current, SalesDelivery delivery, CancellationToken cancellationToken)
    {
        var lines = delivery.Lines.OrderBy(l => l.Sequence).ToList();
        var (transaction, refused) = await StockPosting.PostAsync(db, current, new StockEntry(
            SalesMenu.SalesDelivery, delivery.Id, delivery.Code, delivery.DeliveryDate, null, delivery.CustomerName,
            [.. lines.Select(l => StockEntryLine.Out(l.ItemId, l.UomId, l.WarehouseId, l.Quantity))]), cancellationToken);
        if (transaction is null) return refused;

        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].CostTotal = -transaction.Lines[i].Value;
            lines[i].CostPrice = Math.Round(lines[i].CostTotal / lines[i].Quantity, 4);
        }

        var cost = lines.Sum(l => l.CostTotal);
        var sales = lines.Sum(l => l.TotalValue - l.SdAmount - l.VatAmount);
        var (journal, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            SalesMenu.SalesDelivery, delivery.Id, delivery.Code, delivery.DeliveryDate, $"Goods sold {delivery.Code}",
            [
                ToCustomer(PostingKeys.AccountsReceivable, delivery.PendingAmount, delivery),
                ToCustomer(PostingKeys.CustomerAdvance, delivery.AdvanceBalanceAdjust, delivery),
                await PaymentSources.DebitAsync(
                    db, delivery.PaymentMethodId, delivery.BankAccountId, delivery.ReceiveAmount, cancellationToken),
                JournalEntryLine.Debit(PostingKeys.SalesDiscount, delivery.DeductionValue + delivery.SpecialDiscountAmount),
                JournalEntryLine.Credit(PostingKeys.Sales, sales),
                JournalEntryLine.Credit(PostingKeys.SdPayable, delivery.TotalSd),
                JournalEntryLine.Credit(PostingKeys.VatPayable, delivery.TotalVat),
                Charge(delivery.OtherChargeAccountId, delivery.OthersCost),
                Charge(delivery.ExtraChargeAccountId, delivery.ExtraCharge),
                JournalEntryLine.Debit(PostingKeys.CostOfGoodsSold, cost),
                JournalEntryLine.Credit(PostingKeys.Inventory, cost),
            ]), cancellationToken);
        transaction.Journal = journal;
        return problem;
    }

    private static JournalEntryLine ToCustomer(string key, decimal amount, SalesDocument document) =>
        JournalEntryLine.Debit(key, amount) with { CustomerId = document.CustomerId };

    private static JournalEntryLine Charge(int? accountId, decimal amount) =>
        accountId is { } id ? JournalEntryLine.Account(id, -amount) : JournalEntryLine.Credit(PostingKeys.OtherIncome, amount);
}
