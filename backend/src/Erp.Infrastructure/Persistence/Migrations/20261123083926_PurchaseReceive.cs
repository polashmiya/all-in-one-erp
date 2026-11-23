using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseReceive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseReceive",
                columns: table => new
                {
                    PurchaseReceiveID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseReceiveCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseOrderID = table.Column<int>(type: "int", nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallanNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChallanDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VATChallanNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PaymentTypeID = table.Column<int>(type: "int", nullable: true),
                    PaymentDaysAfterReceive = table.Column<int>(type: "int", nullable: true),
                    isInclusive = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HeaderDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeliveryCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalLineDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ItemTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_PurchaseReceive", x => x.PurchaseReceiveID);
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_LookupValues_PaymentTypeID",
                        column: x => x.PaymentTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_PurchaseOrder_PurchaseOrderID",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrder",
                        principalColumn: "PurchaseOrderID");
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_Supplier_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                    table.ForeignKey(
                        name: "FK_PurchaseReceive_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiveLine",
                columns: table => new
                {
                    PurchaseReceiveLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseReceiveID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Batch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiveLine", x => x.PurchaseReceiveLineID);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiveLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiveLine_PurchaseReceive_PurchaseReceiveID",
                        column: x => x.PurchaseReceiveID,
                        principalTable: "PurchaseReceive",
                        principalColumn: "PurchaseReceiveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiveLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_AccountID",
                table: "PurchaseReceive",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_BusinessUnitID_isActive_Status_ReceiveDate",
                table: "PurchaseReceive",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "ReceiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_BusinessUnitID_PurchaseReceiveCode",
                table: "PurchaseReceive",
                columns: new[] { "BusinessUnitID", "PurchaseReceiveCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_PaymentTypeID",
                table: "PurchaseReceive",
                column: "PaymentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_PurchaseOrderID",
                table: "PurchaseReceive",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_SupplierID",
                table: "PurchaseReceive",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceive_WarehouseID",
                table: "PurchaseReceive",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiveLine_ItemID",
                table: "PurchaseReceiveLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiveLine_PurchaseReceiveID",
                table: "PurchaseReceiveLine",
                column: "PurchaseReceiveID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiveLine_UomID",
                table: "PurchaseReceiveLine",
                column: "UomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReceiveLine");

            migrationBuilder.DropTable(
                name: "PurchaseReceive");
        }
    }
}
