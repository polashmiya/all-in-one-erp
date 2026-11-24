using Erp.Core.Domain.Approvals;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Procurement;

public static class PostingBackfill
{
    private sealed record UnitContext(Guid? AccountId, Guid? BusinessUnitId) : ICurrentContext
    {
        public bool IsAuthenticated => false;

        public Guid? UserId => null;

        public bool IsAccountAdmin => false;
    }

    public static async Task PostEarlierPurchasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ErpDbContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PostingBackfill));
        var units = await scope.ServiceProvider.GetRequiredService<ErpDbContext>().BusinessUnits.AcrossTenants()
            .Select(u => new UnitContext(u.AccountId, u.Id))
            .ToListAsync(cancellationToken);

        foreach (var unit in units)
        {
            await using var db = new ErpDbContext(options, unit);
            await PostUnitAsync(db, unit, logger, cancellationToken);
        }
    }

    private static async Task PostUnitAsync(ErpDbContext db, ICurrentContext unit, ILogger logger, CancellationToken cancellationToken)
    {
        var receives = await db.PurchaseReceives
            .Where(r => r.IsActive && r.Status == ApprovalStatus.Approved &&
                !db.InventoryTransactions.Any(t => t.Feature == ProcurementMenu.PurchaseReceive && t.DocumentId == r.Id))
            .Select(r => new { r.Id, r.Code, Date = r.ReviewedAt, Feature = ProcurementMenu.PurchaseReceive })
            .ToListAsync(cancellationToken);
        var returns = await db.PurchaseReturns
            .Where(r => r.IsActive && r.Status == ApprovalStatus.Approved &&
                !db.InventoryTransactions.Any(t => t.Feature == ProcurementMenu.PurchaseReturn && t.DocumentId == r.Id))
            .Select(r => new { r.Id, r.Code, Date = r.ReviewedAt, Feature = ProcurementMenu.PurchaseReturn })
            .ToListAsync(cancellationToken);
        var payments = await db.PurchasePayments
            .Where(p => p.IsActive && p.Status == ApprovalStatus.Approved &&
                !db.Journals.Any(j => j.Feature == ProcurementMenu.PurchasePayment && j.DocumentId == p.Id))
            .Select(p => new { p.Id, p.Code, Date = p.ReviewedAt, Feature = ProcurementMenu.PurchasePayment })
            .ToListAsync(cancellationToken);

        foreach (var document in receives.Concat(returns).Concat(payments).OrderBy(d => d.Date))
        {
            ProblemHttpResult? problem = document.Feature switch
            {
                ProcurementMenu.PurchaseReceive => await PurchasePosting.ReceiveAsync(
                    db, unit, await db.PurchaseReceives.SingleAsync(r => r.Id == document.Id, cancellationToken), cancellationToken),
                ProcurementMenu.PurchaseReturn => await PurchasePosting.ReturnAsync(
                    db, unit, await db.PurchaseReturns.SingleAsync(r => r.Id == document.Id, cancellationToken), cancellationToken),
                _ => await PurchasePosting.PaymentAsync(
                    db, unit, await db.PurchasePayments.SingleAsync(p => p.Id == document.Id, cancellationToken), cancellationToken),
            };
            if (problem is null) await db.SaveChangesAsync(cancellationToken);
            else logger.LogWarning("{Code} was not posted: {Reason}", document.Code, problem.ProblemDetails.Detail);
            db.ChangeTracker.Clear();
        }
    }
}
