using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionHeader",
                columns: table => new
                {
                    PurchaseRequisitionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseRequisitionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestTypeID = table.Column<int>(type: "int", nullable: true),
                    PurchaseMethodID = table.Column<int>(type: "int", nullable: true),
                    ItemTypeID = table.Column<int>(type: "int", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: true),
                    SectionID = table.Column<int>(type: "int", nullable: true),
                    EmployeeID = table.Column<int>(type: "int", nullable: true),
                    BuyerID = table.Column<int>(type: "int", nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_PurchaseRequisitionHeader", x => x.PurchaseRequisitionID);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_Employee_BuyerID",
                        column: x => x.BuyerID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_Employee_EmployeeID",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_LookupValues_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_LookupValues_ItemTypeID",
                        column: x => x.ItemTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_LookupValues_PurchaseMethodID",
                        column: x => x.PurchaseMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_LookupValues_RequestTypeID",
                        column: x => x.RequestTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_LookupValues_SectionID",
                        column: x => x.SectionID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionHeader_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionRow",
                columns: table => new
                {
                    PurchaseRequisitionRowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseRequisitionID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionRow", x => x.PurchaseRequisitionRowID);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionRow_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionRow_PurchaseRequisitionHeader_PurchaseRequisitionID",
                        column: x => x.PurchaseRequisitionID,
                        principalTable: "PurchaseRequisitionHeader",
                        principalColumn: "PurchaseRequisitionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionRow_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_AccountID",
                table: "PurchaseRequisitionHeader",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_BusinessUnitID_isActive_Status_RequestDate",
                table: "PurchaseRequisitionHeader",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_BusinessUnitID_PurchaseRequisitionCode",
                table: "PurchaseRequisitionHeader",
                columns: new[] { "BusinessUnitID", "PurchaseRequisitionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_BuyerID",
                table: "PurchaseRequisitionHeader",
                column: "BuyerID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_DepartmentID",
                table: "PurchaseRequisitionHeader",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_EmployeeID",
                table: "PurchaseRequisitionHeader",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_ItemTypeID",
                table: "PurchaseRequisitionHeader",
                column: "ItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_PurchaseMethodID",
                table: "PurchaseRequisitionHeader",
                column: "PurchaseMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_RequestTypeID",
                table: "PurchaseRequisitionHeader",
                column: "RequestTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_SectionID",
                table: "PurchaseRequisitionHeader",
                column: "SectionID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionHeader_WarehouseID",
                table: "PurchaseRequisitionHeader",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionRow_ItemID",
                table: "PurchaseRequisitionRow",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionRow_PurchaseRequisitionID",
                table: "PurchaseRequisitionRow",
                column: "PurchaseRequisitionID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionRow_UomID",
                table: "PurchaseRequisitionRow",
                column: "UomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseRequisitionRow");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionHeader");
        }
    }
}
