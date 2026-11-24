using Erp.Api.Accounting;
using Erp.Api.Inventory;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Procurement;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement;

internal static class PurchasePosting
{
    public static async Task<ProblemHttpResult?> ReceiveAsync(
        ErpDbContext db, ICurrentContext current, PurchaseReceive receive, CancellationToken cancellationToken)
    {
        await db.Entry(receive).Collection(r => r.Lines).LoadAsync(cancellationToken);
        var itemIds = receive.Lines.Select(l => l.ItemId).ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var lines = receive.Lines.OrderBy(l => l.Sequence).ToList();

        var rebate = lines.Sum(l => items[l.ItemId].IsVatRebatable ? l.VatAmount : 0);
        var shared = receive.NetAmount - receive.ItemTotalAmount;
        var left = shared;
        var stock = lines.Select((line, index) =>
        {
            var share = index == lines.Count - 1
                ? left
                : receive.ItemTotalAmount == 0 ? 0 : Math.Round(shared * line.NetAmount / receive.ItemTotalAmount, 2);
            left -= share;
            var value = line.NetAmount - (items[line.ItemId].IsVatRebatable ? line.VatAmount : 0) + share;
            return StockEntryLine.In(line.ItemId, line.UomId, receive.WarehouseId, line.Quantity, value);
        }).ToList();

        var (transaction, refused) = await StockPosting.PostAsync(db, current, new StockEntry(
            ProcurementMenu.PurchaseReceive, receive.Id, receive.Code, receive.ReceiveDate, receive.SupplierId, receive.Remarks, stock),
            cancellationToken);
        if (transaction is null) return refused;
        foreach (var line in stock) items[line.ItemId].LastCostPrice = Math.Round(line.Value / line.Quantity, 4);

        var (journal, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            ProcurementMenu.PurchaseReceive, receive.Id, receive.Code, receive.ReceiveDate, $"Goods received {receive.Code}",
            [
                JournalEntryLine.Debit(PostingKeys.Inventory, stock.Sum(l => l.Value)),
                JournalEntryLine.Debit(PostingKeys.VatReceivable, rebate),
                JournalEntryLine.Credit(PostingKeys.AccountsPayable, receive.NetAmount, receive.SupplierId),
            ]), cancellationToken);
        transaction.Journal = journal;
        return problem;
    }

    public static async Task<ProblemHttpResult?> ReturnAsync(
        ErpDbContext db, ICurrentContext current, PurchaseReturn purchaseReturn, CancellationToken cancellationToken)
    {
        await db.Entry(purchaseReturn).Collection(r => r.Lines).LoadAsync(cancellationToken);
        var itemIds = purchaseReturn.Lines.Select(l => l.ItemId).ToList();
        var rebatable = await db.Items.Where(i => itemIds.Contains(i.Id) && i.IsVatRebatable).Select(i => i.Id).ToListAsync(cancellationToken);
        var stock = purchaseReturn.Lines.OrderBy(l => l.Sequence)
            .Select(l => StockEntryLine.Out(l.ItemId, l.UomId, purchaseReturn.WarehouseId, l.Quantity))
            .ToList();

        var (transaction, refused) = await StockPosting.PostAsync(db, current, new StockEntry(
            ProcurementMenu.PurchaseReturn, purchaseReturn.Id, purchaseReturn.Code, purchaseReturn.ReturnDate, purchaseReturn.SupplierId,
            purchaseReturn.Remarks, stock), cancellationToken);
        if (transaction is null) return refused;

        var cost = -transaction.Lines.Sum(l => l.Value);
        var rebate = purchaseReturn.Lines.Where(l => rebatable.Contains(l.ItemId)).Sum(l => l.VatAmount);
        var (journal, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            ProcurementMenu.PurchaseReturn, purchaseReturn.Id, purchaseReturn.Code, purchaseReturn.ReturnDate,
            $"Goods returned {purchaseReturn.Code}",
            [
                JournalEntryLine.Debit(PostingKeys.AccountsPayable, purchaseReturn.NetAmount, purchaseReturn.SupplierId),
                JournalEntryLine.Credit(PostingKeys.Inventory, cost),
                JournalEntryLine.Credit(PostingKeys.VatReceivable, rebate),
                JournalEntryLine.Credit(PostingKeys.PriceDifference, purchaseReturn.NetAmount - cost - rebate),
            ]), cancellationToken);
        transaction.Journal = journal;
        return problem;
    }

    public static async Task<ProblemHttpResult?> PaymentAsync(
        ErpDbContext db, ICurrentContext current, PurchasePayment payment, CancellationToken cancellationToken)
    {
        var method = await db.LookupValues.Where(v => v.Id == payment.PaymentMethodId).Select(v => v.Code).FirstOrDefaultAsync(cancellationToken);
        var source = method switch
        {
            null or ProcurementLookups.Cash => PostingKeys.Cash,
            ProcurementLookups.MobileBanking => PostingKeys.MobileBanking,
            _ => PostingKeys.Bank,
        };
        var settled = payment.PaymentAmount + payment.Vds + payment.Tds + payment.DiscountAmount;
        var (_, problem) = await AccountPosting.PostAsync(db, current, new JournalEntry(
            ProcurementMenu.PurchasePayment, payment.Id, payment.Code, payment.PaymentDate, $"Supplier paid {payment.Code}",
            [
                JournalEntryLine.Debit(PostingKeys.AccountsPayable, settled, payment.SupplierId),
                JournalEntryLine.Credit(source, payment.PaymentAmount),
                JournalEntryLine.Credit(PostingKeys.VdsPayable, payment.Vds),
                JournalEntryLine.Credit(PostingKeys.TdsPayable, payment.Tds),
                JournalEntryLine.Credit(PostingKeys.PurchaseDiscount, payment.DiscountAmount),
            ]), cancellationToken);
        return problem;
    }
}
