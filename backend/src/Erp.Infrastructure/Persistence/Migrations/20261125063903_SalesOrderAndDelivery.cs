using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesOrderAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "SubLedgerRow",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerOrder",
                columns: table => new
                {
                    CustomerOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerOrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesQuotationID = table.Column<int>(type: "int", nullable: true),
                    SalesQuotationCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdjustedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AdvanceRemainingAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, computedColumnSql: "[AdvanceAmount] - [AdjustedAmount]"),
                    WalletCommision = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DueLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    POAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CustomerPoRefAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SalesTermID = table.Column<int>(type: "int", nullable: true),
                    SalesTermName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DMSInvoise = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsForeigne = table.Column<bool>(type: "bit", nullable: false),
                    LcTypeID = table.Column<int>(type: "int", nullable: true),
                    LcTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LcNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportLcID = table.Column<int>(type: "int", nullable: true),
                    ProformaInvoiceID = table.Column<int>(type: "int", nullable: true),
                    PaymentTermsID = table.Column<int>(type: "int", nullable: true),
                    PaymentTermsName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAllowPartialShipment = table.Column<bool>(type: "bit", nullable: false),
                    IsAllowTransitShipment = table.Column<bool>(type: "bit", nullable: false),
                    IsPickPackConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ReasonID = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Review = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ClosedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OfficeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesForceID = table.Column<int>(type: "int", nullable: true),
                    SalesForceName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceTypeID = table.Column<int>(type: "int", nullable: true),
                    PriceType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferredByPartnerID = table.Column<int>(type: "int", nullable: true),
                    ReferredByPartnerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerPO = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerPoDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentTypeID = table.Column<int>(type: "int", nullable: true),
                    PaymentTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    BankAccID = table.Column<int>(type: "int", nullable: true),
                    BankAccName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ShippingContactPerson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShippingAddressID = table.Column<int>(type: "int", nullable: true),
                    ShippingAddressName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    isInclusive = table.Column<bool>(type: "bit", nullable: false),
                    IsGrossDiscount = table.Column<bool>(type: "bit", nullable: false),
                    HeaderDiscountPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalLineDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SpecialDiscountID = table.Column<int>(type: "int", nullable: true),
                    SpecialDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OthersCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChartOfAccIDOtherCharge = table.Column<int>(type: "int", nullable: true),
                    ExtraCharge = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChartOfAccIDExtraOtherCharge = table.Column<int>(type: "int", nullable: true),
                    DeductionLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeductionValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FreeTypeID = table.Column<int>(type: "int", nullable: true),
                    FreeTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyID = table.Column<int>(type: "int", nullable: true),
                    CurrencyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ItemTotalAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscoundItemTotalPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OfferItemTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalSd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalVat = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAITAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReceiveAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PendingAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsAdvanceAdjust = table.Column<bool>(type: "bit", nullable: false),
                    WalletID = table.Column<int>(type: "int", nullable: true),
                    SupplierContributionAmountPerInvoice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CustomOfferID = table.Column<int>(type: "int", nullable: true),
                    CustomOfferName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Attachment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproveDatetime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerOrderGuID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomData = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrder", x => x.CustomerOrderID);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_BankAccount_BankAccID",
                        column: x => x.BankAccID,
                        principalTable: "BankAccount",
                        principalColumn: "BankAccID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_ChartOfAccount_ChartOfAccIDExtraOtherCharge",
                        column: x => x.ChartOfAccIDExtraOtherCharge,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_ChartOfAccount_ChartOfAccIDOtherCharge",
                        column: x => x.ChartOfAccIDOtherCharge,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Customer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Customer_ReferredByPartnerID",
                        column: x => x.ReferredByPartnerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Employee_SalesForceID",
                        column: x => x.SalesForceID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_FreeTypeID",
                        column: x => x.FreeTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_LcTypeID",
                        column: x => x.LcTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_PaymentTermsID",
                        column: x => x.PaymentTermsID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_PaymentTypeID",
                        column: x => x.PaymentTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_PriceTypeID",
                        column: x => x.PriceTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_SalesTermID",
                        column: x => x.SalesTermID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_LookupValues_ShippingAddressID",
                        column: x => x.ShippingAddressID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Offices_OfficeID",
                        column: x => x.OfficeID,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_SalesQuotationHeader_SalesQuotationID",
                        column: x => x.SalesQuotationID,
                        principalTable: "SalesQuotationHeader",
                        principalColumn: "SalesQuotationID");
                    table.ForeignKey(
                        name: "FK_CustomerOrder_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrderLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerOrderID = table.Column<int>(type: "int", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ScheduledQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DemandQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LineDiscountRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, computedColumnSql: "CONVERT([decimal](18,2), CASE WHEN [Quantity] = 0 THEN 0 ELSE [LineDiscount] / [Quantity] END)"),
                    OfferName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SellingItemQtyForOffer = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SetUpFreeQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FOBRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FreightRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    Sd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Vat = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AitPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AitAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WarrantyInMonth = table.Column<int>(type: "int", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ItemBasePriceInclusive = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderDiscountProportion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderCostProportion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsOfferItem = table.Column<bool>(type: "bit", nullable: false),
                    OfferItemID = table.Column<int>(type: "int", nullable: true),
                    OfferItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OfferItemQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FreeTypeID = table.Column<int>(type: "int", nullable: true),
                    FreeTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountID = table.Column<int>(type: "int", nullable: true),
                    SpecialDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SupplierDiscountContributionInPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SupplierDiscountContributionInAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ProformaInvoiceID = table.Column<int>(type: "int", nullable: true),
                    PINo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    numBdPortQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    numForeignPortQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    strShelfLife = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParentRowID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLine_CustomerOrder_CustomerOrderID",
                        column: x => x.CustomerOrderID,
                        principalTable: "CustomerOrder",
                        principalColumn: "CustomerOrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLine_LookupValues_FreeTypeID",
                        column: x => x.FreeTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesDelivery",
                columns: table => new
                {
                    SalesDeliveryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesDeliveryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryTypeID = table.Column<int>(type: "int", nullable: false),
                    CustomerOrderID = table.Column<int>(type: "int", nullable: true),
                    CustomerOrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChallanNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VatChallanNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BillNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefInvoice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnonymousAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    DeliveryMethodID = table.Column<int>(type: "int", nullable: true),
                    DeliveryMethodName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CourierOrEmployeeID = table.Column<int>(type: "int", nullable: true),
                    CourierOrEmployeeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AlternativeDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AdvanceBalanceAdjust = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CustomerNetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmountWithInterest = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalNoOfInstallment = table.Column<int>(type: "int", nullable: false),
                    InstallmentStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InstallmentTypeID = table.Column<int>(type: "int", nullable: true),
                    InstallmentType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AmountPerInstallment = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ComissionPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EarlyPaymentDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsPosSales = table.Column<bool>(type: "bit", nullable: false),
                    IsExportSales = table.Column<bool>(type: "bit", nullable: false),
                    IsServiceRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsBillCreated = table.Column<bool>(type: "bit", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedByName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ActualDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    strGRNNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    dteGRNDate = table.Column<DateOnly>(type: "date", nullable: true),
                    numGRNAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    GRNCollectionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    intCollectedByID = table.Column<int>(type: "int", nullable: true),
                    strCollectedByName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfirmAttachment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DueInvoiceRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DueInvoicePendingReasonID = table.Column<int>(type: "int", nullable: true),
                    DueInvoicePendingReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoicePDFCount = table.Column<int>(type: "int", nullable: false),
                    ExternalApiSync = table.Column<bool>(type: "bit", nullable: false),
                    SmsTransactionID = table.Column<int>(type: "int", nullable: true),
                    ProjectID = table.Column<int>(type: "int", nullable: true),
                    JobOrderID = table.Column<int>(type: "int", nullable: true),
                    ScheduleID = table.Column<int>(type: "int", nullable: true),
                    intOutletID = table.Column<int>(type: "int", nullable: true),
                    strOutletName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDistributorReceive = table.Column<bool>(type: "bit", nullable: false),
                    DistributorReceiveByID = table.Column<int>(type: "int", nullable: true),
                    DistributorReceiveByName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DistributorReceiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DistributorReceiveRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessUnitID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OfficeID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesForceID = table.Column<int>(type: "int", nullable: true),
                    SalesForceName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceTypeID = table.Column<int>(type: "int", nullable: true),
                    PriceType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferredByPartnerID = table.Column<int>(type: "int", nullable: true),
                    ReferredByPartnerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerPO = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerPoDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentTypeID = table.Column<int>(type: "int", nullable: true),
                    PaymentTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    BankAccID = table.Column<int>(type: "int", nullable: true),
                    BankAccName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstrumentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ShippingContactPerson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShippingAddressID = table.Column<int>(type: "int", nullable: true),
                    ShippingAddressName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    isInclusive = table.Column<bool>(type: "bit", nullable: false),
                    IsGrossDiscount = table.Column<bool>(type: "bit", nullable: false),
                    HeaderDiscountPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalLineDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetDiscount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SpecialDiscountID = table.Column<int>(type: "int", nullable: true),
                    SpecialDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OthersCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChartOfAccIDOtherCharge = table.Column<int>(type: "int", nullable: true),
                    ExtraCharge = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChartOfAccIDExtraOtherCharge = table.Column<int>(type: "int", nullable: true),
                    DeductionLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeductionValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FreeTypeID = table.Column<int>(type: "int", nullable: true),
                    FreeTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyID = table.Column<int>(type: "int", nullable: true),
                    CurrencyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ItemTotalAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscoundItemTotalPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OfferItemTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalSd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalVat = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalAITAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReceiveAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PendingAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsAdvanceAdjust = table.Column<bool>(type: "bit", nullable: false),
                    WalletID = table.Column<int>(type: "int", nullable: true),
                    SupplierContributionAmountPerInvoice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CustomOfferID = table.Column<int>(type: "int", nullable: true),
                    CustomOfferName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    strSalesDeliveryAttatchment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproveDatetime = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApproveByID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalesDeliveryGuID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomData = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDelivery", x => x.SalesDeliveryID);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_BankAccount_BankAccID",
                        column: x => x.BankAccID,
                        principalTable: "BankAccount",
                        principalColumn: "BankAccID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_BusinessUnits_BusinessUnitID",
                        column: x => x.BusinessUnitID,
                        principalTable: "BusinessUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_ChartOfAccount_ChartOfAccIDExtraOtherCharge",
                        column: x => x.ChartOfAccIDExtraOtherCharge,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_ChartOfAccount_ChartOfAccIDOtherCharge",
                        column: x => x.ChartOfAccIDOtherCharge,
                        principalTable: "ChartOfAccount",
                        principalColumn: "ChartOfAccID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_CustomerOrder_CustomerOrderID",
                        column: x => x.CustomerOrderID,
                        principalTable: "CustomerOrder",
                        principalColumn: "CustomerOrderID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Customer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Customer_ReferredByPartnerID",
                        column: x => x.ReferredByPartnerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Employee_CourierOrEmployeeID",
                        column: x => x.CourierOrEmployeeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Employee_SalesForceID",
                        column: x => x.SalesForceID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Employee_intCollectedByID",
                        column: x => x.intCollectedByID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_DeliveryMethodID",
                        column: x => x.DeliveryMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_FreeTypeID",
                        column: x => x.FreeTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_InstallmentTypeID",
                        column: x => x.InstallmentTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_PaymentMethodID",
                        column: x => x.PaymentMethodID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_PaymentTypeID",
                        column: x => x.PaymentTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_PriceTypeID",
                        column: x => x.PriceTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_LookupValues_ShippingAddressID",
                        column: x => x.ShippingAddressID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Offices_OfficeID",
                        column: x => x.OfficeID,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesDelivery_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesDeliveryLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesDeliveryID = table.Column<int>(type: "int", nullable: false),
                    intCustomerOrderID = table.Column<int>(type: "int", nullable: true),
                    strCustomerOrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    intCustomerOrderLineID = table.Column<int>(type: "int", nullable: true),
                    WarehouseID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WarrantyExpiredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CostTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AnonymousAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemSerial = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Batch = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    PackQty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfirmationReturnQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ExchangeRefID = table.Column<int>(type: "int", nullable: true),
                    ExchangeQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    strRejectReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SalesReturnLossAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    intOriginalDeliveryLineID = table.Column<int>(type: "int", nullable: true),
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
                    WarrantyInMonth = table.Column<int>(type: "int", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ItemBasePriceInclusive = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderDiscountProportion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HeaderCostProportion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsOfferItem = table.Column<bool>(type: "bit", nullable: false),
                    OfferItemID = table.Column<int>(type: "int", nullable: true),
                    OfferItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OfferItemQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FreeTypeID = table.Column<int>(type: "int", nullable: true),
                    FreeTypeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountID = table.Column<int>(type: "int", nullable: true),
                    SpecialDiscountAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SupplierDiscountContributionInPercent = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SupplierDiscountContributionInAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ProformaInvoiceID = table.Column<int>(type: "int", nullable: true),
                    PINo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    numBdPortQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    numForeignPortQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    strShelfLife = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParentRowID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveryLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_CustomerOrderLine_intCustomerOrderLineID",
                        column: x => x.intCustomerOrderLineID,
                        principalTable: "CustomerOrderLine",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_Item_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Item",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_LookupValues_FreeTypeID",
                        column: x => x.FreeTypeID,
                        principalTable: "LookupValues",
                        principalColumn: "LookupValueID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_SalesDelivery_SalesDeliveryID",
                        column: x => x.SalesDeliveryID,
                        principalTable: "SalesDelivery",
                        principalColumn: "SalesDeliveryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_UnitOfMeasure_UomID",
                        column: x => x.UomID,
                        principalTable: "UnitOfMeasure",
                        principalColumn: "UomID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLine_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubLedgerRow_CustomerID_SubLedgerHeaderID",
                table: "SubLedgerRow",
                columns: new[] { "CustomerID", "SubLedgerHeaderID" })
                .Annotation("SqlServer:Include", new[] { "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_AccountID",
                table: "CustomerOrder",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_BankAccID",
                table: "CustomerOrder",
                column: "BankAccID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_BusinessUnitID_CustomerOrderCode",
                table: "CustomerOrder",
                columns: new[] { "BusinessUnitID", "CustomerOrderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_BusinessUnitID_isActive_Status_OrderDate",
                table: "CustomerOrder",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_ChartOfAccIDExtraOtherCharge",
                table: "CustomerOrder",
                column: "ChartOfAccIDExtraOtherCharge");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_ChartOfAccIDOtherCharge",
                table: "CustomerOrder",
                column: "ChartOfAccIDOtherCharge");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_CurrencyID",
                table: "CustomerOrder",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_CustomerID",
                table: "CustomerOrder",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_FreeTypeID",
                table: "CustomerOrder",
                column: "FreeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_LcTypeID",
                table: "CustomerOrder",
                column: "LcTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_OfficeID",
                table: "CustomerOrder",
                column: "OfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_PaymentMethodID",
                table: "CustomerOrder",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_PaymentTermsID",
                table: "CustomerOrder",
                column: "PaymentTermsID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_PaymentTypeID",
                table: "CustomerOrder",
                column: "PaymentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_PriceTypeID",
                table: "CustomerOrder",
                column: "PriceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_ReferredByPartnerID",
                table: "CustomerOrder",
                column: "ReferredByPartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_SalesForceID",
                table: "CustomerOrder",
                column: "SalesForceID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_SalesQuotationID",
                table: "CustomerOrder",
                column: "SalesQuotationID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_SalesTermID",
                table: "CustomerOrder",
                column: "SalesTermID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_ShippingAddressID",
                table: "CustomerOrder",
                column: "ShippingAddressID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrder_WarehouseID",
                table: "CustomerOrder",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLine_CustomerOrderID",
                table: "CustomerOrderLine",
                column: "CustomerOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLine_FreeTypeID",
                table: "CustomerOrderLine",
                column: "FreeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLine_ItemID",
                table: "CustomerOrderLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLine_UomID",
                table: "CustomerOrderLine",
                column: "UomID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_AccountID",
                table: "SalesDelivery",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_BankAccID",
                table: "SalesDelivery",
                column: "BankAccID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_BusinessUnitID_isActive_Status_OrderDate",
                table: "SalesDelivery",
                columns: new[] { "BusinessUnitID", "isActive", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_BusinessUnitID_SalesDeliveryCode",
                table: "SalesDelivery",
                columns: new[] { "BusinessUnitID", "SalesDeliveryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_ChartOfAccIDExtraOtherCharge",
                table: "SalesDelivery",
                column: "ChartOfAccIDExtraOtherCharge");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_ChartOfAccIDOtherCharge",
                table: "SalesDelivery",
                column: "ChartOfAccIDOtherCharge");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_CourierOrEmployeeID",
                table: "SalesDelivery",
                column: "CourierOrEmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_CurrencyID",
                table: "SalesDelivery",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_CustomerID",
                table: "SalesDelivery",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_CustomerOrderID",
                table: "SalesDelivery",
                column: "CustomerOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_DeliveryMethodID",
                table: "SalesDelivery",
                column: "DeliveryMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_FreeTypeID",
                table: "SalesDelivery",
                column: "FreeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_InstallmentTypeID",
                table: "SalesDelivery",
                column: "InstallmentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_intCollectedByID",
                table: "SalesDelivery",
                column: "intCollectedByID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_OfficeID",
                table: "SalesDelivery",
                column: "OfficeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_PaymentMethodID",
                table: "SalesDelivery",
                column: "PaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_PaymentTypeID",
                table: "SalesDelivery",
                column: "PaymentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_PriceTypeID",
                table: "SalesDelivery",
                column: "PriceTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_ReferredByPartnerID",
                table: "SalesDelivery",
                column: "ReferredByPartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_SalesForceID",
                table: "SalesDelivery",
                column: "SalesForceID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_ShippingAddressID",
                table: "SalesDelivery",
                column: "ShippingAddressID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDelivery_WarehouseID",
                table: "SalesDelivery",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_FreeTypeID",
                table: "SalesDeliveryLine",
                column: "FreeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_intCustomerOrderLineID",
                table: "SalesDeliveryLine",
                column: "intCustomerOrderLineID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_ItemID",
                table: "SalesDeliveryLine",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_SalesDeliveryID",
                table: "SalesDeliveryLine",
                column: "SalesDeliveryID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_UomID",
                table: "SalesDeliveryLine",
                column: "UomID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLine_WarehouseID",
                table: "SalesDeliveryLine",
                column: "WarehouseID");

            migrationBuilder.AddForeignKey(
                name: "FK_SubLedgerRow_Customer_CustomerID",
                table: "SubLedgerRow",
                column: "CustomerID",
                principalTable: "Customer",
                principalColumn: "CustomerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubLedgerRow_Customer_CustomerID",
                table: "SubLedgerRow");

            migrationBuilder.DropTable(
                name: "SalesDeliveryLine");

            migrationBuilder.DropTable(
                name: "CustomerOrderLine");

            migrationBuilder.DropTable(
                name: "SalesDelivery");

            migrationBuilder.DropTable(
                name: "CustomerOrder");

            migrationBuilder.DropIndex(
                name: "IX_SubLedgerRow_CustomerID_SubLedgerHeaderID",
                table: "SubLedgerRow");

            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "SubLedgerRow");
        }
    }
}
