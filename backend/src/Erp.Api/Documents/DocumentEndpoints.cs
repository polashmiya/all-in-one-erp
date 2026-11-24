using Erp.Api.Inventory;
using Erp.Api.Procurement;
using Erp.Core.Authorization;
using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Documents;

public sealed class DocumentEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        erp.MapGet("/documents/items", (string? search, ErpDbContext db, CancellationToken cancellationToken) =>
                DocumentData.ItemsAsync(db, search, cancellationToken))
            .WithName("GetDocumentItemOptions")
            .WithTags("Documents")
            .WithSummary("Up to 20 active items matching the search, with their UOM, last cost and purchase VAT")
            .RequireAnyPermission([.. ProcurementModule.Editors, .. StockModule.Readers]);
    }
}
