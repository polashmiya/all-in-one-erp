using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FeatureConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfigVersion",
                table: "AccountFeatures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FeatureSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LabelBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Columns = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureSections_ErpFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ErpFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureSections_FeatureSections_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountSectionSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LabelBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    Columns = table.Column<int>(type: "int", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSectionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSectionSettings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountSectionSettings_FeatureSections_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountSectionSettings_FeatureSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LookupSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ParentFieldKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LabelBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HelpEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HelpBn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PlaceholderEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlaceholderBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ColSpan = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Precision = table.Column<int>(type: "int", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ListOrder = table.Column<int>(type: "int", nullable: true),
                    CanFilter = table.Column<bool>(type: "bit", nullable: false),
                    CanSort = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFields_ErpFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ErpFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureFields_FeatureSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountFieldSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: true),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: true),
                    LabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LabelBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HelpEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HelpBn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PlaceholderEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlaceholderBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    ColSpan = table.Column<int>(type: "int", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    InList = table.Column<bool>(type: "bit", nullable: true),
                    ListOrder = table.Column<int>(type: "int", nullable: true),
                    InFilter = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountFieldSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountFieldSettings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountFieldSettings_FeatureFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FeatureFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountFieldSettings_FeatureSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_AccountId_FieldId",
                table: "AccountFieldSettings",
                columns: new[] { "AccountId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_FieldId",
                table: "AccountFieldSettings",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountFieldSettings_SectionId",
                table: "AccountFieldSettings",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_AccountId_SectionId",
                table: "AccountSectionSettings",
                columns: new[] { "AccountId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_ParentId",
                table: "AccountSectionSettings",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSectionSettings_SectionId",
                table: "AccountSectionSettings",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFields_FeatureId_Key",
                table: "FeatureFields",
                columns: new[] { "FeatureId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFields_SectionId",
                table: "FeatureFields",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSections_FeatureId_Key",
                table: "FeatureSections",
                columns: new[] { "FeatureId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSections_ParentId",
                table: "FeatureSections",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountFieldSettings");

            migrationBuilder.DropTable(
                name: "AccountSectionSettings");

            migrationBuilder.DropTable(
                name: "FeatureFields");

            migrationBuilder.DropTable(
                name: "FeatureSections");

            migrationBuilder.DropColumn(
                name: "ConfigVersion",
                table: "AccountFeatures");
        }
    }
}
