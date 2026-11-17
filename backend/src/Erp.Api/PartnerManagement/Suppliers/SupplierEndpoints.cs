using Erp.Core.Authorization;
using Erp.Core.Domain.Partners;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.PartnerManagement.Suppliers;

internal static class SupplierEndpoints
{
    private const string Feature = PartnerManagementMenu.Supplier;
    private const string CodePrefix = "SUP-";

    public static void Map(IEndpointRouteBuilder group)
    {
        var suppliers = group.MapGroup("/suppliers");

        suppliers.MapGet("", List)
            .WithName("ListSuppliers")
            .WithSummary("Suppliers of the selected business unit")
            .RequirePermission($"{Feature}.view");

        suppliers.MapGet("/options", Options)
            .WithName("GetSupplierOptions")
            .WithSummary("Suppliers of the selected business unit, for dropdowns")
            .RequireAnyPermission(
                $"{Feature}.view",
                $"{ItemManagementMenu.ItemProfile}.view",
                $"{ItemManagementMenu.ItemProfile}.create",
                $"{ItemManagementMenu.ItemProfile}.edit");

        suppliers.MapGet("/{id:int}", Get)
            .WithName("GetSupplier")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        suppliers.MapPost("", Create)
            .WithName("CreateSupplier")
            .WithSummary("Create a supplier; an empty code gets the next SUP- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSupplierRequest>();

        suppliers.MapPut("/{id:int}", Update)
            .WithName("UpdateSupplier")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSupplierRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        suppliers.MapPost("/status", SetStatus)
            .WithName("SetSuppliersStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<PartnerStatusRequest>();
    }

    private static async Task<SupplierList> List(
        string? search,
        string? status,
        int? groupId,
        int? districtId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var suppliers = PartnerData.Search(db.Suppliers.AsNoTracking(), search);
        if (groupId is not null) suppliers = suppliers.Where(s => s.GroupId == groupId);
        if (districtId is not null) suppliers = suppliers.Where(s => s.DistrictId == districtId);

        var (filtered, counts) = await PartnerData.ByStatusAsync(suppliers, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var rows = await PartnerData.Page(filtered, sort, page, pageSize)
            .Select(s => new
            {
                Row = new SupplierRow(
                    s.Id,
                    s.Code,
                    s.Name,
                    s.BillingName,
                    s.GroupId,
                    db.LookupValues.Where(v => v.Id == s.GroupId).Select(v => v.NameEn).FirstOrDefault(),
                    s.ContactPerson,
                    s.Mobile,
                    s.DistrictId,
                    db.Districts.Where(d => d.Id == s.DistrictId).Select(d => d.Name).FirstOrDefault(),
                    s.PaymentDays,
                    s.IsForeign,
                    s.IsActive,
                    s.CreatedAt,
                    s.ModifiedAt,
                    null),
                s.CustomData,
            })
            .ToListAsync(cancellationToken);

        return new SupplierList(rows.Select(r => r.Row with { CustomData = CustomFieldData.Parse(r.CustomData) }).ToList(), total, counts);
    }

    private static Task<List<SupplierOption>> Options(ErpDbContext db, CancellationToken cancellationToken) =>
        db.Suppliers.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SupplierOption(s.Id, s.Code, s.Name, s.IsActive))
            .ToListAsync(cancellationToken);

    private static async Task<Results<Ok<SupplierDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SupplierDetail>, NotFound, ValidationProblem>> Create(
        SaveSupplierRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
            Name = "",
        };
        return SaveAsync(supplier, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SupplierDetail>, NotFound, ValidationProblem>> Update(
        int id, SaveSupplierRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (supplier is null) return TypedResults.NotFound();
        return await SaveAsync(supplier, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<NoContent, ValidationProblem>> SetStatus(
        PartnerStatusRequest request, ErpDbContext db, CancellationToken cancellationToken) =>
        await PartnerData.SetStatusAsync(db.Suppliers, request, db, cancellationToken) is { } problem
            ? problem
            : TypedResults.NoContent();

    private static async Task<Results<Ok<SupplierDetail>, NotFound, ValidationProblem>> SaveAsync(
        Supplier supplier, SaveSupplierRequest sent, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var existing = supplier.Id > 0 ? ToRequest(supplier) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = await PartnerData.CodeAsync(db.Suppliers, supplier, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db, [("groupId", request.GroupId, existing?.GroupId, PartnerLookups.SupplierGroup)], errors, cancellationToken);
        await PartnerData.CheckPlaceAsync(db, request, errors, cancellationToken);

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        if (supplier.Id == 0) db.Suppliers.Add(supplier);
        supplier.Code = code ?? await PartnerData.NextCodeAsync(db.Suppliers, CodePrefix, cancellationToken);
        PartnerData.Apply(supplier, request);
        supplier.Tolerance = request.Tolerance;

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, supplier.Id, cancellationToken))!);
    }

    private static SaveSupplierRequest ToRequest(Supplier s) => new()
    {
        Code = s.Code,
        Name = s.Name,
        BillingName = s.BillingName,
        GroupId = s.GroupId,
        Mobile = s.Mobile,
        Email = s.Email,
        ContactPerson = s.ContactPerson,
        ContactMobile = s.ContactMobile,
        Address = s.Address,
        City = s.City,
        DistrictId = s.DistrictId,
        ThanaId = s.ThanaId,
        IsForeign = s.IsForeign,
        CreditLimit = s.CreditLimit,
        PaymentDays = s.PaymentDays,
        Tolerance = s.Tolerance,
        BinNumber = s.BinNumber,
        TinNumber = s.TinNumber,
        TradeLicenseNumber = s.TradeLicenseNumber,
        Nid = s.Nid,
        CentralVatRegistrationNo = s.CentralVatRegistrationNo,
        IsVatNotApplicable = s.IsVatNotApplicable,
        IsVdsApplicable = s.IsVdsApplicable,
        Remarks = s.Remarks,
        IsActive = s.IsActive,
        CustomData = CustomFieldData.Parse(s.CustomData),
    };

    private static async Task<SupplierDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var s = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [s.GroupId], cancellationToken);
        var place = await PartnerData.PlaceNamesAsync(db, s.DistrictId, s.ThanaId, cancellationToken);
        string? Lookup(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;

        return new SupplierDetail
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            BillingName = s.BillingName,
            GroupId = s.GroupId,
            GroupName = Lookup(s.GroupId),
            Mobile = s.Mobile,
            Email = s.Email,
            ContactPerson = s.ContactPerson,
            ContactMobile = s.ContactMobile,
            Address = s.Address,
            City = s.City,
            DistrictId = s.DistrictId,
            DistrictName = place.District,
            ThanaId = s.ThanaId,
            ThanaName = place.Thana,
            IsForeign = s.IsForeign,
            CreditLimit = s.CreditLimit,
            PaymentDays = s.PaymentDays,
            Tolerance = s.Tolerance,
            BinNumber = s.BinNumber,
            TinNumber = s.TinNumber,
            TradeLicenseNumber = s.TradeLicenseNumber,
            Nid = s.Nid,
            CentralVatRegistrationNo = s.CentralVatRegistrationNo,
            IsVatNotApplicable = s.IsVatNotApplicable,
            IsVdsApplicable = s.IsVdsApplicable,
            Remarks = s.Remarks,
            IsActive = s.IsActive,
            CustomData = CustomFieldData.Parse(s.CustomData),
            CreatedAt = s.CreatedAt,
            ModifiedAt = s.ModifiedAt,
        };
    }
}
