using Erp.Api.Inventory;
using Erp.Api.Procurement;
using Erp.Api.Sales;
using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Documents;

public sealed class DocumentEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        erp.MapGet("/documents/items", (string? search, bool? saleable, ErpDbContext db, CancellationToken cancellationToken) =>
                DocumentData.ItemsAsync(db, search, saleable, cancellationToken))
            .WithName("GetDocumentItemOptions")
            .WithTags("Documents")
            .WithSummary("Up to 20 active items matching the search, with their UOM, last cost, purchase VAT, sales price, sales VAT and SD")
            .RequireAnyPermission([.. ProcurementModule.Editors, .. StockModule.Readers, .. SalesModule.Editors]);
    }
}
