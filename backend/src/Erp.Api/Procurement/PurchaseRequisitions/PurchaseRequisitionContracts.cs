using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Procurement.PurchaseRequisitions;

public sealed record RequisitionRow(
    int Id,
    string Code,
    DateOnly RequestDate,
    DateOnly? DueDate,
    Guid WarehouseId,
    string? WarehouseName,
    int? RequestTypeId,
    string? RequestTypeName,
    int? DepartmentId,
    string? DepartmentName,
    int? EmployeeId,
    string? EmployeeName,
    string Status,
    int ItemCount,
    decimal TotalQuantity,
    decimal TotalAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record RequisitionList(IReadOnlyList<RequisitionRow> Items, int Total, ApprovalCounts Counts);

public sealed record SaveRequisitionLine
{
    public required int ItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Rate { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveRequisitionRequest
{
    public required string? Code { get; init; }
    public required DateOnly RequestDate { get; init; }
    public required DateOnly? DueDate { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? RequestTypeId { get; init; }
    public required int? PurchaseMethodId { get; init; }
    public required int? ItemTypeId { get; init; }
    public required int? DepartmentId { get; init; }
    public required int? SectionId { get; init; }
    public required int? EmployeeId { get; init; }
    public required int? BuyerId { get; init; }
    public required string? ProjectName { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SaveRequisitionLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record RequisitionLine(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    int UomId,
    string UomName,
    decimal Quantity,
    decimal Rate,
    decimal Amount,
    string? Remarks);

public sealed record RequisitionDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly RequestDate { get; init; }
    public required DateOnly? DueDate { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? RequestTypeId { get; init; }
    public required string? RequestTypeName { get; init; }
    public required int? PurchaseMethodId { get; init; }
    public required string? PurchaseMethodName { get; init; }
    public required int? ItemTypeId { get; init; }
    public required string? ItemTypeName { get; init; }
    public required int? DepartmentId { get; init; }
    public required string? DepartmentName { get; init; }
    public required int? SectionId { get; init; }
    public required string? SectionName { get; init; }
    public required int? EmployeeId { get; init; }
    public required string? EmployeeName { get; init; }
    public required int? BuyerId { get; init; }
    public required string? BuyerName { get; init; }
    public required string? ProjectName { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<RequisitionLine> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveRequisitionRequestValidator : AbstractValidator<SaveRequisitionRequest>
{
    public SaveRequisitionRequestValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.RequestDate).When(x => x.DueDate is not null)
            .WithMessage("Use a date on or after the request date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(ProcurementData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.Rate).GreaterThanOrEqualTo(0).When(l => l.Rate is not null);
            line.RuleFor(l => l.Remarks).MaximumLength(300);
        });
    }
}

