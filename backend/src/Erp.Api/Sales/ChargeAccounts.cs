using Erp.Core.Authorization;
using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales;

public sealed record ChargeAccountOption(int Id, string Code, string Name, bool IsActive);

internal static class ChargeAccounts
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/charge-accounts", Options)
            .WithName("GetChargeAccountOptions")
            .WithSummary("Income accounts that can take a delivery or extra charge")
            .RequireAnyPermission(SalesModule.Editors);

    public static IQueryable<ChartOfAccount> Choices(ErpDbContext db) =>
        db.ChartOfAccounts.AsNoTracking().Where(a => a.IsActive && !a.IsGroup && a.Type == AccountType.Income);

    private static Task<List<ChargeAccountOption>> Options(ErpDbContext db, CancellationToken cancellationToken) =>
        Choices(db)
            .OrderBy(a => a.Code)
            .Select(a => new ChargeAccountOption(a.Id, a.Code, a.Name, a.IsActive))
            .ToListAsync(cancellationToken);
}
