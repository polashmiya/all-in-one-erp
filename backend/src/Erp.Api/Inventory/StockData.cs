using Erp.Api.Documents;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Inventory;

public sealed record StockOnHand(int ItemId, decimal Quantity, decimal Cost);

internal static class StockData
{
    public static Task<Dictionary<int, StockOnHand>> OnHandAsync(
        ErpDbContext db, Guid warehouseId, IReadOnlyCollection<int> itemIds, CancellationToken cancellationToken) =>
        db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == warehouseId && itemIds.Contains(b.ItemId))
            .Select(b => new StockOnHand(b.ItemId, b.Quantity, b.Quantity > 0 ? Math.Round(b.Value / b.Quantity, 4) : 0))
            .ToDictionaryAsync(b => b.ItemId, cancellationToken);

    public static void CheckEnough(
        IEnumerable<(int ItemId, decimal Quantity)> lines, Dictionary<int, DocumentItemOption> items,
        Dictionary<int, StockOnHand> onHand, Dictionary<string, string[]> errors)
    {
        var problems = lines
            .Where(l => l.Quantity > (onHand.GetValueOrDefault(l.ItemId)?.Quantity ?? 0))
            .Select(l => $"Only {onHand.GetValueOrDefault(l.ItemId)?.Quantity ?? 0:0.####} of {items[l.ItemId].Code} is in stock.")
            .ToArray();
        if (problems.Length > 0) errors["lines"] = problems;
    }
}
