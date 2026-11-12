namespace Erp.Api.Panel.Accounts;

// ---------- Setup (one request creates the whole account tree) ----------

public sealed record AccountInfoInput(
    string Code,
    string Name,
    string Email,
    string Phone,
    string? Website,
    string? Industry,
    string? TaxId,
    string Address,
    string? City,
    string Country,
    string BaseCurrency,
    string TimeZone);

public sealed record OfficeInput(string Code, string Name, string? Email, string? Phone, string? Address);

/// <summary>A warehouse of the business unit; <see cref="OfficeCode"/> places it under one of the unit's offices.</summary>
public sealed record WarehouseInput(string Code, string Name, string? OfficeCode, string? Address);

public sealed record BusinessUnitInput(
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    IReadOnlyList<OfficeInput> Offices,
    IReadOnlyList<WarehouseInput> Warehouses);

/// <summary>The account's first ERP user. Leave <see cref="Password"/> empty to have a secure one generated.</summary>
public sealed record AdminUserInput(string FullName, string Email, string? Password);

/// <param name="FeatureCodes">ERP features (menus) the account gets, e.g. system-setup.item-profile.</param>
public sealed record AccountSetupRequest(
    AccountInfoInput Account,
    IReadOnlyList<BusinessUnitInput> BusinessUnits,
    IReadOnlyList<string> FeatureCodes,
    AdminUserInput Admin);

public sealed record AdminCredentials(string FullName, string Email, string? GeneratedPassword);

public sealed record AccountSetupResult(
    Guid AccountId,
    string Code,
    string Name,
    int BusinessUnits,
    int Offices,
    int Warehouses,
    int Features,
    AdminCredentials Admin);

public sealed record AvailabilityResponse(bool? AccountCodeAvailable, bool? AdminEmailAvailable);

// ---------- Landing list ----------

public sealed record AccountListItem(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? City,
    string? Country,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int BusinessUnits,
    int Offices,
    int Warehouses,
    int Users,
    int Features,
    string? AdminEmail);

public sealed record AccountSummary(int Accounts, int ActiveAccounts, int BusinessUnits, int Offices, int Warehouses);

public sealed record AccountListResponse(IReadOnlyList<AccountListItem> Items, int Total, AccountSummary Summary);

// ---------- Detail ----------

public sealed record WarehouseDetail(Guid Id, string Code, string Name, string? Address, bool IsActive);

public sealed record OfficeDetail(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    IReadOnlyList<WarehouseDetail> Warehouses);

/// <param name="Warehouses">Warehouses directly under the business unit (not under an office).</param>
public sealed record BusinessUnitDetail(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    IReadOnlyList<OfficeDetail> Offices,
    IReadOnlyList<WarehouseDetail> Warehouses);

public sealed record AccountAdminDto(Guid Id, string FullName, string Email, bool IsActive, DateTimeOffset? LastLoginAt);

public sealed record AccountDetail(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Website,
    string? Industry,
    string? TaxId,
    string? Address,
    string? City,
    string? Country,
    string BaseCurrency,
    string TimeZone,
    bool IsActive,
    DateTimeOffset CreatedAt,
    AccountAdminDto? Admin,
    IReadOnlyList<BusinessUnitDetail> BusinessUnits,
    IReadOnlyList<string> FeatureCodes,
    AccountTheme? Theme);

// ---------- Menu access (features) ----------

public sealed record AccountFeaturesRequest(IReadOnlyList<string> FeatureCodes);

public sealed record AccountFeaturesResponse(IReadOnlyList<string> FeatureCodes);
