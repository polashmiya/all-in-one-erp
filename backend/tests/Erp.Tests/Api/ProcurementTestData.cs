using System.Net;
using System.Net.Http.Json;
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
    public static string Unique() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
