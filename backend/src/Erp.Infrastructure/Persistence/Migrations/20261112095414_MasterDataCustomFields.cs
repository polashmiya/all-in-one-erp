using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MasterDataCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "UnitOfMeasure",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 14);

            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "ItemSubCategory",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "ItemMinorCategory",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "ItemDepartment",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);

            migrationBuilder.AddColumn<string>(
                name: "CustomData",
                table: "ItemCategory",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 13);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "UnitOfMeasure");

            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "ItemSubCategory");

            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "ItemMinorCategory");

            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "ItemDepartment");

            migrationBuilder.DropColumn(
                name: "CustomData",
                table: "ItemCategory");
        }
    }
}
