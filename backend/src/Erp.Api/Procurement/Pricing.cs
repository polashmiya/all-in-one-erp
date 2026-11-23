namespace Erp.Api.Procurement;

internal static class Pricing
{
    public static (decimal Total, decimal Vat, decimal Net) Line(
        decimal quantity, decimal price, decimal discount, decimal vatPercentage, bool vatInclusive)
    {
        var total = Math.Round(quantity * price, 2);
        var taxable = total - discount;
        var vat = Math.Round(vatInclusive
            ? taxable * vatPercentage / (100 + vatPercentage)
            : taxable * vatPercentage / 100, 2);
        return (total, vat, vatInclusive ? taxable : taxable + vat);
    }
}
