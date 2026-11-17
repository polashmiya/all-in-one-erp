namespace Erp.Core.Domain.Locations;

public class Thana
{
    public int Id { get; set; }

    public int DistrictId { get; set; }

    public required string Name { get; set; }

    public required string NameBn { get; set; }
}
