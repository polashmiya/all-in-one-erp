using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountGroupNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @map TABLE (OldCode nvarchar(50), OldName nvarchar(300), NewCode nvarchar(50), NewName nvarchar(300));
                INSERT INTO @map VALUES
                    ('1000', 'Current assets', '1', 'Current Assets'),
                    ('1500', 'Fixed assets', '2', 'Non-Current Assets'),
                    ('2000', 'Current liabilities', '3', 'Current Liabilities'),
                    ('2500', 'Long-term liabilities', '4', 'Non-Current Liabilities'),
                    ('3000', 'Equity', '5', 'Equity'),
                    ('4000', 'Income', '6', 'Income'),
                    ('4500', 'Cost of sales', '7', 'Cost Of Sales'),
                    ('5000', 'Expenses', '8', 'Expense'),
                    ('1510', 'Furniture and fixtures', '2100', 'Furniture and fixtures'),
                    ('1520', 'Office equipment', '2200', 'Office equipment'),
                    ('1530', 'Vehicles', '2300', 'Vehicles'),
                    ('1590', 'Accumulated depreciation', '2900', 'Accumulated depreciation'),
                    ('2100', 'Accounts payable', '3100', 'Accounts payable'),
                    ('2200', 'VAT payable', '3200', 'VAT payable'),
                    ('2210', 'VDS payable', '3210', 'VDS payable'),
                    ('2220', 'TDS payable', '3220', 'TDS payable'),
                    ('2300', 'Salary payable', '3300', 'Salary payable'),
                    ('2510', 'Bank loan', '4100', 'Bank loan'),
                    ('3100', 'Capital', '5100', 'Capital'),
                    ('3200', 'Retained earnings', '5200', 'Retained earnings'),
                    ('3300', 'Opening balance', '5300', 'Opening balance'),
                    ('4100', 'Sales', '6100', 'Sales'),
                    ('4200', 'Purchase discount received', '6200', 'Purchase discount received'),
                    ('4300', 'Stock gain', '6300', 'Stock gain'),
                    ('4400', 'Other income', '6400', 'Other income'),
                    ('4510', 'Cost of goods sold', '7100', 'Cost of goods sold'),
                    ('5100', 'Cost of goods sold', '7100', 'Cost of goods sold'),
                    ('5200', 'Stock loss', '8100', 'Stock loss'),
                    ('5300', 'Purchase price difference', '8200', 'Purchase price difference'),
                    ('5400', 'Operating expenses', '8300', 'Operating expenses'),
                    ('5410', 'Salary and wages', '8310', 'Salary and wages'),
                    ('5420', 'Rent', '8320', 'Rent'),
                    ('5430', 'Utilities', '8330', 'Utilities'),
                    ('5440', 'Transport', '8340', 'Transport'),
                    ('5450', 'Bank charges', '8350', 'Bank charges'),
                    ('5460', 'Depreciation', '8360', 'Depreciation');

                UPDATE c SET ChartOfAccCode = m.NewCode, ChartOfAccName = m.NewName
                FROM ChartOfAccount c
                JOIN @map m ON m.OldCode = c.ChartOfAccCode AND m.OldName = c.ChartOfAccName
                WHERE NOT EXISTS (
                    SELECT 1 FROM ChartOfAccount o
                    WHERE o.AccountID = c.AccountID
                        AND NOT EXISTS (SELECT 1 FROM @map own WHERE own.OldCode = o.ChartOfAccCode AND own.OldName = o.ChartOfAccName)
                        AND EXISTS (SELECT 1 FROM @map taken WHERE taken.NewCode = o.ChartOfAccCode OR taken.NewName = o.ChartOfAccName));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
