using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureFields_FeatureId_Key",
                table: "FeatureFields");

            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "Item",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 51);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "FeatureFields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFields_AccountId",
                table: "FeatureFields",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFields_FeatureId_AccountId_Key",
                table: "FeatureFields",
                columns: new[] { "FeatureId", "AccountId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FeatureFields_Accounts_AccountId",
                table: "FeatureFields",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeatureFields_Accounts_AccountId",
                table: "FeatureFields");

            migrationBuilder.DropIndex(
                name: "IX_FeatureFields_AccountId",
                table: "FeatureFields");

            migrationBuilder.DropIndex(
                name: "IX_FeatureFields_FeatureId_AccountId_Key",
                table: "FeatureFields");

            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "FeatureFields");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFields_FeatureId_Key",
                table: "FeatureFields",
                columns: new[] { "FeatureId", "Key" },
                unique: true);
        }
    }
}
