using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesQuotationHeader",
                columns: table => new
                {
                    SalesQuotationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuotationTypeID = table.Column<int>(type: "int", nullable: true),
                    SQVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionCreateDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OfficeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerID = table.Column<int>(type: "int", nullable: false),
                    PartnerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SalesForceID = table.Column<int>(type: "int", nullable: true),
                    SalesForceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReferenceID = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(1250)", maxLength: 1250, nullable: true),
                    BillingContactPerson = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BillingPhoneNo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ShippingContactPerson = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ShippingAddressID = table.Column<int>(type: "int", nullable: true),
                    ShippingAddressName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    isVATInclusive = table.Column<bool>(type: "bit", nullable: false),
                    IsGrossDiscount = table.Column<bool>(type: "bit", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DeliveryCharge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExtraCharge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Subjects = table.Column<string>(type: "nvarchar(1250)", maxLength: 1250, nullable: true),
                    BodyNarration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterNarration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    isExpired = table.Column<bool>(type: "bit", nullable: false, computedColumnSql: "CONVERT([bit], CASE WHEN [ValidityDate] < CONVERT([date], GETDATE()) THEN 1 ELSE 0 END)"),
                    isSO = table.Column<bool>(type: "bit", nullable: false),
                    SoStatusID = table.Column<int>(type: "int", nullable: false),
                    SoStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false, computedColumnSql: "CASE [SoStatusID] WHEN 0 THEN N'Created' WHEN 1 THEN N'Not Created' ELSE N'Rejected' END"),
                    SalesQuotationGuID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_SalesQuotationHeader", x => x.SalesQuotationID);
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_Customer_PartnerID",
                        column: x => x.PartnerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_Employee_SalesForceID",
                        column: x => x.SalesForceID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_LookupValues_QuotationTypeID",
                        column: x => x.QuotationTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_LookupValues_ShippingAddressID",
                        column: x => x.ShippingAddressID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_Offices_OfficeID",
                        column: x => x.OfficeID,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesQuotationHeader_SalesQuotationHeader_ReferenceID",
                        column: x => x.ReferenceID,
                        principalTable: "SalesQuotationHeader",
                        principalColumn: "SalesQuotationID");
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationRow",
                columns: table => new
                {
                    SalesQuotationRowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationHeaderID = table.Column<int>(type: "int", nullable: false),
                    ViewSequenceID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    UomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsAmountWiseDiscount = table.Column<bool>(type: "bit", nullable: false),
                    isDiscountPerQuantity = table.Column<bool>(type: "bit", nullable: false),
                    SDPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SDAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VATPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VATAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AitPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AitAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WarrantyInMonth = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ApproveQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovePrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApproveAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationRow", x => x.SalesQuotationRowID);
                    table.ForeignKey(
                        name: "FK_SalesQuotationRow_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationRow_SalesQuotationHeader_SalesQuotationHeaderID",
                        column: x => x.SalesQuotationHeaderID,
                        principalTable: "SalesQuotationHeader",
                        principalColumn: "SalesQuotationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotationRow_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_AccountID",
                table: "SalesQuotationHeader",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_BusinessUnitID_isActive_Status_QuotationDate",
                table: "SalesQuotationHeader",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "QuotationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_BusinessUnitID_SalesQuotationCode_SQVersion",
                table: "SalesQuotationHeader",
                columns: new[] { "BusinessUnitID", "SalesQuotationCode", "SQVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_OfficeID",
                table: "SalesQuotationHeader",
                column: "OfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_PartnerID",
                table: "SalesQuotationHeader",
                column: "PartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_QuotationTypeID",
                table: "SalesQuotationHeader",
                column: "QuotationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_ReferenceID",
                table: "SalesQuotationHeader",
                column: "ReferenceID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_SalesForceID",
                table: "SalesQuotationHeader",
                column: "SalesForceID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationHeader_ShippingAddressID",
                table: "SalesQuotationHeader",
                column: "ShippingAddressID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationRow_ItemID",
                table: "SalesQuotationRow",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationRow_SalesQuotationHeaderID",
                table: "SalesQuotationRow",
                column: "SalesQuotationHeaderID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationRow_UomID",
                table: "SalesQuotationRow",
                column: "UomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesQuotationRow");

            migrationBuilder.DropTable(
                name: "SalesQuotationHeader");
        }
    }
}
