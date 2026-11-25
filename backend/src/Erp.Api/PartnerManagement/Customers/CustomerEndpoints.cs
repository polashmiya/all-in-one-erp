using Erp.Api.Sales;
using Erp.Api.UserManagement.Employees;
using Erp.Core.Authorization;
using Erp.Core.Domain.Partners;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.PartnerManagement.Customers;

internal static class CustomerEndpoints
{
    private const string Feature = PartnerManagementMenu.Customer;
    private const string CodePrefix = "CUS-";

    public static void Map(IEndpointRouteBuilder group)
    {
        var customers = group.MapGroup("/customers");

        customers.MapGet("", List)
            .WithName("ListCustomers")
            .WithSummary("Customers of the selected business unit")
            .RequirePermission($"{Feature}.view");

        customers.MapGet("/options", Options)
            .WithName("GetCustomerOptions")
            .WithSummary("Customers of the selected business unit with their contact details, for dropdowns")
            .RequireAnyPermission([$"{Feature}.view", .. SalesModule.Editors]);

        customers.MapGet("/{id:int}", Get)
            .WithName("GetCustomer")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        customers.MapPost("", Create)
            .WithName("CreateCustomer")
            .WithSummary("Create a customer; an empty code gets the next CUS- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveCustomerRequest>();

        customers.MapPut("/{id:int}", Update)
            .WithName("UpdateCustomer")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveCustomerRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        customers.MapPost("/status", SetStatus)
            .WithName("SetCustomersStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<PartnerStatusRequest>();
    }

    private static async Task<CustomerList> List(
        string? search,
        string? status,
        int? groupId,
        int? priceTypeId,
        int? districtId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var customers = PartnerData.Search(db.Customers.AsNoTracking(), search);
        if (groupId is not null) customers = customers.Where(c => c.GroupId == groupId);
        if (priceTypeId is not null) customers = customers.Where(c => c.PriceTypeId == priceTypeId);
        if (districtId is not null) customers = customers.Where(c => c.DistrictId == districtId);

        var (filtered, counts) = await PartnerData.ByStatusAsync(customers, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var rows = await PartnerData.Page(filtered, sort, page, pageSize)
            .Select(c => new
            {
                Row = new CustomerRow(
                    c.Id,
                    c.Code,
                    c.Name,
                    c.BillingName,
                    c.GroupId,
                    db.LookupValues.Where(v => v.Id == c.GroupId).Select(v => v.NameEn).FirstOrDefault(),
                    c.PriceTypeId,
                    db.LookupValues.Where(v => v.Id == c.PriceTypeId).Select(v => v.NameEn).FirstOrDefault(),
                    c.Mobile,
                    c.DistrictId,
                    db.Districts.Where(d => d.Id == c.DistrictId).Select(d => d.Name).FirstOrDefault(),
                    c.CreditLimit,
                    c.SalesRepresentativeId,
                    db.Employees.Where(e => e.Id == c.SalesRepresentativeId).Select(e => e.Name).FirstOrDefault(),
                    c.IsActive,
                    c.CreatedAt,
                    c.ModifiedAt,
                    null),
                c.CustomData,
            })
            .ToListAsync(cancellationToken);

        return new CustomerList(rows.Select(r => r.Row with { CustomData = CustomFieldData.Parse(r.CustomData) }).ToList(), total, counts);
    }

    private static Task<List<CustomerOption>> Options(ErpDbContext db, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerOption(
                c.Id, c.Code, c.Name, c.ContactPerson, c.ContactMobile ?? c.Mobile, c.Address, c.SalesRepresentativeId, c.IsActive))
            .ToListAsync(cancellationToken);

    private static async Task<Results<Ok<CustomerDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<CustomerDetail>, NotFound, ValidationProblem>> Create(
        SaveCustomerRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
            Name = "",
        };
        return SaveAsync(customer, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<CustomerDetail>, NotFound, ValidationProblem>> Update(
        int id, SaveCustomerRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer is null) return TypedResults.NotFound();
        return await SaveAsync(customer, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<NoContent, ValidationProblem>> SetStatus(
        PartnerStatusRequest request, ErpDbContext db, CancellationToken cancellationToken) =>
        await PartnerData.SetStatusAsync(db.Customers, request, db, cancellationToken) is { } problem
            ? problem
            : TypedResults.NoContent();

    private static async Task<Results<Ok<CustomerDetail>, NotFound, ValidationProblem>> SaveAsync(
        Customer customer, SaveCustomerRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = customer.Id > 0 ? ToRequest(customer) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await PartnerData.CodeAsync(db.Customers, customer, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("groupId", request.GroupId, existing?.GroupId, PartnerLookups.CustomerGroup),
            ("priceTypeId", request.PriceTypeId, existing?.PriceTypeId, PartnerLookups.PriceType),
            ("territoryId", request.TerritoryId, existing?.TerritoryId, PartnerLookups.Territory),
            ("genderId", request.GenderId, existing?.GenderId, EmployeeLookups.Gender),
        ], errors, cancellationToken);
        await PartnerData.CheckPlaceAsync(db, request, errors, cancellationToken);

        if (request.SalesRepresentativeId is { } rep && rep != existing?.SalesRepresentativeId &&
            !await db.Employees.AnyAsync(e => e.Id == rep && e.IsActive, cancellationToken))
        {
            errors["salesRepresentativeId"] = ["Choose an active employee."];
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        if (customer.Id == 0) db.Customers.Add(customer);
        customer.Code = code ?? await PartnerData.NextCodeAsync(db.Customers, CodePrefix, cancellationToken);
        PartnerData.Apply(customer, request);
        customer.PriceTypeId = request.PriceTypeId;
        customer.TerritoryId = request.TerritoryId;
        customer.SalesRepresentativeId = request.SalesRepresentativeId;
        customer.DateOfBirth = request.DateOfBirth;
        customer.GenderId = request.GenderId;
        customer.Profession = PartnerData.Trimmed(request.Profession);
        customer.MarriageDate = request.MarriageDate;

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, customer.Id, cancellationToken))!);
    }

    private static SaveCustomerRequest ToRequest(Customer c) => new()
    {
        Code = c.Code,
        Name = c.Name,
        BillingName = c.BillingName,
        GroupId = c.GroupId,
        PriceTypeId = c.PriceTypeId,
        Mobile = c.Mobile,
        Email = c.Email,
        ContactPerson = c.ContactPerson,
        ContactMobile = c.ContactMobile,
        Address = c.Address,
        City = c.City,
        DistrictId = c.DistrictId,
        ThanaId = c.ThanaId,
        TerritoryId = c.TerritoryId,
        IsForeign = c.IsForeign,
        CreditLimit = c.CreditLimit,
        PaymentDays = c.PaymentDays,
        SalesRepresentativeId = c.SalesRepresentativeId,
        BinNumber = c.BinNumber,
        TinNumber = c.TinNumber,
        TradeLicenseNumber = c.TradeLicenseNumber,
        Nid = c.Nid,
        CentralVatRegistrationNo = c.CentralVatRegistrationNo,
        IsVatNotApplicable = c.IsVatNotApplicable,
        IsVdsApplicable = c.IsVdsApplicable,
        DateOfBirth = c.DateOfBirth,
        GenderId = c.GenderId,
        Profession = c.Profession,
        MarriageDate = c.MarriageDate,
        Remarks = c.Remarks,
        IsActive = c.IsActive,
        CustomData = CustomFieldData.Parse(c.CustomData),
    };

    private static async Task<CustomerDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var c = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [c.GroupId, c.PriceTypeId, c.TerritoryId, c.GenderId], cancellationToken);
        var place = await PartnerData.PlaceNamesAsync(db, c.DistrictId, c.ThanaId, cancellationToken);
        string? Lookup(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;

        return new CustomerDetail
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            BillingName = c.BillingName,
            GroupId = c.GroupId,
            GroupName = Lookup(c.GroupId),
            PriceTypeId = c.PriceTypeId,
            PriceTypeName = Lookup(c.PriceTypeId),
            Mobile = c.Mobile,
            Email = c.Email,
            ContactPerson = c.ContactPerson,
            ContactMobile = c.ContactMobile,
            Address = c.Address,
            City = c.City,
            DistrictId = c.DistrictId,
            DistrictName = place.District,
            ThanaId = c.ThanaId,
            ThanaName = place.Thana,
            TerritoryId = c.TerritoryId,
            TerritoryName = Lookup(c.TerritoryId),
            IsForeign = c.IsForeign,
            CreditLimit = c.CreditLimit,
            PaymentDays = c.PaymentDays,
            SalesRepresentativeId = c.SalesRepresentativeId,
            SalesRepresentativeName = await db.Employees.Where(e => e.Id == c.SalesRepresentativeId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken),
            BinNumber = c.BinNumber,
            TinNumber = c.TinNumber,
            TradeLicenseNumber = c.TradeLicenseNumber,
            Nid = c.Nid,
            CentralVatRegistrationNo = c.CentralVatRegistrationNo,
            IsVatNotApplicable = c.IsVatNotApplicable,
            IsVdsApplicable = c.IsVdsApplicable,
            DateOfBirth = c.DateOfBirth,
            GenderId = c.GenderId,
            GenderName = Lookup(c.GenderId),
            Profession = c.Profession,
            MarriageDate = c.MarriageDate,
            Remarks = c.Remarks,
            IsActive = c.IsActive,
            CustomData = CustomFieldData.Parse(c.CustomData),
            CreatedAt = c.CreatedAt,
            ModifiedAt = c.ModifiedAt,
        };
    }
}
