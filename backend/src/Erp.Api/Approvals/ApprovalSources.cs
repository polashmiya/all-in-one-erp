using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;

namespace Erp.Api.Approvals;

public sealed record ApprovalDocumentInfo(
    int Id, string Code, DateOnly Date, string? Party, decimal Amount, ApprovalStatus Status, string? RequestedBy, DateTime? CreatedAt);

public interface IApprovalSource
{
    string Feature { get; }

    IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids);
}
