using Erp.Core.Domain.Inventory;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Inventory;

internal sealed record StockEntry(
    string Feature, int DocumentId, string DocumentCode, DateOnly Date, int? SupplierId, string? Remarks, IReadOnlyList<StockEntryLine> Lines);

internal sealed record StockEntryLine(int ItemId, int UomId, Guid WarehouseId, decimal Quantity, decimal Value = 0, Guid? ToWarehouseId = null)
{
    public static StockEntryLine In(int itemId, int uomId, Guid warehouseId, decimal quantity, decimal value) =>
        new(itemId, uomId, warehouseId, quantity, value);

    public static StockEntryLine Out(int itemId, int uomId, Guid warehouseId, decimal quantity) =>
        new(itemId, uomId, warehouseId, -quantity);

    public static StockEntryLine Move(int itemId, int uomId, Guid from, Guid to, decimal quantity) =>
        new(itemId, uomId, from, -quantity, 0, to);
}

internal static class StockPosting
{
    public static async Task<(InventoryTransaction? Transaction, ProblemHttpResult? Problem)> PostAsync(
        ErpDbContext db, ICurrentContext current, StockEntry entry, CancellationToken cancellationToken)
    {
        var itemIds = entry.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var balances = await db.StockBalances.Where(b => itemIds.Contains(b.ItemId)).ToListAsync(cancellationToken);
        StockBalance Balance(int itemId, Guid warehouseId)
        {
            var balance = balances.Find(b => b.ItemId == itemId && b.WarehouseId == warehouseId);
            if (balance is not null) return balance;
            balance = new StockBalance
            {
                AccountId = current.AccountId!.Value,
                BusinessUnitId = current.BusinessUnitId!.Value,
                WarehouseId = warehouseId,
                ItemId = itemId,
            };
            balances.Add(balance);
            db.StockBalances.Add(balance);
            return balance;
        }

        var transaction = new InventoryTransaction
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Feature = entry.Feature,
            DocumentId = entry.DocumentId,
            DocumentCode = entry.DocumentCode,
            PostingDate = entry.Date,
            SupplierId = entry.SupplierId,
            Remarks = entry.Remarks,
        };
        var problems = new List<string>();
        void Add(StockEntryLine line, Guid warehouseId, decimal quantity, decimal value)
        {
            var balance = Balance(line.ItemId, warehouseId);
            balance.Quantity += quantity;
            balance.Value += value;
            transaction.Lines.Add(new InventoryLine
            {
                Sequence = transaction.Lines.Count + 1,
                ItemId = line.ItemId,
                UomId = line.UomId,
                WarehouseId = warehouseId,
                Quantity = quantity,
                Value = value,
            });
        }

        foreach (var line in entry.Lines)
        {
            if (line.Quantity >= 0)
            {
                Add(line, line.WarehouseId, line.Quantity, line.Value);
                continue;
            }

            var balance = Balance(line.ItemId, line.WarehouseId);
            var quantity = -line.Quantity;
            if (balance.Quantity < quantity)
            {
                problems.Add($"Only {Math.Max(0, balance.Quantity):0.####} of {items[line.ItemId].Code} is in stock.");
                continue;
            }

            var value = quantity == balance.Quantity ? balance.Value : Math.Round(balance.Value / balance.Quantity * quantity, 2);
            Add(line, line.WarehouseId, -quantity, -value);
            if (line.ToWarehouseId is { } to) Add(line, to, quantity, value);
        }

        if (problems.Count > 0)
        {
            return (null, TypedResults.Problem(
                title: "Not enough stock",
                detail: string.Join(" ", problems.Distinct()),
                statusCode: StatusCodes.Status409Conflict));
        }

        foreach (var item in items.Values)
        {
            var own = balances.Where(b => b.ItemId == item.Id).ToList();
            var quantity = own.Sum(b => b.Quantity);
            if (quantity > 0) item.AverageCostPrice = Math.Round(own.Sum(b => b.Value) / quantity, 4);
        }

        db.InventoryTransactions.Add(transaction);
        return (transaction, null);
    }
}
