using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAndAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChartOfAccount",
                columns: table => new
                {
                    ChartOfAccID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChartOfAccCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChartOfAccName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ChartOfAccType = table.Column<int>(type: "int", nullable: false),
                    IsGroup = table.Column<bool>(type: "bit", nullable: false),
                    ParentID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ChartOfAccount", x => x.ChartOfAccID);
                    table.ForeignKey(
                        name: "FK_ChartOfAccount_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChartOfAccount_ChartOfAccount_ParentID",
                        column: x => x.ParentID,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustment",
                columns: table => new
                {
                    StockAdjustmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockAdjustmentCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdjustmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonID = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StockAdjustment", x => x.StockAdjustmentID);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_LookupValues_ReasonID",
                        column: x => x.ReasonID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustment_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBalance_BusinessUnits_BusinessUnitId",
                        column: x => x.BusinessUnitId,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalance_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "ItemID");
                    table.ForeignKey(
                        name: "FK_StockBalance_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTransfer",
                columns: table => new
                {
                    StockTransferID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTransferCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TransferDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FromWarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_StockTransfer", x => x.StockTransferID);
                    table.ForeignKey(
                        name: "FK_StockTransfer_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfer_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfer_Warehouses_FromWarehouseID",
                        column: x => x.FromWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTransfer_Warehouses_ToWarehouseID",
                        column: x => x.ToWarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubLedgerHeader",
                columns: table => new
                {
                    SubLedgerHeaderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubLedgerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JournalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TransactionRefID = table.Column<int>(type: "int", nullable: true),
                    TransactionRefCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsManualJV = table.Column<bool>(type: "bit", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SubLedgerHeader", x => x.SubLedgerHeaderID);
                    table.ForeignKey(
                        name: "FK_SubLedgerHeader_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubLedgerHeader_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostingAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostingKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChartOfAccID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingAccount_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostingAccount_ChartOfAccount_ChartOfAccID",
                        column: x => x.ChartOfAccID,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLine",
                columns: table => new
                {
                    StockAdjustmentLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockAdjustmentID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLine", x => x.StockAdjustmentLineID);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID");
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLine_StockAdjustment_StockAdjustmentID",
                        column: x => x.StockAdjustmentID,
                        principalTable: "StockAdjustment",
                        principalColumn: "StockAdjustmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID");
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLine",
                columns: table => new
                {
                    StockTransferLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTransferID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLine", x => x.StockTransferLineID);
                    table.ForeignKey(
                        name: "FK_StockTransferLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID");
                    table.ForeignKey(
                        name: "FK_StockTransferLine_StockTransfer_StockTransferID",
                        column: x => x.StockTransferID,
                        principalTable: "StockTransfer",
                        principalColumn: "StockTransferID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransferLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryHeader",
                columns: table => new
                {
                    InventoryTransID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TransactionRefID = table.Column<int>(type: "int", nullable: false),
                    TransactionRefCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PartnerID = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    SubLedgerHeaderID = table.Column<int>(type: "int", nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryHeader", x => x.InventoryTransID);
                    table.ForeignKey(
                        name: "FK_InventoryHeader_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryHeader_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryHeader_SubLedgerHeader_SubLedgerHeaderID",
                        column: x => x.SubLedgerHeaderID,
                        principalTable: "SubLedgerHeader",
                        principalColumn: "SubLedgerHeaderID");
                    table.ForeignKey(
                        name: "FK_InventoryHeader_Supplier_PartnerID",
                        column: x => x.PartnerID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "SubLedgerRow",
                columns: table => new
                {
                    RowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubLedgerHeaderID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ChartOfAccID = table.Column<int>(type: "int", nullable: false),
                    PartnerID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubLedgerRow", x => x.RowID);
                    table.ForeignKey(
                        name: "FK_SubLedgerRow_ChartOfAccount_ChartOfAccID",
                        column: x => x.ChartOfAccID,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                    table.ForeignKey(
                        name: "FK_SubLedgerRow_SubLedgerHeader_SubLedgerHeaderID",
                        column: x => x.SubLedgerHeaderID,
                        principalTable: "SubLedgerHeader",
                        principalColumn: "SubLedgerHeaderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubLedgerRow_Supplier_PartnerID",
                        column: x => x.PartnerID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "InventoryLine",
                columns: table => new
                {
                    InventoryLineID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TransValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLine", x => x.InventoryLineID);
                    table.ForeignKey(
                        name: "FK_InventoryLine_InventoryHeader_InventoryTransID",
                        column: x => x.InventoryTransID,
                        principalTable: "InventoryHeader",
                        principalColumn: "InventoryTransID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID");
                    table.ForeignKey(
                        name: "FK_InventoryLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID");
                    table.ForeignKey(
                        name: "FK_InventoryLine_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_AccountID_ChartOfAccCode",
                table: "ChartOfAccount",
                columns: new[] { "AccountID", "ChartOfAccCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_AccountID_ChartOfAccName",
                table: "ChartOfAccount",
                columns: new[] { "AccountID", "ChartOfAccName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_ParentID",
                table: "ChartOfAccount",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_AccountID",
                table: "InventoryHeader",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_BusinessUnitID_InventoryTransType_TransactionRefID",
                table: "InventoryHeader",
                columns: new[] { "BusinessUnitID", "InventoryTransType", "TransactionRefID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_BusinessUnitID_PostingDate",
                table: "InventoryHeader",
                columns: new[] { "BusinessUnitID", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_PartnerID",
                table: "InventoryHeader",
                column: "PartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHeader_SubLedgerHeaderID",
                table: "InventoryHeader",
                column: "SubLedgerHeaderID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLine_InventoryTransID",
                table: "InventoryLine",
                column: "InventoryTransID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLine_ItemID",
                table: "InventoryLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLine_UomID",
                table: "InventoryLine",
                column: "UomID");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLine_WarehouseID_ItemID",
                table: "InventoryLine",
                columns: new[] { "WarehouseID", "ItemID" });

            migrationBuilder.CreateIndex(
                name: "IX_PostingAccount_AccountId_PostingKey",
                table: "PostingAccount",
                columns: new[] { "AccountId", "PostingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingAccount_ChartOfAccID",
                table: "PostingAccount",
                column: "ChartOfAccID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_AccountID",
                table: "StockAdjustment",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_BusinessUnitID_isActive_Status_AdjustmentDate",
                table: "StockAdjustment",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "AdjustmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_BusinessUnitID_StockAdjustmentCode",
                table: "StockAdjustment",
                columns: new[] { "BusinessUnitID", "StockAdjustmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_ReasonID",
                table: "StockAdjustment",
                column: "ReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_WarehouseID",
                table: "StockAdjustment",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLine_ItemID",
                table: "StockAdjustmentLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLine_StockAdjustmentID",
                table: "StockAdjustmentLine",
                column: "StockAdjustmentID");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLine_UomID",
                table: "StockAdjustmentLine",
                column: "UomID");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_BusinessUnitId_WarehouseId_ItemId",
                table: "StockBalance",
                columns: new[] { "BusinessUnitId", "WarehouseId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_ItemId",
                table: "StockBalance",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_WarehouseId",
                table: "StockBalance",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_AccountID",
                table: "StockTransfer",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_BusinessUnitID_isActive_Status_TransferDate",
                table: "StockTransfer",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "TransferDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_BusinessUnitID_StockTransferCode",
                table: "StockTransfer",
                columns: new[] { "BusinessUnitID", "StockTransferCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_FromWarehouseID",
                table: "StockTransfer",
                column: "FromWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfer_ToWarehouseID",
                table: "StockTransfer",
                column: "ToWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLine_ItemID",
                table: "StockTransferLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLine_StockTransferID",
                table: "StockTransferLine",
                column: "StockTransferID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLine_UomID",
                table: "StockTransferLine",
                column: "UomID");

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_AccountID",
                table: "SubLedgerHeader",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_isActive_Status_JournalDate",
                table: "SubLedgerHeader",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "JournalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_SubLedgerCode",
                table: "SubLedgerHeader",
                columns: new[] { "BusinessUnitID", "SubLedgerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerHeader_BusinessUnitID_TransactionType_TransactionRefID",
                table: "SubLedgerHeader",
                columns: new[] { "BusinessUnitID", "TransactionType", "TransactionRefID" },
                unique: true,
                filter: "[TransactionRefID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerRow_ChartOfAccID_SubLedgerHeaderID",
                table: "SubLedgerRow",
                columns: new[] { "ChartOfAccID", "SubLedgerHeaderID" })
                .Annotation("SqlServer:Include", new[] { "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerRow_PartnerID_SubLedgerHeaderID",
                table: "SubLedgerRow",
                columns: new[] { "PartnerID", "SubLedgerHeaderID" })
                .Annotation("SqlServer:Include", new[] { "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerRow_SubLedgerHeaderID",
                table: "SubLedgerRow",
                column: "SubLedgerHeaderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLine");

            migrationBuilder.DropTable(
                name: "PostingAccount");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLine");

            migrationBuilder.DropTable(
                name: "StockBalance");

            migrationBuilder.DropTable(
                name: "StockTransferLine");

            migrationBuilder.DropTable(
                name: "SubLedgerRow");

            migrationBuilder.DropTable(
                name: "InventoryHeader");

            migrationBuilder.DropTable(
                name: "StockAdjustment");

            migrationBuilder.DropTable(
                name: "StockTransfer");

            migrationBuilder.DropTable(
                name: "ChartOfAccount");

            migrationBuilder.DropTable(
                name: "SubLedgerHeader");
        }
    }
}
