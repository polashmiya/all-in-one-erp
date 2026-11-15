using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsernameAndResetCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "ErpUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE ErpUsers SET Username = LEFT(LOWER(REPLACE(LEFT(Email, CHARINDEX('@', Email) - 1), '+', '.')), 40);
                WITH numbered AS (SELECT Username, ROW_NUMBER() OVER (PARTITION BY Username ORDER BY CreatedAt) AS n FROM ErpUsers)
                UPDATE numbered SET Username = Username + CAST(n AS nvarchar(10)) WHERE n > 1;
                """);

            migrationBuilder.CreateTable(
                name: "PasswordResetCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Surface = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpUsers_Username",
                table: "ErpUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_Surface_UserId_CreatedAt",
                table: "PasswordResetCodes",
                columns: new[] { "Surface", "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetCodes");

            migrationBuilder.DropIndex(
                name: "IX_ErpUsers_Username",
                table: "ErpUsers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "ErpUsers");
        }
    }
}
