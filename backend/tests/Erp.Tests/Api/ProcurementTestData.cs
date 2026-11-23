using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Inventory;
using Erp.Core.Domain.Partners;
using Erp.Core.Security;
using Erp.Infrastructure.Persistence;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Tests.Api;

internal static class ProcurementTestData
{
    public const string ProcurementOrdersUrl = "/api/erp/procurement/purchase-orders";
    public const string ProcurementReceivesUrl = "/api/erp/procurement/purchase-receives";

    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    public static async Task<Guid> WarehouseAsync(Guid unitId)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.Warehouses.AcrossTenants().Where(w => w.BusinessUnitId == unitId && w.IsActive).Select(w => w.Id).FirstAsync();
    }

    public static async Task<int> ItemAsync(Guid unitId, decimal cost, decimal vat = 0)
    {
        await using var db = ApiFactory.CreateDbContext();
        var minor = await db.ItemMinorCategories.AcrossTenants()
            .Where(m => m.BusinessUnitId == unitId && m.IsActive)
            .Select(m => new
            {
                m.Id,
                m.AccountId,
                m.CategoryId,
                m.SubCategoryId,
                DepartmentId = db.ItemCategories.AcrossTenants().Where(c => c.Id == m.CategoryId).Select(c => c.DepartmentId).First(),
            })
            .FirstAsync();
        var uom = await db.UnitsOfMeasure.AcrossTenants().Where(u => u.AccountId == minor.AccountId).FirstAsync();
        var code = Unique();
        var item = new Item
        {
            AccountId = minor.AccountId,
            BusinessUnitId = unitId,
            Code = code,
            BarCode = code,
            Name = $"Item {code}",
            DepartmentId = minor.DepartmentId,
            CategoryId = minor.CategoryId,
            SubCategoryId = minor.SubCategoryId,
            MinorCategoryId = minor.Id,
            UomId = uom.Id,
            UomName = uom.Name,
            LastCostPrice = cost,
            PurchaseVatPercentage = vat,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public static async Task<int> SupplierAsync(Guid unitId, decimal? tolerance = null)
    {
        await using var db = ApiFactory.CreateDbContext();
        var accountId = await db.BusinessUnits.AcrossTenants().Where(u => u.Id == unitId).Select(u => u.AccountId).FirstAsync();
        var code = Unique();
        var supplier = new Supplier
        {
            AccountId = accountId,
            BusinessUnitId = unitId,
            Code = code,
            Name = $"Supplier {code}",
            Tolerance = tolerance,
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<IEnumerable<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest)).Errors.Keys;


    public static async Task<(Guid UserId, Guid RoleId, HttpClient Client)> ApproverAsync(
        ApiFactory factory, Guid unitId, params string[] permissions)
    {
        var hasher = factory.Services.GetRequiredService<IPasswordHasher>();
        var code = Unique().ToLowerInvariant();
        await using (var db = ApiFactory.CreateDbContext())
        {
            var accountId = await db.BusinessUnits.AcrossTenants().Where(u => u.Id == unitId).Select(u => u.AccountId).FirstAsync();
            var role = new Role { AccountId = accountId, Name = $"Approver {code}" };
            foreach (var permission in permissions) role.Permissions.Add(new RolePermission { PermissionCode = permission });
            var user = new ErpUser
            {
                AccountId = accountId,
                Username = $"approver.{code}",
                Email = $"approver.{code}@test.example",
                FullName = $"Approver {code}",
                PasswordHash = hasher.Hash(ApiFactory.TestPassword),
            };
            user.BusinessUnits.Add(new UserBusinessUnit { AccountId = accountId, BusinessUnitId = unitId, Role = role });
            db.AddRange(role, user);
            await db.SaveChangesAsync();

            var login = await factory.ErpLoginAsync(user.Email, ApiFactory.TestPassword);
            return (user.Id, role.Id, factory.CreateClient(login.AccessToken, unitId));
        }
    }
    public static async Task<PurchaseOrderDetail> ApprovedOrderAsync(
        ApiFactory factory, HttpClient admin, int itemId, decimal quantity, int? supplierId = null)
    {
        var order = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(ProcurementOrdersUrl, new SavePurchaseOrderRequest
        {
            Code = null,
            OrderDate = Today,
            SupplierId = supplierId ?? await SupplierAsync(factory.DhakaId),
            WarehouseId = await WarehouseAsync(factory.DhakaId),
            RequisitionId = null,
            ReferenceNo = null,
            ExpectedReceiveDate = null,
            PaymentTypeId = null,
            PaymentDays = null,
            IsVatInclusive = false,
            ItemTypeId = null,
            ShippingAddress = null,
            HeaderDiscount = null,
            DeliveryCharge = null,
            LaborCharge = null,
            OtherCharge = null,
            Remarks = null,
            Lines = [new SavePurchaseOrderLine
            {
                ItemId = itemId,
                Quantity = quantity,
                Price = null,
                Discount = null,
                VatPercentage = null,
                Specification = null,
            }],
        }));
        return await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync($"{ProcurementOrdersUrl}/{order.Id}/approve", new ReviewRequest(null)));
    }

    public static async Task<PurchaseReceiveDetail> ApprovedReceiveAsync(
        ApiFactory factory, HttpClient admin, int itemId, decimal quantity, int? supplierId = null)
    {
        var order = await ApprovedOrderAsync(factory, admin, itemId, quantity, supplierId);
        var receive = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(ProcurementReceivesUrl, await ReceiveAsync(factory, order.Id, itemId, quantity)));
        return await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync($"{ProcurementReceivesUrl}/{receive.Id}/approve", new ReviewRequest(null)));
    }

    public static async Task<SavePurchaseReceiveRequest> ReceiveAsync(ApiFactory factory, int orderId, int itemId, decimal quantity) => new()
    {
        Code = null,
        ReceiveDate = Today,
        PurchaseOrderId = orderId,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        ChallanNo = null,
        ChallanDate = null,
        VatChallanNo = null,
        PaymentTypeId = null,
        PaymentDays = null,
        IsVatInclusive = false,
        HeaderDiscount = null,
        DeliveryCharge = null,
        LaborCharge = null,
        OtherCharge = null,
        Remarks = null,
        Lines = [new SavePurchaseReceiveLine
        {
            ItemId = itemId,
            Quantity = quantity,
            Price = null,
            Discount = null,
            VatPercentage = null,
            Batch = null,
        }],
    };

    public static string Unique() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
