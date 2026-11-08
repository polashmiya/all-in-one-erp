using Erp.Core.Authorization;

namespace Erp.Core.Modules;

public sealed record MenuText(string En, string Bn);

public sealed record LocalizedLabel(string En, string? Bn);

/// <summary>A screen of the ERP. Its code is <c>{module}.{feature}</c>; permissions are <c>{code}.{action}</c>.</summary>
public sealed record MenuFeature(string Key, MenuText Name, int Order, IReadOnlyList<string>? Actions = null);

public sealed record MenuSubModule(string Key, MenuText Name, string? Icon, int Order, IReadOnlyList<MenuFeature> Features);

public sealed record MenuModule(string Key, MenuText Name, string Icon, int Order, IReadOnlyList<MenuSubModule> SubModules);

/// <summary>
/// Implemented by code modules to declare the ERP menu entries (Module → Sub-module → Feature) they provide.
/// These seed the ERP menu tables; after that the database is the source of truth for names, order and icons.
/// </summary>
public interface IErpMenuProvider
{
    IEnumerable<MenuModule> GetModules();
}

public static class FeatureActions
{
    public const string View = "view";
    public const string Create = "create";
    public const string Edit = "edit";
    public const string Delete = "delete";
    public const string Export = "export";
    public const string Import = "import";

    public static readonly IReadOnlyList<string> Standard = [View, Create, Edit, Delete, Export, Import];

    public static string Describe(string action) => action switch
    {
        View => "View",
        Create => "Create",
        Edit => "Edit",
        Delete => "Delete",
        Export => "Export",
        Import => "Import",
        _ => char.ToUpperInvariant(action[0]) + action[1..],
    };
}

/// <summary>A feature flattened with its place in the menu.</summary>
public sealed record CatalogFeature(MenuModule Module, MenuSubModule SubModule, MenuFeature Feature)
{
    public string Code => $"{Module.Key}.{Feature.Key}";

    public IReadOnlyList<string> Actions => Feature.Actions ?? FeatureActions.Standard;
}

/// <summary>All menu entries declared by the registered code modules, validated for duplicate keys.</summary>
public sealed class ErpMenuCatalog
{
    public ErpMenuCatalog(IEnumerable<IErpMenuProvider> providers)
    {
        Modules = providers.SelectMany(p => p.GetModules()).OrderBy(m => m.Order).ToList();

        Ensure(Modules.Select(m => m.Key), "module");
        foreach (var module in Modules) Ensure(module.SubModules.Select(s => s.Key), $"sub-module in '{module.Key}'");

        Features = Modules
            .SelectMany(m => m.SubModules.SelectMany(s => s.Features.Select(f => new CatalogFeature(m, s, f))))
            .ToList();
        Ensure(Features.Select(f => f.Code), "feature");

        foreach (var feature in Features)
        {
            if (!PermissionCode.IsValid($"{feature.Code}.view"))
            {
                throw new InvalidOperationException($"Feature code '{feature.Code}' must be lower-case letters, digits and dashes.");
            }
        }
    }

    public IReadOnlyList<MenuModule> Modules { get; }

    public IReadOnlyList<CatalogFeature> Features { get; }

    private static void Ensure(IEnumerable<string> keys, string what)
    {
        var duplicate = keys.GroupBy(k => k).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate {what} key '{duplicate.Key}' in the ERP menu.");
    }
}

/// <summary>Every feature contributes one permission per action, e.g. system-setup.item-profile.create.</summary>
public sealed class MenuPermissionProvider(ErpMenuCatalog catalog) : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
        catalog.Features.SelectMany(f => f.Actions.Select(action => new PermissionDefinition(
            PermissionCode.Parse($"{f.Code}.{action}"),
            $"{FeatureActions.Describe(action)} {f.Feature.Name.En}")));
}
