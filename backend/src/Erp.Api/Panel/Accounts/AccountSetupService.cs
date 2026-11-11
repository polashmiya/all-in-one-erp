using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Security;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Security;
using Erp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Accounts;

/// <summary>Outcome of a setup: the created account, or field errors keyed like the request JSON.</summary>
public sealed record AccountSetupOutcome(AccountSetupResult? Result, IDictionary<string, string[]>? Errors);

/// <summary>
/// Creates an account with its business units, offices, warehouses, ERP features, system roles and primary ERP admin.
/// Everything is saved with one SaveChanges, i.e. one transaction: either the whole tree exists or nothing does.
/// </summary>
public sealed class AccountSetupService(
    ErpDbContext db, IPasswordHasher hasher, PermissionCatalog catalog, AccountFeatureService features, LookupDefaults lookups)
{
    public async Task<AccountSetupOutcome> SetupAsync(AccountSetupRequest request, CancellationToken cancellationToken)
    {
        var info = request.Account;
        var code = AccountSetupRequestValidator.Normalize(info.Code);
        var adminEmail = NormalizeEmail(request.Admin.Email);

        var errors = new Dictionary<string, string[]>();
        if (await db.Accounts.AnyAsync(a => a.Code == code, cancellationToken))
        {
            errors["account.code"] = ["This account code is already in use."];
        }

        if (await db.ErpUsers.AcrossTenants().AnyAsync(u => u.Email == adminEmail, cancellationToken))
        {
            errors["admin.email"] = ["A user with this email already exists."];
        }

        var (featureIds, unknownFeatures) = await features.ResolveAsync(request.FeatureCodes, cancellationToken);
        if (unknownFeatures.Count > 0)
        {
            errors[AccountFeatureService.ErrorKey] = [AccountFeatureService.UnknownMessage(unknownFeatures)];
        }

        if (errors.Count > 0) return new AccountSetupOutcome(null, errors);

        var account = new Account
        {
            Code = code,
            Name = info.Name.Trim(),
            Email = info.Email.Trim(),
            Phone = info.Phone.Trim(),
            Website = Clean(info.Website),
            Industry = Clean(info.Industry),
            TaxId = Clean(info.TaxId),
            Address = info.Address.Trim(),
            City = Clean(info.City),
            Country = info.Country.Trim().ToUpperInvariant(),
            BaseCurrency = info.BaseCurrency.Trim().ToUpperInvariant(),
            TimeZone = info.TimeZone.Trim(),
        };
        db.Accounts.Add(account); // assigns account.Id

        var units = request.BusinessUnits.Select(input => AddBusinessUnit(account, input)).ToList();
        await features.ReplaceAsync(account.Id, featureIds, cancellationToken);

        var administrator = SystemRoles.CreateAdministrator(account.Id, catalog);
        db.Roles.AddRange(administrator, SystemRoles.CreateViewer(account.Id, catalog));

        var generatedPassword = string.IsNullOrEmpty(request.Admin.Password) ? PasswordGenerator.Generate() : null;
        var admin = new ErpUser
        {
            AccountId = account.Id,
            Email = adminEmail,
            FullName = request.Admin.FullName.Trim(),
            PasswordHash = hasher.Hash(generatedPassword ?? request.Admin.Password!),
            IsPrimaryAdmin = true,
        };
        foreach (var unit in units)
        {
            admin.BusinessUnits.Add(new UserBusinessUnit { AccountId = account.Id, BusinessUnit = unit, Role = administrator });
        }

        db.ErpUsers.Add(admin);
        await lookups.AddMissingAsync(account.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new AccountSetupOutcome(
            new AccountSetupResult(
                account.Id,
                account.Code,
                account.Name,
                units.Count,
                units.Sum(u => u.Offices.Count),
                units.Sum(u => u.Warehouses.Count),
                featureIds.Count,
                new AdminCredentials(admin.FullName, admin.Email, generatedPassword)),
            null);
    }

    private BusinessUnit AddBusinessUnit(Account account, BusinessUnitInput input)
    {
        var unit = new BusinessUnit
        {
            AccountId = account.Id,
            Code = AccountSetupRequestValidator.Normalize(input.Code),
            Name = input.Name.Trim(),
            Email = Clean(input.Email),
            Phone = Clean(input.Phone),
            Address = Clean(input.Address),
        };
        db.BusinessUnits.Add(unit);

        var offices = new Dictionary<string, Office>();
        foreach (var o in input.Offices)
        {
            var office = new Office
            {
                AccountId = account.Id,
                BusinessUnit = unit,
                Code = AccountSetupRequestValidator.Normalize(o.Code),
                Name = o.Name.Trim(),
                Email = Clean(o.Email),
                Phone = Clean(o.Phone),
                Address = Clean(o.Address),
            };
            offices[office.Code] = office;
            unit.Offices.Add(office);
        }

        foreach (var w in input.Warehouses)
        {
            var officeCode = AccountSetupRequestValidator.Normalize(w.OfficeCode);
            unit.Warehouses.Add(new Warehouse
            {
                AccountId = account.Id,
                BusinessUnit = unit,
                Office = officeCode.Length > 0 ? offices[officeCode] : null,
                Code = AccountSetupRequestValidator.Normalize(w.Code),
                Name = w.Name.Trim(),
                Address = Clean(w.Address),
            });
        }

        return unit;
    }

    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
