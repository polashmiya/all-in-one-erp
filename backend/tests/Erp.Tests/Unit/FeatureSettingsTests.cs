using Erp.Core.Domain.Metadata;
using Erp.Metadata;

namespace Erp.Tests.Unit;

public sealed class FeatureSettingsTests
{
    private static readonly Guid Unit = Guid.NewGuid();
    private static readonly Guid OtherUnit = Guid.NewGuid();
    private static readonly Guid Field = Guid.NewGuid();

    [Fact]
    public void Business_unit_values_win_and_missing_ones_come_from_the_account()
    {
        var rows = new[]
        {
            new AccountFieldSetting { FieldId = Field, IsVisible = false, LabelEn = "Account", MaxLength = 20 },
            new AccountFieldSetting { FieldId = Field, BusinessUnitId = Unit, IsVisible = true, LabelBn = "ইউনিট" },
            new AccountFieldSetting { FieldId = Field, BusinessUnitId = OtherUnit, IsRequired = true },
        };

        var merged = Assert.Single(FeatureSettings.ForUnit(rows.Take(2), Unit));

        Assert.Equal((true, "Account", "ইউনিট", 20, (bool?)null), (merged.IsVisible, merged.LabelEn, merged.LabelBn, merged.MaxLength, merged.IsRequired));
    }

    [Fact]
    public void Without_a_unit_only_account_rows_apply()
    {
        var rows = new[]
        {
            new AccountFieldSetting { FieldId = Field, IsVisible = false },
            new AccountFieldSetting { FieldId = Field, BusinessUnitId = Unit, IsVisible = true },
        };

        Assert.False(Assert.Single(FeatureSettings.ForUnit(rows, null)).IsVisible);
        Assert.True(Assert.Single(FeatureSettings.ForUnit([rows[1]], Unit)).IsVisible);
    }
}
