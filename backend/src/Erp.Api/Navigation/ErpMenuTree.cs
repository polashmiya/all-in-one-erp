using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Navigation;

/// <param name="Code">Feature code, <c>{module}.{feature}</c>; also the key used for account grants and permissions.</param>
/// <param name="Path">ERP route, <c>/app/{module}/{feature}</c>.</param>
public sealed record ErpMenuFeature(string Code, string Key, LocalizedLabel Label, string Path, int ConfigVersion);

public sealed record ErpMenuSubModule(string Key, LocalizedLabel Label, string? Icon, IReadOnlyList<ErpMenuFeature> Features);

public sealed record ErpMenuModule(string Key, LocalizedLabel Label, string? Icon, IReadOnlyList<ErpMenuSubModule> SubModules);

/// <summary>Builds the Module → Sub-module → Feature tree from the ERP menu tables, keeping only included features.</summary>
public static class ErpMenuTree
{
    public static string PathOf(string featureCode) => $"/app/{featureCode.Replace('.', '/')}";

    public static async Task<IReadOnlyList<ErpMenuModule>> BuildAsync(
        ErpDbContext db,
        Func<string, bool> includeFeature,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, int>? configVersions = null)
    {
        var rows = await db.ErpFeatures.AsNoTracking()
            .Where(f => f.IsActive && f.SubModule!.IsActive && f.SubModule.Module!.IsActive)
            .Select(f => new
            {
                f.Code,
                f.Key,
                f.NameEn,
                f.NameBn,
                f.SortOrder,
                SubKey = f.SubModule!.Key,
                SubNameEn = f.SubModule.NameEn,
                SubNameBn = f.SubModule.NameBn,
                SubIcon = f.SubModule.Icon,
                SubOrder = f.SubModule.SortOrder,
                ModuleKey = f.SubModule.Module!.Key,
                ModuleNameEn = f.SubModule.Module.NameEn,
                ModuleNameBn = f.SubModule.Module.NameBn,
                ModuleIcon = f.SubModule.Module.Icon,
                ModuleOrder = f.SubModule.Module.SortOrder,
            })
            .ToListAsync(cancellationToken);

        // Modules and sub-modules only appear when at least one of their features is included.
        return rows
            .Where(r => includeFeature(r.Code))
            .GroupBy(r => new { r.ModuleKey, r.ModuleNameEn, r.ModuleNameBn, r.ModuleIcon, r.ModuleOrder })
            .OrderBy(m => m.Key.ModuleOrder).ThenBy(m => m.Key.ModuleNameEn)
            .Select(m => new ErpMenuModule(
                m.Key.ModuleKey,
                new LocalizedLabel(m.Key.ModuleNameEn, m.Key.ModuleNameBn),
                m.Key.ModuleIcon,
                m.GroupBy(r => new { r.SubKey, r.SubNameEn, r.SubNameBn, r.SubIcon, r.SubOrder })
                    .OrderBy(s => s.Key.SubOrder).ThenBy(s => s.Key.SubNameEn)
                    .Select(s => new ErpMenuSubModule(
                        s.Key.SubKey,
                        new LocalizedLabel(s.Key.SubNameEn, s.Key.SubNameBn),
                        s.Key.SubIcon,
                        s.OrderBy(f => f.SortOrder).ThenBy(f => f.NameEn)
                            .Select(f => new ErpMenuFeature(
                                f.Code,
                                f.Key,
                                new LocalizedLabel(f.NameEn, f.NameBn),
                                PathOf(f.Code),
                                configVersions?.GetValueOrDefault(f.Code) ?? 0))
                            .ToList()))
                    .ToList()))
            .ToList();
    }
}
