using Erp.Core.Domain.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Inserts the platform's default Panel menu items when their key is missing. Existing rows are never
/// overwritten: once inserted, the database is the source of truth (labels, order, icons can be edited there).
/// </summary>
public static class PanelMenuSynchronizer
{
    private sealed record Default(string Key, string LabelEn, string LabelBn, string Icon, string? Path, int SortOrder);

    private static readonly Default[] Defaults =
    [
        new("accounts", "Accounts", "অ্যাকাউন্ট", "bank", "/accounts", 10),
        new("configuration", "Screen configuration", "স্ক্রিন কনফিগারেশন", "setting", "/configuration", 20),
    ];

    public static async Task SyncAsync(ErpDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var existing = await db.PanelMenuItems.Select(m => m.Key).ToListAsync(cancellationToken);
        var missing = Defaults.Where(d => !existing.Contains(d.Key)).ToList();
        if (missing.Count == 0) return;

        db.PanelMenuItems.AddRange(missing.Select(d => new PanelMenuItem
        {
            Key = d.Key,
            LabelEn = d.LabelEn,
            LabelBn = d.LabelBn,
            Icon = d.Icon,
            Path = d.Path,
            SortOrder = d.SortOrder,
        }));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Panel menu: added {Keys}", string.Join(", ", missing.Select(d => d.Key)));
    }
}
