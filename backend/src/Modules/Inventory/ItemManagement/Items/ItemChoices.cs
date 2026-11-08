namespace Erp.Modules.Inventory.ItemManagement.Items;

internal static class ItemChoices
{
    public static readonly IReadOnlyList<ItemLookup> ItemTypes =
    [
        Choice(1, "Finished Goods"),
        Choice(2, "Raw Material"),
        Choice(3, "Semi-Finished Goods"),
        Choice(4, "Packaging Material"),
        Choice(5, "Consumable"),
        Choice(6, "Spare Parts"),
        Choice(7, "Service"),
        Choice(8, "Fixed Asset"),
    ];

    public static readonly IReadOnlyList<ItemLookup> SupplyTypes =
    [
        Choice(1, "Standard VAT"),
        Choice(2, "Reduced VAT"),
        Choice(3, "Zero Rated"),
        Choice(4, "Exempted"),
        Choice(5, "Specific VAT"),
        Choice(6, "MRP Based"),
    ];

    public static string? NameOf(IReadOnlyList<ItemLookup> choices, int? id) =>
        choices.FirstOrDefault(c => c.Id == id)?.Name;

    private static ItemLookup Choice(int id, string name) => new(id, null, name, true, null);
}
