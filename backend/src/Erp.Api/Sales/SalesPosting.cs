using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Inventory;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Inventory;
using Erp.Core.Domain.Sales;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
                ToCustomer(PostingKeys.CustomerAdvance, -order.AdvanceAmount, order.CustomerId),
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
                ToCustomer(PostingKeys.AccountsReceivable, delivery.PendingAmount, delivery.CustomerId),
                ToCustomer(PostingKeys.CustomerAdvance, delivery.AdvanceBalanceAdjust, delivery.CustomerId),
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

    public static async Task<ProblemHttpResult?> ReturnAsync(
        ErpDbContext db, ICurrentContext current, SalesReturn salesReturn, CancellationToken cancellationToken)
    {
        var lines = salesReturn.Lines.OrderBy(l => l.Sequence).ToList();
        var restocked = lines.Where(l => !l.IsDamaged).ToList();
        InventoryTransaction? transaction = null;
        if (restocked.Count > 0)
        {
            (transaction, var refused) = await StockPosting.PostAsync(db, current, new StockEntry(
                SalesMenu.SalesReturn, salesReturn.Id, salesReturn.Code, salesReturn.ReturnDate, null, salesReturn.CustomerName,
                [.. restocked.Select(l => StockEntryLine.In(l.ItemId, l.UomId, salesReturn.WarehouseId, l.Quantity, l.CostTotal))]),
                cancellationToken);
            if (transaction is null) return refused;
        }

        var sales = lines.Sum(l => l.TotalValue - l.SdAmount - l.VatAmount);
        var chargeAccount = await db.SalesDeliveries.Where(d => d.Id == salesReturn.DeliveryId)
            .Select(d => d.OtherChargeAccountId ?? d.ExtraChargeAccountId).SingleAsync(cancellationToken);
        var refund = await PaymentSources.CreditAsync(
            db, salesReturn.PaymentMethodId, salesReturn.BankAccountId, salesReturn.RefundAmount, cancellationToken);
        var (journal, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            SalesMenu.SalesReturn, salesReturn.Id, salesReturn.Code, salesReturn.ReturnDate, $"Goods returned by customer {salesReturn.Code}",
            [
                JournalEntryLine.Debit(PostingKeys.SalesReturn, sales),
                JournalEntryLine.Debit(PostingKeys.SdPayable, salesReturn.TotalSd),
                JournalEntryLine.Debit(PostingKeys.VatPayable, salesReturn.TotalVat),
                Charge(chargeAccount, -salesReturn.ChargeRefundAmount),
                JournalEntryLine.Credit(PostingKeys.SalesDiscount, salesReturn.SpecialDiscountAmount),
                JournalEntryLine.Credit(PostingKeys.OtherIncome, salesReturn.DeductionValue),
                ToCustomer(PostingKeys.AccountsReceivable, -salesReturn.DueAdjusted, salesReturn.CustomerId),
                ToCustomer(PostingKeys.CustomerAdvance, -salesReturn.AdvanceAmount, salesReturn.CustomerId),
                refund,
                JournalEntryLine.Debit(PostingKeys.Inventory, salesReturn.TotalCost - salesReturn.LossAmount),
                JournalEntryLine.Debit(PostingKeys.StockLoss, salesReturn.LossAmount),
                JournalEntryLine.Credit(PostingKeys.CostOfGoodsSold, salesReturn.TotalCost),
            ]), cancellationToken);
        if (transaction is not null) transaction.Journal = journal;
        return problem;
    }

    public static async Task<ProblemHttpResult?> CollectionAsync(
        ErpDbContext db, ICurrentContext current, SalesCollection collection, CancellationToken cancellationToken)
    {
        var receipt = await PaymentSources.DebitAsync(
            db, collection.PaymentMethodId, collection.BankAccountId, collection.CollectionAmount, cancellationToken);
        JournalEntryLine[] lines = collection.IsAdvance
            ? [receipt, ToCustomer(PostingKeys.CustomerAdvance, -collection.CollectionAmount, collection.CustomerId)]
            :
            [
                receipt,
                JournalEntryLine.Debit(PostingKeys.VdsReceivable, collection.Vds),
                JournalEntryLine.Debit(PostingKeys.AitReceivable, collection.Ait),
                JournalEntryLine.Debit(PostingKeys.SalesDiscount, collection.DiscountAmount),
                JournalEntryLine.Debit(PostingKeys.BankCharge, collection.ExpenseAmount),
                ToCustomer(PostingKeys.CustomerAdvance, collection.AdvanceAdjusted, collection.CustomerId),
                ToCustomer(PostingKeys.AccountsReceivable, -collection.SettledAmount, collection.CustomerId),
            ];
        var narration = collection.IsAdvance ? $"Customer advance {collection.Code}" : $"Collected from customer {collection.Code}";
        var (_, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            SalesMenu.SalesCollection, collection.Id, collection.Code, collection.CollectionDate, narration, lines), cancellationToken);
        return problem;
    }

    private static JournalEntryLine ToCustomer(string key, decimal amount, int customerId) =>
        JournalEntryLine.Debit(key, amount) with { CustomerId = customerId };

    private static JournalEntryLine Charge(int? accountId, decimal amount) =>
        accountId is { } id ? JournalEntryLine.Account(id, -amount) : JournalEntryLine.Credit(PostingKeys.OtherIncome, amount);
}
