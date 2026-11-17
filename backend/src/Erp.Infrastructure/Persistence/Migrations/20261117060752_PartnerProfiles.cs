using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartnerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CustomerBillingName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerGroupID = table.Column<int>(type: "int", nullable: true),
                    MobileNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OtherContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OtherContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DistrictID = table.Column<int>(type: "int", nullable: true),
                    ThanaID = table.Column<int>(type: "int", nullable: true),
                    IsForeign = table.Column<bool>(type: "bit", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PaymentDays = table.Column<int>(type: "int", nullable: true),
                    PriceTypeID = table.Column<int>(type: "int", nullable: true),
                    TerritoryID = table.Column<int>(type: "int", nullable: true),
                    SalesRepresentativeID = table.Column<int>(type: "int", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    GenderID = table.Column<int>(type: "int", nullable: true),
                    Profession = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MarriageDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BinNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TINNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TradeLicenseNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CentralVATRegistrationNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVATNotApplicable = table.Column<bool>(type: "bit", nullable: false),
                    isVDSApplicable = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Customer", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customer_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customer_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customer_Employee_SalesRepresentativeID",
                        column: x => x.SalesRepresentativeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SupplierBillingName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupplierGroupID = table.Column<int>(type: "int", nullable: true),
                    MobileNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OtherContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OtherContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DistrictID = table.Column<int>(type: "int", nullable: true),
                    ThanaID = table.Column<int>(type: "int", nullable: true),
                    IsForeign = table.Column<bool>(type: "bit", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PaymentDays = table.Column<int>(type: "int", nullable: true),
                    Tolerance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BinNumber = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TINNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TradeLicenseNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CentralVATRegistrationNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVATNotApplicable = table.Column<bool>(type: "bit", nullable: false),
                    isVDSApplicable = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Supplier", x => x.SupplierID);
                    table.ForeignKey(
                        name: "FK_Supplier_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Supplier_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AccountID",
                table: "Customer",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_BusinessUnitID_CustomerCode",
                table: "Customer",
                columns: new[] { "BusinessUnitID", "CustomerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_SalesRepresentativeID",
                table: "Customer",
                column: "SalesRepresentativeID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_AccountID",
                table: "Supplier",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_BusinessUnitID_SupplierCode",
                table: "Supplier",
                columns: new[] { "BusinessUnitID", "SupplierCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Supplier");
        }
    }
}
