using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeesAndUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenByDefault",
                table: "FeatureFields",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "ErpUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameBn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImageSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesignationId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    DepartmentSectionId = table.Column<int>(type: "int", nullable: true),
                    EmployeeTypeId = table.Column<int>(type: "int", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OfficialContact = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyMobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FatherName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    ReligionId = table.Column<int>(type: "int", nullable: true),
                    BloodGroupId = table.Column<int>(type: "int", nullable: true),
                    Nid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PresentAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignatureImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeBusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JoiningDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupervisorId = table.Column<int>(type: "int", nullable: true),
                    LineManagerId = table.Column<int>(type: "int", nullable: true),
                    AttendanceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSalesRepresentative = table.Column<bool>(type: "bit", nullable: false),
                    IsServiceStaff = table.Column<bool>(type: "bit", nullable: false),
                    CustomData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Employee_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_BusinessUnits_HomeBusinessUnitId",
                        column: x => x.HomeBusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_Employee_LineManagerId",
                        column: x => x.LineManagerId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_Employee_Employee_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_Employee_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employee_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpUsers_EmployeeId",
                table: "ErpUsers",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_AccountID_Code",
                table: "Employee",
                columns: new[] { "AccountID", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_HomeBusinessUnitId",
                table: "Employee",
                column: "HomeBusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_LineManagerId",
                table: "Employee",
                column: "LineManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_OfficeId",
                table: "Employee",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_SupervisorId",
                table: "Employee",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_WarehouseId",
                table: "Employee",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ErpUsers_Employee_EmployeeId",
                table: "ErpUsers",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "EmployeeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ErpUsers_Employee_EmployeeId",
                table: "ErpUsers");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_ErpUsers_EmployeeId",
                table: "ErpUsers");

            migrationBuilder.DropColumn(
                name: "IsHiddenByDefault",
                table: "FeatureFields");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "ErpUsers");
        }
    }
}
