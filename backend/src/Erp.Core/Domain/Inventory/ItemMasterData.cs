namespace Erp.Core.Domain.Inventory;

public abstract class ItemClassification : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public required string Name { get; set; }

    public string? ImageSource { get; set; }

    public string? CustomData { get; set; }
}

public class ItemDepartment : ItemClassification
{
    public required string Code { get; set; }

    public int? Serial { get; set; }
}

public class ItemCategory : ItemClassification
{
    public required string Code { get; set; }

    public int DepartmentId { get; set; }

    public ItemDepartment? Department { get; set; }
}

public class ItemSubCategory : ItemClassification
{
    public int? DepartmentId { get; set; }

    public ItemDepartment? Department { get; set; }

    public int CategoryId { get; set; }

    public ItemCategory? Category { get; set; }
}

public class ItemMinorCategory : ItemClassification
{
    public int CategoryId { get; set; }

    public ItemCategory? Category { get; set; }

    public int SubCategoryId { get; set; }

    public ItemSubCategory? SubCategory { get; set; }
}

public class UnitOfMeasure : MasterRecord
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? RoundingLength { get; set; }

    public bool IsDecimalAllowed { get; set; }

    public Guid? BranchId { get; set; }

    public string? CustomData { get; set; }
}
