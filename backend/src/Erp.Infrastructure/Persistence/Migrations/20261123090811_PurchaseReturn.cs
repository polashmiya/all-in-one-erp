using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "PurchaseReceiveLine",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnAmount",
                table: "PurchaseReceive",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 35);

            migrationBuilder.CreateTable(
                name: "PurchaseReturn",
                columns: table => new
                {
                    PurchaseReturnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseReturnCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseReceiveID = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderID = table.Column<int>(type: "int", nullable: false),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnReasonID = table.Column<int>(type: "int", nullable: true),
                    isInclusive = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_PurchaseReturn", x => x.PurchaseReturnID);
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_LookupValues_ReturnReasonID",
                        column: x => x.ReturnReasonID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_PurchaseOrder_PurchaseOrderID",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrder",
                        principalColumn: "PurchaseOrderID");
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_PurchaseReceive_PurchaseReceiveID",
                        column: x => x.PurchaseReceiveID,
                        principalTable: "PurchaseReceive",
                        principalColumn: "PurchaseReceiveID");
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_Supplier_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                    table.ForeignKey(
                        name: "FK_PurchaseReturn_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnLine",
                columns: table => new
                {
                    PurchaseReturnLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseReturnID = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseReturnLine", x => x.PurchaseReturnLineID);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLine_PurchaseReturn_PurchaseReturnID",
                        column: x => x.PurchaseReturnID,
                        principalTable: "PurchaseReturn",
                        principalColumn: "PurchaseReturnID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_AccountID",
                table: "PurchaseReturn",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_BusinessUnitID_isActive_Status_ReturnDate",
                table: "PurchaseReturn",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_BusinessUnitID_PurchaseReturnCode",
                table: "PurchaseReturn",
                columns: new[] { "BusinessUnitID", "PurchaseReturnCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_PurchaseOrderID",
                table: "PurchaseReturn",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_PurchaseReceiveID",
                table: "PurchaseReturn",
                column: "PurchaseReceiveID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_ReturnReasonID",
                table: "PurchaseReturn",
                column: "ReturnReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_SupplierID",
                table: "PurchaseReturn",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_WarehouseID",
                table: "PurchaseReturn",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLine_ItemID",
                table: "PurchaseReturnLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLine_PurchaseReturnID",
                table: "PurchaseReturnLine",
                column: "PurchaseReturnID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLine_UomID",
                table: "PurchaseReturnLine",
                column: "UomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReturnLine");

            migrationBuilder.DropTable(
                name: "PurchaseReturn");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "PurchaseReceiveLine");

            migrationBuilder.DropColumn(
                name: "ReturnAmount",
                table: "PurchaseReceive");
        }
    }
}
