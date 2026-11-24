using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CancelBanksAndClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE ChartOfAccount SET ChartOfAccType = CASE
                    WHEN ChartOfAccType = 5 AND ChartOfAccCode = '5100' AND ChartOfAccName = 'Cost of goods sold' THEN 7
                    WHEN ChartOfAccType = 5 THEN 8
                    WHEN ChartOfAccType = 4 THEN 6
                    WHEN ChartOfAccType = 3 THEN 5
                    WHEN ChartOfAccType = 2 THEN 3
                    ELSE ChartOfAccType END;
                UPDATE c SET ChartOfAccName = 'Current assets' FROM ChartOfAccount c WHERE c.ChartOfAccCode = '1000' AND c.ChartOfAccName = 'Assets'
                    AND NOT EXISTS (SELECT 1 FROM ChartOfAccount o WHERE o.AccountID = c.AccountID AND o.ChartOfAccName = 'Current assets');
                UPDATE c SET ChartOfAccName = 'Current liabilities' FROM ChartOfAccount c WHERE c.ChartOfAccCode = '2000' AND c.ChartOfAccName = 'Liabilities'
                    AND NOT EXISTS (SELECT 1 FROM ChartOfAccount o WHERE o.AccountID = c.AccountID AND o.ChartOfAccName = 'Current liabilities');
                """);

            migrationBuilder.DropIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_TransactionType_TransactionRefID",
                table: "SubLedgerHeader");

            migrationBuilder.DropIndex(
                name: "IX_InventoryHeader_BusinessUnitID_InventoryTransType_TransactionRefID",
                table: "InventoryHeader");

            migrationBuilder.AddColumn<bool>(
                name: "IsReversal",
                table: "SubLedgerHeader",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 22);

            migrationBuilder.AddColumn<Guid>(
                name: "FromOfficeID",
                table: "StockTransfer",
                type: "uniqueidentifier",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 20);

            migrationBuilder.AddColumn<Guid>(
                name: "ToOfficeID",
                table: "StockTransfer",
                type: "uniqueidentifier",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 21);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceAdjusted",
                table: "PurchasePaymentLine",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceAdjusted",
                table: "PurchasePayment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 29);

            migrationBuilder.AddColumn<int>(
                name: "BankAccountID",
                table: "PurchasePayment",
                type: "int",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 28);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdvance",
                table: "PurchasePayment",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 27);

            migrationBuilder.AddColumn<bool>(
                name: "IsReversal",
                table: "InventoryHeader",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:ColumnOrder", 16);

            migrationBuilder.CreateTable(
                name: "AccountingSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingSetting_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccount",
                columns: table => new
                {
                    BankAccID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankAccName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankAccType = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoutingNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChartOfAccID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CustomData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccount", x => x.BankAccID);
                    table.ForeignKey(
                        name: "FK_BankAccount_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccount_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccount_ChartOfAccount_ChartOfAccID",
                        column: x => x.ChartOfAccID,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                });

            migrationBuilder.CreateTable(
                name: "PeriodClosing",
                columns: table => new
                {
                    PeriodClosingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    IsYear = table.Column<bool>(type: "bit", nullable: false),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodClosing", x => x.PeriodClosingID);
                    table.ForeignKey(
                        name: "FK_PeriodClosing_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodClosing_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeposit",
                columns: table => new
                {
                    TaxDepositID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxDepositCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    BankAccountID = table.Column<int>(type: "int", nullable: true),
                    ChallanNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChallanDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CustomData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeposit", x => x.TaxDepositID);
                    table.ForeignKey(
                        name: "FK_TaxDeposit_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeposit_BankAccount_BankAccountID",
                        column: x => x.BankAccountID,
                        principalTable: "BankAccount",
                        principalColumn: "BankAccID");
                    table.ForeignKey(
                        name: "FK_TaxDeposit_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxDeposit_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxDepositLine",
                columns: table => new
                {
                    TaxDepositLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxDepositID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PurchasePaymentID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDepositLine", x => x.TaxDepositLineID);
                    table.ForeignKey(
                        name: "FK_TaxDepositLine_PurchasePayment_PurchasePaymentID",
                        column: x => x.PurchasePaymentID,
                        principalTable: "PurchasePayment",
                        principalColumn: "PurchasePaymentID");
                    table.ForeignKey(
                        name: "FK_TaxDepositLine_TaxDeposit_TaxDepositID",
                        column: x => x.TaxDepositID,
                        principalTable: "TaxDeposit",
                        principalColumn: "TaxDepositID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_TransactionType_TransactionRefID_IsReversal",
                table: "SubLedgerHeader",
                columns: new[] { "BusinessUnitID", "TransactionType", "TransactionRefID", "IsReversal" },
                unique: true,
                filter: "[TransactionRefID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_FromOfficeID",
                table: "StockTransfer",
                column: "FromOfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_ToOfficeID",
                table: "StockTransfer",
                column: "ToOfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_BankAccountID",
                table: "PurchasePayment",
                column: "BankAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_BusinessUnitID_InventoryTransType_TransactionRefID_IsReversal",
                table: "InventoryHeader",
                columns: new[] { "BusinessUnitID", "InventoryTransType", "TransactionRefID", "IsReversal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSetting_AccountId",
                table: "AccountingSetting",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_AccountID",
                table: "BankAccount",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_BusinessUnitID_BankAccName",
                table: "BankAccount",
                columns: new[] { "BusinessUnitID", "BankAccName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccount_ChartOfAccID",
                table: "BankAccount",
                column: "ChartOfAccID");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodClosing_AccountID",
                table: "PeriodClosing",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodClosing_BusinessUnitID_IsYear_PeriodEnd",
                table: "PeriodClosing",
                columns: new[] { "BusinessUnitID", "IsYear", "PeriodEnd" },
                unique: true,
                filter: "[isActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeposit_AccountID",
                table: "TaxDeposit",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeposit_BankAccountID",
                table: "TaxDeposit",
                column: "BankAccountID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeposit_BusinessUnitID_isActive_Status_DepositDate",
                table: "TaxDeposit",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "DepositDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeposit_BusinessUnitID_TaxDepositCode",
                table: "TaxDeposit",
                columns: new[] { "BusinessUnitID", "TaxDepositCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeposit_PaymentMethodID",
                table: "TaxDeposit",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDepositLine_PurchasePaymentID",
                table: "TaxDepositLine",
                column: "PurchasePaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDepositLine_TaxDepositID",
                table: "TaxDepositLine",
                column: "TaxDepositID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchasePayment_BankAccount_BankAccountID",
                table: "PurchasePayment",
                column: "BankAccountID",
                principalTable: "BankAccount",
                principalColumn: "BankAccID");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfer_Offices_FromOfficeID",
                table: "StockTransfer",
                column: "FromOfficeID",
                principalTable: "Offices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfer_Offices_ToOfficeID",
                table: "StockTransfer",
                column: "ToOfficeID",
                principalTable: "Offices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE ChartOfAccount SET ChartOfAccType = CASE
                    WHEN ChartOfAccType IN (1, 2) THEN 1
                    WHEN ChartOfAccType IN (3, 4) THEN 2
                    WHEN ChartOfAccType = 5 THEN 3
                    WHEN ChartOfAccType = 6 THEN 4
                    ELSE 5 END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_PurchasePayment_BankAccount_BankAccountID",
                table: "PurchasePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfer_Offices_FromOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfer_Offices_ToOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropTable(
                name: "AccountingSetting");

            migrationBuilder.DropTable(
                name: "PeriodClosing");

            migrationBuilder.DropTable(
                name: "TaxDepositLine");

            migrationBuilder.DropTable(
                name: "TaxDeposit");

            migrationBuilder.DropTable(
                name: "BankAccount");

            migrationBuilder.DropIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_TransactionType_TransactionRefID_IsReversal",
                table: "SubLedgerHeader");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfer_FromOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfer_ToOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayment_BankAccountID",
                table: "PurchasePayment");

            migrationBuilder.DropIndex(
                name: "IX_InventoryHeader_BusinessUnitID_InventoryTransType_TransactionRefID_IsReversal",
                table: "InventoryHeader");

            migrationBuilder.DropColumn(
                name: "IsReversal",
                table: "SubLedgerHeader");

            migrationBuilder.DropColumn(
                name: "FromOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropColumn(
                name: "ToOfficeID",
                table: "StockTransfer");

            migrationBuilder.DropColumn(
                name: "AdvanceAdjusted",
                table: "PurchasePaymentLine");

            migrationBuilder.DropColumn(
                name: "AdvanceAdjusted",
                table: "PurchasePayment");

            migrationBuilder.DropColumn(
                name: "BankAccountID",
                table: "PurchasePayment");

            migrationBuilder.DropColumn(
                name: "IsAdvance",
                table: "PurchasePayment");

            migrationBuilder.DropColumn(
                name: "IsReversal",
                table: "InventoryHeader");

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_TransactionType_TransactionRefID",
                table: "SubLedgerHeader",
                columns: new[] { "BusinessUnitID", "TransactionType", "TransactionRefID" },
                unique: true,
                filter: "[TransactionRefID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_BusinessUnitID_InventoryTransType_TransactionRefID",
                table: "InventoryHeader",
                columns: new[] { "BusinessUnitID", "InventoryTransType", "TransactionRefID" },
                unique: true);
        }
    }
}
