using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BusinessUnitFeatureSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountSectionSettings_AccountId_SectionId",
                table: "AccountSectionSettings");

            migrationBuilder.DropIndex(
                name: "IX_AccountFieldSettings_AccountId_FieldId",
                table: "AccountFieldSettings");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessUnitId",
                table: "AccountSectionSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessUnitId",
                table: "AccountFieldSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_AccountId_BusinessUnitId_SectionId",
                table: "AccountSectionSettings",
                columns: new[] { "AccountId", "BusinessUnitId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_BusinessUnitId",
                table: "AccountSectionSettings",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_AccountId_BusinessUnitId_FieldId",
                table: "AccountFieldSettings",
                columns: new[] { "AccountId", "BusinessUnitId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_BusinessUnitId",
                table: "AccountFieldSettings",
                column: "BusinessUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountFieldSettings_BusinessUnits_BusinessUnitId",
                table: "AccountFieldSettings",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountSectionSettings_BusinessUnits_BusinessUnitId",
                table: "AccountSectionSettings",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountFieldSettings_BusinessUnits_BusinessUnitId",
                table: "AccountFieldSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountSectionSettings_BusinessUnits_BusinessUnitId",
                table: "AccountSectionSettings");

            migrationBuilder.DropIndex(
                name: "IX_AccountSectionSettings_AccountId_BusinessUnitId_SectionId",
                table: "AccountSectionSettings");

            migrationBuilder.DropIndex(
                name: "IX_AccountSectionSettings_BusinessUnitId",
                table: "AccountSectionSettings");

            migrationBuilder.DropIndex(
                name: "IX_AccountFieldSettings_AccountId_BusinessUnitId_FieldId",
                table: "AccountFieldSettings");

            migrationBuilder.DropIndex(
                name: "IX_AccountFieldSettings_BusinessUnitId",
                table: "AccountFieldSettings");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "AccountSectionSettings");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "AccountFieldSettings");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_AccountId_SectionId",
                table: "AccountSectionSettings",
                columns: new[] { "AccountId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_AccountId_FieldId",
                table: "AccountFieldSettings",
                columns: new[] { "AccountId", "FieldId" },
                unique: true);
        }
    }
}
