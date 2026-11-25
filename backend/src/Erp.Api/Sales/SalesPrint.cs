using Erp.Core.Authorization;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales;

public sealed record PrintCompany(
    string Name, string UnitName, string? Address, string? Phone, string? Email, string? Website, string? TaxId);

public sealed record PrintCustomer(
    string Code, string Name, string? ContactPerson, string? Phone, string? Address, string? BinNumber, string? TinNumber);

public sealed record PrintParties(PrintCompany Company, PrintCustomer Customer);

internal static class SalesPrint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/print-parties/{customerId:int}", Parties)
            .WithName("GetPrintParties")
            .WithSummary("Company and customer details for the head of a printed sales document")
            .RequireAnyPermission(SalesModule.Readers)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<PrintParties>, NotFound>> Parties(
        int customerId, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var company = await db.BusinessUnits.AsNoTracking()
            .Where(u => u.Id == current.BusinessUnitId)
            .Select(u => new PrintCompany(
                u.Account!.Name, u.Name, u.Address ?? u.Account.Address, u.Phone ?? u.Account.Phone, u.Email ?? u.Account.Email,
                u.Account.Website, u.Account.TaxId))
            .SingleAsync(cancellationToken);
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new PrintCustomer(
                c.Code, c.BillingName ?? c.Name, c.ContactPerson, c.Mobile ?? c.ContactMobile, c.Address, c.BinNumber, c.TinNumber))
            .SingleOrDefaultAsync(cancellationToken);
        return customer is null ? TypedResults.NotFound() : TypedResults.Ok(new PrintParties(company, customer));
    }
}
