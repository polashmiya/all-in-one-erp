using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesReturnAndCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "SalesDeliveryLine",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CollectedAmount",
                table: "SalesDelivery",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnAdjusted",
                table: "SalesDelivery",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SalesCollection",
                columns: table => new
                {
                    SalesCollectionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesCollectionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAdvance = table.Column<bool>(type: "bit", nullable: false),
                    OfficeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CollectedByID = table.Column<int>(type: "int", nullable: true),
                    CollectedByName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MoneyReceiptNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BankAccID = table.Column<int>(type: "int", nullable: true),
                    BankAccName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerBankName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerBankBranch = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentTypeID = table.Column<int>(type: "int", nullable: true),
                    InstrumentTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TransactionNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VdsCertificateNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VdsCertificateDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AitCertificateNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AitCertificateDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrencyID = table.Column<int>(type: "int", nullable: true),
                    CurrencyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CollectionAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Vds = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Ait = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExpenseAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceAdjusted = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproveDatetime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalesCollectionGuID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomData = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SalesCollection", x => x.SalesCollectionID);
                    table.ForeignKey(
                        name: "FK_SalesCollection_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCollection_BankAccount_BankAccID",
                        column: x => x.BankAccID,
                        principalTable: "BankAccount",
                        principalColumn: "BankAccID");
                    table.ForeignKey(
                        name: "FK_SalesCollection_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCollection_Customer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesCollection_Employee_CollectedByID",
                        column: x => x.CollectedByID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesCollection_LookupValues_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCollection_LookupValues_InstrumentTypeID",
                        column: x => x.InstrumentTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCollection_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCollection_Offices_OfficeID",
                        column: x => x.OfficeID,
                        principalTable: "Offices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesReturn",
                columns: table => new
                {
                    SalesReturnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesReturnCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SalesDeliveryID = table.Column<int>(type: "int", nullable: false),
                    SalesDeliveryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerOrderID = table.Column<int>(type: "int", nullable: true),
                    CustomerOrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OfficeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesForceID = table.Column<int>(type: "int", nullable: true),
                    SalesForceName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnTypeID = table.Column<int>(type: "int", nullable: true),
                    ReturnTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnReasonID = table.Column<int>(type: "int", nullable: true),
                    ReturnReasonName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceivedByID = table.Column<int>(type: "int", nullable: true),
                    ReceivedByName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnRequestNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnRefNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnRefDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturnRefAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CreditNoteNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreditNoteDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ChallanNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VatChallanNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryMethodID = table.Column<int>(type: "int", nullable: true),
                    DeliveryMethodName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PickupAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsExchange = table.Column<bool>(type: "bit", nullable: false),
                    isInclusive = table.Column<bool>(type: "bit", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ItemTotalAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalSd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalVat = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAITAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SpecialDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChargeRefundAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DeductionLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeductionValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DueAdjusted = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    BankAccID = table.Column<int>(type: "int", nullable: true),
                    BankAccName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SalesReturnLossAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproveDatetime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalesReturnGuID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomData = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SalesReturn", x => x.SalesReturnID);
                    table.ForeignKey(
                        name: "FK_SalesReturn_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_BankAccount_BankAccID",
                        column: x => x.BankAccID,
                        principalTable: "BankAccount",
                        principalColumn: "BankAccID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_CustomerOrder_CustomerOrderID",
                        column: x => x.CustomerOrderID,
                        principalTable: "CustomerOrder",
                        principalColumn: "CustomerOrderID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_Customer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_Employee_ReceivedByID",
                        column: x => x.ReceivedByID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_Employee_SalesForceID",
                        column: x => x.SalesForceID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_LookupValues_DeliveryMethodID",
                        column: x => x.DeliveryMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_LookupValues_ReturnReasonID",
                        column: x => x.ReturnReasonID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_LookupValues_ReturnTypeID",
                        column: x => x.ReturnTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturn_Offices_OfficeID",
                        column: x => x.OfficeID,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturn_SalesDelivery_SalesDeliveryID",
                        column: x => x.SalesDeliveryID,
                        principalTable: "SalesDelivery",
                        principalColumn: "SalesDeliveryID");
                    table.ForeignKey(
                        name: "FK_SalesReturn_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesCollectionLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesCollectionID = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SalesDeliveryID = table.Column<int>(type: "int", nullable: false),
                    SalesDeliveryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CollectionAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Vds = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Ait = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExpenseAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceAdjusted = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SettledAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCollectionLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesCollectionLine_SalesCollection_SalesCollectionID",
                        column: x => x.SalesCollectionID,
                        principalTable: "SalesCollection",
                        principalColumn: "SalesCollectionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesCollectionLine_SalesDelivery_SalesDeliveryID",
                        column: x => x.SalesDeliveryID,
                        principalTable: "SalesDelivery",
                        principalColumn: "SalesDeliveryID");
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesReturnID = table.Column<int>(type: "int", nullable: false),
                    SalesDeliveryLineID = table.Column<int>(type: "int", nullable: false),
                    IsOfferItem = table.Column<bool>(type: "bit", nullable: false),
                    IsDamaged = table.Column<bool>(type: "bit", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CostTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SalesReturnLossAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Batch = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    ItemSerial = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ViewSequenceID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UomID = table.Column<int>(type: "int", nullable: false),
                    UomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LineDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsAmountWiseDiscount = table.Column<bool>(type: "bit", nullable: false),
                    IsDiscountPerQuantity = table.Column<bool>(type: "bit", nullable: false),
                    SdPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SdAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AitPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AitAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WarrantyInMonth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLine_SalesDeliveryLine_SalesDeliveryLineID",
                        column: x => x.SalesDeliveryLineID,
                        principalTable: "SalesDeliveryLine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturnLine_SalesReturn_SalesReturnID",
                        column: x => x.SalesReturnID,
                        principalTable: "SalesReturn",
                        principalColumn: "SalesReturnID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_AccountID",
                table: "SalesCollection",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_BankAccID",
                table: "SalesCollection",
                column: "BankAccID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_BusinessUnitID_isActive_Status_CollectionDate",
                table: "SalesCollection",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "CollectionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_BusinessUnitID_SalesCollectionCode",
                table: "SalesCollection",
                columns: new[] { "BusinessUnitID", "SalesCollectionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_CollectedByID",
                table: "SalesCollection",
                column: "CollectedByID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_CurrencyID",
                table: "SalesCollection",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_CustomerID",
                table: "SalesCollection",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_InstrumentTypeID",
                table: "SalesCollection",
                column: "InstrumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_OfficeID",
                table: "SalesCollection",
                column: "OfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollection_PaymentMethodID",
                table: "SalesCollection",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollectionLine_SalesCollectionID",
                table: "SalesCollectionLine",
                column: "SalesCollectionID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCollectionLine_SalesDeliveryID",
                table: "SalesCollectionLine",
                column: "SalesDeliveryID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_AccountID",
                table: "SalesReturn",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_BankAccID",
                table: "SalesReturn",
                column: "BankAccID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_BusinessUnitID_isActive_Status_ReturnDate",
                table: "SalesReturn",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_BusinessUnitID_SalesReturnCode",
                table: "SalesReturn",
                columns: new[] { "BusinessUnitID", "SalesReturnCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_CustomerID",
                table: "SalesReturn",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_CustomerOrderID",
                table: "SalesReturn",
                column: "CustomerOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_DeliveryMethodID",
                table: "SalesReturn",
                column: "DeliveryMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_OfficeID",
                table: "SalesReturn",
                column: "OfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_PaymentMethodID",
                table: "SalesReturn",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_ReceivedByID",
                table: "SalesReturn",
                column: "ReceivedByID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_ReturnReasonID",
                table: "SalesReturn",
                column: "ReturnReasonID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_ReturnTypeID",
                table: "SalesReturn",
                column: "ReturnTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_SalesDeliveryID",
                table: "SalesReturn",
                column: "SalesDeliveryID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_SalesForceID",
                table: "SalesReturn",
                column: "SalesForceID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturn_WarehouseID",
                table: "SalesReturn",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLine_ItemID",
                table: "SalesReturnLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLine_SalesDeliveryLineID",
                table: "SalesReturnLine",
                column: "SalesDeliveryLineID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLine_SalesReturnID",
                table: "SalesReturnLine",
                column: "SalesReturnID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLine_UomID",
                table: "SalesReturnLine",
                column: "UomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesCollectionLine");

            migrationBuilder.DropTable(
                name: "SalesReturnLine");

            migrationBuilder.DropTable(
                name: "SalesCollection");

            migrationBuilder.DropTable(
                name: "SalesReturn");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "SalesDeliveryLine");

            migrationBuilder.DropColumn(
                name: "CollectedAmount",
                table: "SalesDelivery");

            migrationBuilder.DropColumn(
                name: "ReturnAdjusted",
                table: "SalesDelivery");
        }
    }
}
