using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchasePayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "PurchaseReceive",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 36);

            migrationBuilder.CreateTable(
                name: "PurchasePayment",
                columns: table => new
                {
                    PurchasePaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchasePaymentCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    PayeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccountNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstrumentNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstrumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TDS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_PurchasePayment", x => x.PurchasePaymentID);
                    table.ForeignKey(
                        name: "FK_PurchasePayment_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasePayment_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasePayment_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasePayment_Supplier_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "PurchasePaymentLine",
                columns: table => new
                {
                    PurchasePaymentLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchasePaymentID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PurchaseReceiveID = table.Column<int>(type: "int", nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VDS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TDS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasePaymentLine", x => x.PurchasePaymentLineID);
                    table.ForeignKey(
                        name: "FK_PurchasePaymentLine_PurchasePayment_PurchasePaymentID",
                        column: x => x.PurchasePaymentID,
                        principalTable: "PurchasePayment",
                        principalColumn: "PurchasePaymentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchasePaymentLine_PurchaseReceive_PurchaseReceiveID",
                        column: x => x.PurchaseReceiveID,
                        principalTable: "PurchaseReceive",
                        principalColumn: "PurchaseReceiveID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_AccountID",
                table: "PurchasePayment",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_BusinessUnitID_isActive_Status_PaymentDate",
                table: "PurchasePayment",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_BusinessUnitID_PurchasePaymentCode",
                table: "PurchasePayment",
                columns: new[] { "BusinessUnitID", "PurchasePaymentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_PaymentMethodID",
                table: "PurchasePayment",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayment_SupplierID",
                table: "PurchasePayment",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePaymentLine_PurchasePaymentID",
                table: "PurchasePaymentLine",
                column: "PurchasePaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePaymentLine_PurchaseReceiveID",
                table: "PurchasePaymentLine",
                column: "PurchaseReceiveID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchasePaymentLine");

            migrationBuilder.DropTable(
                name: "PurchasePayment");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "PurchaseReceive");
        }
    }
}
