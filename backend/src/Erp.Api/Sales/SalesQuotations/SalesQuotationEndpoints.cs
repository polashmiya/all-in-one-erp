using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Core.Authorization;
using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Sales;
using Erp.Core.Modules;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales.SalesQuotations;

internal static class SalesQuotationEndpoints
{
    private const string Feature = SalesMenu.SalesQuotation;
    private const string CodePrefix = "SQ-";
    private const string FirstVersion = "v1";

    public static readonly string[] Editors = [$"{Feature}.view", $"{Feature}.create", $"{Feature}.edit"];

    public static void Map(IEndpointRouteBuilder group)
    {
        var quotations = group.MapGroup("/sales-quotations");

        quotations.MapGet("", List)
            .WithName("ListSalesQuotations")
            .WithSummary("Sales quotations of the selected business unit with counts by status")
            .RequirePermission($"{Feature}.view");

        quotations.MapGet("/{id:int}", Get)
            .WithName("GetSalesQuotation")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        quotations.MapPost("", Create)
            .WithName("CreateSalesQuotation")
            .WithSummary("Create a pending sales quotation; an empty number gets the next SQ- number")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveSalesQuotationRequest>();

        quotations.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesQuotation")
            .WithSummary("Change a pending sales quotation")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveSalesQuotationRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapPost("/{id:int}/revise", Revise)
            .WithName("ReviseSalesQuotation")
            .WithSummary("Copy a reviewed sales quotation to its next version; an approved one is closed")
            .RequirePermission($"{Feature}.create")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapGet("/{id:int}/approval", Approval)
            .WithName("GetSalesQuotationApproval")
            .WithSummary("Approval steps of a sales quotation and whether the current user can act on the current one")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        quotations.MapPost("/{id:int}/approve", Approve)
            .WithName("ApproveSalesQuotation")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapPost("/{id:int}/reject", Reject)
            .WithName("RejectSalesQuotation")
            .WithSummary("Reject a pending sales quotation; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Approve}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapPost("/{id:int}/close", Close)
            .WithName("CloseSalesQuotation")
            .WithSummary("Close an approved sales quotation so nothing more is ordered from it")
            .RequirePermission($"{Feature}.edit")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelSalesQuotation")
            .WithSummary("Cancel an approved sales quotation; the comment is the reason and is required")
            .RequirePermission($"{Feature}.{FeatureActions.Cancel}")
            .Validate<ReviewRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        quotations.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesQuotation")
            .WithSummary("Delete a pending or rejected sales quotation")
            .RequirePermission($"{Feature}.delete")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<SalesQuotationList> List(
        string? search,
        string? status,
        int? customerId,
        int? quotationTypeId,
        Guid? officeId,
        int? salesForceId,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var quotations = db.SalesQuotations.AsNoTracking().Where(q => q.IsActive);
        if (DocumentData.Trimmed(search) is { } term)
        {
            quotations = quotations.Where(q => q.Code.Contains(term) || q.CustomerName.Contains(term) ||
                (q.Reference != null && q.Reference.Contains(term)) ||
                (q.Subjects != null && q.Subjects.Contains(term)) ||
                q.Lines.Any(l => l.ItemName.Contains(term) || (l.ItemCode != null && l.ItemCode.Contains(term))));
        }

        if (customerId is not null) quotations = quotations.Where(q => q.CustomerId == customerId);
        if (quotationTypeId is not null) quotations = quotations.Where(q => q.QuotationTypeId == quotationTypeId);
        if (officeId is not null) quotations = quotations.Where(q => q.OfficeId == officeId);
        if (salesForceId is not null) quotations = quotations.Where(q => q.SalesForceId == salesForceId);

        var (filtered, counts) = await DocumentData.ByStatusAsync(quotations, status, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var sorted = sort switch
        {
            "code" => filtered.OrderBy(q => q.Code).ThenBy(q => q.Id),
            "-code" => filtered.OrderByDescending(q => q.Code).ThenByDescending(q => q.Id),
            "quotationDate" => filtered.OrderBy(q => q.QuotationDate).ThenBy(q => q.Id),
            "validityDate" => filtered.OrderBy(q => q.ValidityDate),
            "-validityDate" => filtered.OrderByDescending(q => q.ValidityDate),
            _ => filtered.OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id),
        };

        var rows = await DocumentData.Page(sorted, page, pageSize)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.Version,
                q.QuotationDate,
                q.ValidityDate,
                q.IsExpired,
                q.CustomerId,
                q.CustomerName,
                q.QuotationTypeId,
                QuotationTypeName = db.LookupValues.Where(v => v.Id == q.QuotationTypeId).Select(v => v.NameEn).FirstOrDefault(),
                q.OfficeId,
                OfficeName = db.Offices.Where(o => o.Id == q.OfficeId).Select(o => o.Name).FirstOrDefault(),
                q.SalesForceId,
                q.SalesForceName,
                q.Status,
                ItemCount = q.Lines.Count,
                TotalQuantity = q.Lines.Sum(l => l.Quantity),
                q.TotalAmount,
                q.CreatedAt,
                q.ModifiedAt,
            })
            .ToListAsync(cancellationToken);

        return new SalesQuotationList(
            rows.Select(q => new SalesQuotationRow(
                q.Id, q.Code, q.Version, q.QuotationDate, q.ValidityDate, q.IsExpired, q.CustomerId, q.CustomerName, q.QuotationTypeId,
                q.QuotationTypeName, q.OfficeId, q.OfficeName, q.SalesForceId, q.SalesForceName, ApprovalFlow.StatusKey(q.Status),
                q.ItemCount, q.TotalQuantity, q.TotalAmount, q.CreatedAt, q.ModifiedAt)).ToList(),
            total,
            counts);
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveSalesQuotationRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var quotation = new SalesQuotation
        {
            AccountId = current.AccountId!.Value,
            BusinessUnitId = current.BusinessUnitId!.Value,
            Code = "",
        };
        return SaveAsync(quotation, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id, SaveSalesQuotationRequest request, ICurrentContext current, ErpDbContext db, FeatureConfigService configs,
        CancellationToken cancellationToken)
    {
        var quotation = await FindAsync(db, id, cancellationToken);
        if (quotation is null) return TypedResults.NotFound();
        if (quotation.Status != ApprovalStatus.Pending) return ApprovalFlow.NotPending("changed");
        return await SaveAsync(quotation, request, current, db, configs, cancellationToken);
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ProblemHttpResult>> Revise(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var source = await FindAsync(db, id, cancellationToken);
        if (source is null) return TypedResults.NotFound();
        if (!await CanReviseAsync(db, source, cancellationToken))
        {
            return TypedResults.Problem(
                detail: "Only the latest reviewed version without a sales order can be revised.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var copy = new SalesQuotation
        {
            AccountId = source.AccountId,
            BusinessUnitId = source.BusinessUnitId,
            Code = source.Code,
            Version = await NextVersionAsync(db, source.Code, cancellationToken),
            VersionDate = DateOnly.FromDateTime(DateTime.Today),
            QuotationTypeId = source.QuotationTypeId,
            OfficeId = source.OfficeId,
            CustomerId = source.CustomerId,
            CustomerName = source.CustomerName,
            QuotationDate = source.QuotationDate,
            ValidityDate = source.ValidityDate,
            SalesForceId = source.SalesForceId,
            SalesForceName = source.SalesForceName,
            ReferenceId = source.Id,
            Reference = source.Reference,
            BillingContactPerson = source.BillingContactPerson,
            BillingPhoneNo = source.BillingPhoneNo,
            BillingAddress = source.BillingAddress,
            ShippingContactPerson = source.ShippingContactPerson,
            ShippingAddressId = source.ShippingAddressId,
            ShippingAddressName = source.ShippingAddressName,
            IsVatInclusive = source.IsVatInclusive,
            IsGrossDiscount = source.IsGrossDiscount,
            DiscountPercentage = source.DiscountPercentage,
            DiscountAmount = source.DiscountAmount,
            DeliveryCharge = source.DeliveryCharge,
            ExtraCharge = source.ExtraCharge,
            TotalAmount = source.TotalAmount,
            Subjects = source.Subjects,
            BodyNarration = source.BodyNarration,
            FooterNarration = source.FooterNarration,
            Narration = source.Narration,
            CustomData = source.CustomData,
        };
        copy.Lines.AddRange(source.Lines.OrderBy(l => l.Sequence).Select(l => new SalesQuotationLine
        {
            Sequence = l.Sequence,
            ItemId = l.ItemId,
            ItemCode = l.ItemCode,
            ItemName = l.ItemName,
            ItemDescription = l.ItemDescription,
            UomId = l.UomId,
            UomName = l.UomName,
            Quantity = l.Quantity,
            Price = l.Price,
            TotalAmount = l.TotalAmount,
            DiscountPercentage = l.DiscountPercentage,
            DiscountAmount = l.DiscountAmount,
            IsAmountWiseDiscount = l.IsAmountWiseDiscount,
            IsDiscountPerQuantity = l.IsDiscountPerQuantity,
            SdPercentage = l.SdPercentage,
            SdAmount = l.SdAmount,
            VatPercentage = l.VatPercentage,
            VatAmount = l.VatAmount,
            AitPercentage = l.AitPercentage,
            AitAmount = l.AitAmount,
            TotalValue = l.TotalValue,
            WarrantyInMonth = l.WarrantyInMonth,
            DeliveryDate = l.DeliveryDate,
        }));

        if (source.Status == ApprovalStatus.Approved) source.Status = ApprovalStatus.Closed;
        db.SalesQuotations.Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, copy.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, copy.Id, cancellationToken))!);
    }

    private static Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Approve(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async q =>
        {
            var problem = await ApprovalFlow.ActAsync(db, current, Feature, q, true, request.Comment, cancellationToken);
            if (q.Status == ApprovalStatus.Approved) ApproveLines(q);
            return problem;
        }, cancellationToken);

    private static void ApproveLines(SalesQuotation quotation)
    {
        foreach (var line in quotation.Lines)
        {
            line.ApproveQuantity = line.Quantity;
            line.ApprovePrice = line.Price;
            line.ApproveAmount = line.TotalAmount;
        }
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Reject(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db, q => ApprovalFlow.ActAsync(db, current, Feature, q, false, request.Comment, cancellationToken),
                cancellationToken);

    private static Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Close(
        int id, ErpDbContext db, CancellationToken cancellationToken) =>
        ChangeAsync(id, db, async q => await SalesCancel.QuotationAsync(db, q.Id, cancellationToken) ?? DocumentData.Close(q), cancellationToken);

    private static async Task<Results<Ok<ApprovalProgress>, NotFound>> Approval(
        int id, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var quotation = await db.SalesQuotations.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id && q.IsActive, cancellationToken);
        return quotation is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(await ApprovalFlow.ProgressAsync(db, current, Feature, quotation, cancellationToken));
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Cancel(
        int id, ReviewRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken) =>
        ApprovalFlow.MissingReason(request) is { } missing
            ? missing
            : await ChangeAsync(id, db,
                q => ApprovalFlow.CancelAsync(db, current, Feature, q, request.Comment,
                    () => SalesCancel.QuotationAsync(db, q.Id, cancellationToken), cancellationToken),
                cancellationToken);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(int id, ErpDbContext db, CancellationToken cancellationToken)
    {
        var quotation = await db.SalesQuotations.SingleOrDefaultAsync(q => q.Id == id && q.IsActive, cancellationToken);
        if (quotation is null) return TypedResults.NotFound();
        if (DocumentData.Delete(quotation) is { } problem) return problem;
        await ApprovalFlow.RemoveAsync(db, Feature, id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<SalesQuotation?> FindAsync(ErpDbContext db, int id, CancellationToken cancellationToken) =>
        db.SalesQuotations.Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id && q.IsActive, cancellationToken);

    private static async Task<bool> CanReviseAsync(ErpDbContext db, SalesQuotation quotation, CancellationToken cancellationToken) =>
        quotation.Status != ApprovalStatus.Pending && !quotation.IsOrdered &&
        !await db.SalesQuotations.AnyAsync(q => q.ReferenceId == quotation.Id && q.IsActive, cancellationToken);

    private static async Task<string> NextVersionAsync(ErpDbContext db, string code, CancellationToken cancellationToken)
    {
        var used = await db.SalesQuotations.Where(q => q.Code == code).Select(q => q.Version).ToListAsync(cancellationToken);
        var number = used.Count + 1;
        while (used.Contains($"v{number}")) number++;
        return $"v{number}";
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> ChangeAsync(
        int id, ErpDbContext db, Func<SalesQuotation, Task<ProblemHttpResult?>> change, CancellationToken cancellationToken)
    {
        var quotation = await FindAsync(db, id, cancellationToken);
        if (quotation is null) return TypedResults.NotFound();
        if (await change(quotation) is { } problem) return problem;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<Ok<SalesQuotationDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        SalesQuotation quotation, SaveSalesQuotationRequest sent, ICurrentContext current, ErpDbContext db,
        FeatureConfigService configs, CancellationToken cancellationToken)
    {
        var existing = quotation.Id > 0 ? ToRequest(quotation) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var version = DocumentData.Trimmed(request.Version) ?? existing?.Version ?? FirstVersion;
        var code = await DocumentData.CodeAsync(
            db.SalesQuotations.Where(q => q.Version == version), quotation, request.Code, errors, cancellationToken);
        await PartnerData.CheckLookupsAsync(db,
        [
            ("quotationTypeId", request.QuotationTypeId, existing?.QuotationTypeId, SalesLookups.QuotationType),
            ("shippingAddressId", request.ShippingAddressId, existing?.ShippingAddressId, SalesLookups.ShippingAddress),
        ], errors, cancellationToken);
        await SalesData.CheckPeopleAsync(
            db, request.CustomerId, existing?.CustomerId, request.OfficeId, existing?.OfficeId, request.SalesForceId,
            existing?.SalesForceId, errors, cancellationToken);
        var items = await DocumentData.CheckItemsAsync(
            db, [.. request.Lines.Select(l => l.ItemId)], quotation.Lines.Select(l => l.ItemId), errors, cancellationToken);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        var lines = request.Lines.Select((line, index) =>
        {
            var item = items[line.ItemId];
            return new SalesQuotationLine
            {
                Sequence = index + 1,
                ItemId = item.Id,
                ItemCode = item.Code,
                ItemName = item.Name,
                ItemDescription = DocumentData.Trimmed(line.ItemDescription),
                UomId = item.UomId,
                UomName = item.UomName,
                Quantity = line.Quantity,
                Price = line.Price ?? item.SalesPrice,
                DiscountPercentage = line.DiscountPercentage ?? 0,
                DiscountAmount = line.DiscountAmount ?? 0,
                IsAmountWiseDiscount = line.IsAmountWiseDiscount || line.IsDiscountPerQuantity,
                IsDiscountPerQuantity = line.IsDiscountPerQuantity,
                SdPercentage = line.SdPercentage ?? item.SdPercentage,
                VatPercentage = line.VatPercentage ?? item.SalesVatPercentage,
                AitPercentage = line.AitPercentage ?? 0,
                WarrantyInMonth = line.WarrantyInMonth ?? 0,
                DeliveryDate = DocumentData.Trimmed(line.DeliveryDate),
            };
        }).ToList();

        var percentage = request.IsGrossDiscount ? request.DiscountPercentage ?? 0 : 0;
        SalesPricing.Price(
            lines,
            new DiscountRule(request.IsVatInclusive, request.IsGrossDiscount, percentage, request.DiscountAmount ?? 0, "discountAmount"),
            errors);
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        quotation.Lines.Clear();
        quotation.Lines.AddRange(lines);
        quotation.IsVatInclusive = request.IsVatInclusive;
        quotation.IsGrossDiscount = request.IsGrossDiscount;
        quotation.DiscountPercentage = percentage;
        quotation.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        quotation.DeliveryCharge = request.DeliveryCharge ?? 0;
        quotation.ExtraCharge = request.ExtraCharge ?? 0;
        quotation.TotalAmount = lines.Sum(l => l.TotalValue) + quotation.DeliveryCharge + quotation.ExtraCharge;

        if (quotation.Id == 0) db.SalesQuotations.Add(quotation);
        quotation.Code = code ?? await DocumentData.NextCodeAsync(db.SalesQuotations, CodePrefix, cancellationToken);
        quotation.Version = version;
        quotation.VersionDate = request.VersionDate ?? existing?.VersionDate ?? request.QuotationDate;
        quotation.QuotationTypeId = request.QuotationTypeId;
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidityDate = request.ValidityDate;
        quotation.OfficeId = request.OfficeId;
        if (request.CustomerId != existing?.CustomerId)
        {
            quotation.CustomerId = request.CustomerId;
            quotation.CustomerName = await db.Customers.Where(c => c.Id == request.CustomerId).Select(c => c.Name)
                .SingleAsync(cancellationToken);
        }

        if (request.SalesForceId != existing?.SalesForceId)
        {
            quotation.SalesForceId = request.SalesForceId;
            quotation.SalesForceName = await SalesData.EmployeeNameAsync(db, request.SalesForceId, cancellationToken);
        }

        quotation.Reference = DocumentData.Trimmed(request.Reference);
        quotation.BillingContactPerson = DocumentData.Trimmed(request.BillingContactPerson);
        quotation.BillingPhoneNo = DocumentData.Trimmed(request.BillingPhoneNo);
        quotation.BillingAddress = DocumentData.Trimmed(request.BillingAddress);
        quotation.ShippingContactPerson = DocumentData.Trimmed(request.ShippingContactPerson);
        quotation.ShippingAddressId = request.ShippingAddressId;
        quotation.ShippingAddressName = DocumentData.Trimmed(request.ShippingAddressName);
        quotation.Subjects = DocumentData.Trimmed(request.Subjects);
        quotation.BodyNarration = DocumentData.Trimmed(request.BodyNarration);
        quotation.FooterNarration = DocumentData.Trimmed(request.FooterNarration);
        quotation.Narration = DocumentData.Trimmed(request.Narration);
        quotation.CustomData = CustomFieldData.ToJson(request.CustomData);

        await db.SaveChangesAsync(cancellationToken);
        await ApprovalFlow.StartAsync(db, current, Feature, quotation.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, quotation.Id, cancellationToken))!);
    }

    private static SaveSalesQuotationRequest ToRequest(SalesQuotation q) => new()
    {
        Code = q.Code,
        QuotationTypeId = q.QuotationTypeId,
        Version = q.Version,
        VersionDate = q.VersionDate,
        QuotationDate = q.QuotationDate,
        ValidityDate = q.ValidityDate,
        CustomerId = q.CustomerId,
        OfficeId = q.OfficeId,
        SalesForceId = q.SalesForceId,
        Reference = q.Reference,
        BillingContactPerson = q.BillingContactPerson,
        BillingPhoneNo = q.BillingPhoneNo,
        BillingAddress = q.BillingAddress,
        ShippingContactPerson = q.ShippingContactPerson,
        ShippingAddressId = q.ShippingAddressId,
        ShippingAddressName = q.ShippingAddressName,
        IsVatInclusive = q.IsVatInclusive,
        IsGrossDiscount = q.IsGrossDiscount,
        DiscountPercentage = q.DiscountPercentage,
        DiscountAmount = q.DiscountAmount,
        DeliveryCharge = q.DeliveryCharge,
        ExtraCharge = q.ExtraCharge,
        Subjects = q.Subjects,
        BodyNarration = q.BodyNarration,
        FooterNarration = q.FooterNarration,
        Narration = q.Narration,
        Lines = [],
        CustomData = CustomFieldData.Parse(q.CustomData),
    };

    private static async Task<SalesQuotationDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var q = await db.SalesQuotations.AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (q is null) return null;

        var names = await PartnerData.LookupNamesAsync(db, [q.QuotationTypeId, q.ShippingAddressId], cancellationToken);
        string? Lookup(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;

        var versions = await db.SalesQuotations.AsNoTracking()
            .Where(x => x.Code == q.Code && x.IsActive)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return new SalesQuotationDetail
        {
            Id = q.Id,
            Code = q.Code,
            QuotationTypeId = q.QuotationTypeId,
            QuotationTypeName = Lookup(q.QuotationTypeId),
            Version = q.Version,
            VersionDate = q.VersionDate,
            QuotationDate = q.QuotationDate,
            ValidityDate = q.ValidityDate,
            IsExpired = q.IsExpired,
            CustomerId = q.CustomerId,
            CustomerName = q.CustomerName,
            OfficeId = q.OfficeId,
            OfficeName = await db.Offices.Where(o => o.Id == q.OfficeId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken),
            SalesForceId = q.SalesForceId,
            SalesForceName = q.SalesForceName,
            ReferenceId = q.ReferenceId,
            Reference = q.Reference,
            BillingContactPerson = q.BillingContactPerson,
            BillingPhoneNo = q.BillingPhoneNo,
            BillingAddress = q.BillingAddress,
            ShippingContactPerson = q.ShippingContactPerson,
            ShippingAddressId = q.ShippingAddressId,
            ShippingAddressName = q.ShippingAddressName,
            ShipToName = Lookup(q.ShippingAddressId),
            IsVatInclusive = q.IsVatInclusive,
            IsGrossDiscount = q.IsGrossDiscount,
            DiscountPercentage = q.DiscountPercentage,
            DiscountAmount = q.DiscountAmount,
            DeliveryCharge = q.DeliveryCharge,
            ExtraCharge = q.ExtraCharge,
            Subjects = q.Subjects,
            BodyNarration = q.BodyNarration,
            FooterNarration = q.FooterNarration,
            Narration = q.Narration,
            Status = ApprovalFlow.StatusKey(q.Status),
            ReviewedAt = q.ReviewedAt,
            ReviewedByName = await DocumentData.ReviewerNameAsync(db, q.ReviewedById, cancellationToken),
            ReviewComment = q.ReviewComment,
            IsOrdered = q.IsOrdered,
            CanRevise = await CanReviseAsync(db, q, cancellationToken),
            TotalQuantity = q.Lines.Sum(l => l.Quantity),
            GrossAmount = q.Lines.Sum(l => l.TotalAmount),
            SdAmount = q.Lines.Sum(l => l.SdAmount),
            VatAmount = q.Lines.Sum(l => l.VatAmount),
            AitAmount = q.Lines.Sum(l => l.AitAmount),
            TotalAmount = q.TotalAmount,
            Lines = [.. q.Lines.OrderBy(l => l.Sequence).Select(l => new SalesQuotationLineDetail(
                l.Id, l.ItemId, l.ItemCode ?? "", l.ItemName, l.ItemDescription, l.UomId, l.UomName, l.Quantity, l.Price,
                l.TotalAmount, l.DiscountPercentage, l.DiscountAmount, l.IsAmountWiseDiscount, l.IsDiscountPerQuantity,
                l.SdPercentage, l.SdAmount, l.VatPercentage, l.VatAmount, l.AitPercentage, l.AitAmount, l.TotalValue,
                l.WarrantyInMonth, l.DeliveryDate, l.ApproveQuantity, l.ApprovePrice, l.ApproveAmount))],
            Versions = [.. versions.Select(v => new SalesQuotationVersion(
                v.Id, v.Version, v.VersionDate, ApprovalFlow.StatusKey(v.Status), v.TotalAmount))],
            CustomData = CustomFieldData.Parse(q.CustomData),
            CreatedAt = q.CreatedAt,
            ModifiedAt = q.ModifiedAt,
        };
    }
}
