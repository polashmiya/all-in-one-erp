using Erp.Core.Modules;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Navigation;

public sealed record PanelMenuItemDto(
    string Key,
    LocalizedLabel Label,
    string? Icon,
    string? Path,
    IReadOnlyList<PanelMenuItemDto> Children);

public static class PanelNavigationEndpoints
{
    public static IEndpointRouteBuilder MapPanelNavigationEndpoints(this IEndpointRouteBuilder panel)
    {
        panel.MapGet("/navigation", GetNavigation)
            .WithName("GetPanelNavigation")
            .WithTags("PanelNavigation")
            .WithSummary("The Panel sidebar menu, read from the PanelMenuItems table");

        return panel;
    }

    private static async Task<IReadOnlyList<PanelMenuItemDto>> GetNavigation(ErpDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.PanelMenuItems.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.LabelEn)
            .Select(m => new { m.Id, m.ParentId, m.Key, m.LabelEn, m.LabelBn, m.Icon, m.Path })
            .ToListAsync(cancellationToken);

        var byParent = rows.ToLookup(r => r.ParentId);

        // Children of an inactive parent are not reachable from a root, so they drop out naturally.
        IReadOnlyList<PanelMenuItemDto> Build(Guid? parentId) =>
            byParent[parentId]
                .Select(r => new PanelMenuItemDto(r.Key, new LocalizedLabel(r.LabelEn, r.LabelBn), r.Icon, r.Path, Build(r.Id)))
                .ToList();

        return Build(null);
    }
}
