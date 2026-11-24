using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Approvals;

namespace Erp.Api.Accounting;

internal static class JournalQueries
{
    public static IQueryable<Journal> Posted(this IQueryable<Journal> journals) =>
        journals.Where(j => j.IsActive && (j.Status == ApprovalStatus.Approved || j.Status == ApprovalStatus.Cancelled));
}
