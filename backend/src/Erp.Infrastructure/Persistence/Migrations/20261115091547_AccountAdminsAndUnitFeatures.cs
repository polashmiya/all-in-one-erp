using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountAdminsAndUnitFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPrimaryAdmin",
                table: "ErpUsers",
                newName: "IsAccountAdmin");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "UserBusinessUnits",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserBusinessUnits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ErpUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessUnitFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUnitFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessUnitFeatures_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessUnitFeatures_BusinessUnits_BusinessUnitId",
                        column: x => x.BusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessUnitFeatures_ErpFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ErpFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnitFeatures_AccountId",
                table: "BusinessUnitFeatures",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnitFeatures_BusinessUnitId_FeatureId",
                table: "BusinessUnitFeatures",
                columns: new[] { "BusinessUnitId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUnitFeatures_FeatureId",
                table: "BusinessUnitFeatures",
                column: "FeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessUnitFeatures");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserBusinessUnits");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ErpUsers");

            migrationBuilder.RenameColumn(
                name: "IsAccountAdmin",
                table: "ErpUsers",
                newName: "IsPrimaryAdmin");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "UserBusinessUnits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
