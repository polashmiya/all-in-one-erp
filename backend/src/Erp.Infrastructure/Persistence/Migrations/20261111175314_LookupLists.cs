using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LookupLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LookupTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameBn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                columns: table => new
                {
                    LookupValueID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LookupTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameBn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_LookupValues", x => x.LookupValueID);
                    table.ForeignKey(
                        name: "FK_LookupValues_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupTypes_LookupTypeID",
                        column: x => x.LookupTypeID,
                        principalTable: "LookupTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Item_ItemTypeID",
                table: "Item",
                column: "ItemTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Item_PurchaseSupplyTypeID",
                table: "Item",
                column: "PurchaseSupplyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Item_SalesSupplyTypeID",
                table: "Item",
                column: "SalesSupplyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_LookupTypes_Key",
                table: "LookupTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_AccountID_LookupTypeID_Code",
                table: "LookupValues",
                columns: new[] { "AccountID", "LookupTypeID", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_LookupTypeID",
                table: "LookupValues",
                column: "LookupTypeID");

            migrationBuilder.Sql("""
                DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
                DECLARE @itemType uniqueidentifier = NEWID();
                DECLARE @supplyType uniqueidentifier = NEWID();

                INSERT INTO LookupTypes (Id, [Key], NameEn, NameBn, IsActive, CreatedAt) VALUES
                    (@itemType, N'item-type', N'Item type', N'আইটেমের ধরন', 1, @now),
                    (@supplyType, N'vat-supply-type', N'VAT supply type', N'ভ্যাট সাপ্লাই টাইপ', 1, @now);

                INSERT INTO LookupValues (LookupTypeID, Code, NameEn, NameBn, SortOrder, IsDefault, AccountID, isActive, CreatedAt)
                SELECT d.TypeId, d.Code, d.NameEn, d.NameBn, d.SortOrder, d.IsDefault, a.Id, 1, GETUTCDATE()
                FROM Accounts a CROSS JOIN (VALUES
                    (@itemType, N'FG', N'Finished Goods', N'তৈরি পণ্য', 10, 0),
                    (@itemType, N'RM', N'Raw Material', N'কাঁচামাল', 20, 0),
                    (@itemType, N'SFG', N'Semi-Finished Goods', N'আধা-তৈরি পণ্য', 30, 0),
                    (@itemType, N'PM', N'Packaging Material', N'প্যাকেজিং উপকরণ', 40, 0),
                    (@itemType, N'CON', N'Consumable', N'ভোগ্য সামগ্রী', 50, 0),
                    (@itemType, N'SP', N'Spare Parts', N'খুচরা যন্ত্রাংশ', 60, 0),
                    (@itemType, N'SRV', N'Service', N'সেবা', 70, 0),
                    (@itemType, N'FA', N'Fixed Asset', N'স্থায়ী সম্পদ', 80, 0),
                    (@supplyType, N'STD', N'Standard VAT', N'স্ট্যান্ডার্ড ভ্যাট', 10, 1),
                    (@supplyType, N'RED', N'Reduced VAT', N'হ্রাসকৃত ভ্যাট', 20, 0),
                    (@supplyType, N'ZERO', N'Zero Rated', N'শূন্য হার', 30, 0),
                    (@supplyType, N'EXEMPT', N'Exempted', N'অব্যাহতিপ্রাপ্ত', 40, 0),
                    (@supplyType, N'SPECIFIC', N'Specific VAT', N'সুনির্দিষ্ট ভ্যাট', 50, 0),
                    (@supplyType, N'MRP', N'MRP Based', N'এমআরপি ভিত্তিক', 60, 0)
                ) AS d(TypeId, Code, NameEn, NameBn, SortOrder, IsDefault);

                UPDATE i SET ItemTypeID = v.LookupValueID
                FROM Item i LEFT JOIN LookupValues v
                    ON v.AccountID = i.AccountID AND v.LookupTypeID = @itemType AND v.NameEn = i.ItemTypeName;

                UPDATE i SET PurchaseSupplyTypeID = v.LookupValueID
                FROM Item i LEFT JOIN LookupValues v
                    ON v.AccountID = i.AccountID AND v.LookupTypeID = @supplyType AND v.NameEn = i.PurchaseSupplyTypeName;

                UPDATE i SET SalesSupplyTypeID = v.LookupValueID
                FROM Item i LEFT JOIN LookupValues v
                    ON v.AccountID = i.AccountID AND v.LookupTypeID = @supplyType AND v.NameEn = i.SalesSupplyTypeName;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_LookupValues_ItemTypeID",
                table: "Item",
                column: "ItemTypeID",
                principalTable: "LookupValues",
                principalColumn: "LookupValueID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_LookupValues_PurchaseSupplyTypeID",
                table: "Item",
                column: "PurchaseSupplyTypeID",
                principalTable: "LookupValues",
                principalColumn: "LookupValueID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_LookupValues_SalesSupplyTypeID",
                table: "Item",
                column: "SalesSupplyTypeID",
                principalTable: "LookupValues",
                principalColumn: "LookupValueID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Item_LookupValues_ItemTypeID",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_LookupValues_PurchaseSupplyTypeID",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_LookupValues_SalesSupplyTypeID",
                table: "Item");

            migrationBuilder.DropTable(
                name: "LookupValues");

            migrationBuilder.DropTable(
                name: "LookupTypes");

            migrationBuilder.DropIndex(
                name: "IX_Item_ItemTypeID",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_PurchaseSupplyTypeID",
                table: "Item");

            migrationBuilder.DropIndex(
                name: "IX_Item_SalesSupplyTypeID",
                table: "Item");
        }
    }
}
