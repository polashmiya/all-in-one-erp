using System.Globalization;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Accounting;

internal static class PeriodLock
{
    public static Task<DateOnly?> ClosedThroughAsync(ErpDbContext db, CancellationToken cancellationToken) =>
        db.PeriodClosings.Where(p => p.IsActive).MaxAsync(p => (DateOnly?)p.PeriodEnd, cancellationToken);

    public static async Task<ProblemHttpResult?> ProblemAsync(ErpDbContext db, DateOnly date, CancellationToken cancellationToken) =>
        await IsClosedAsync(db, date, cancellationToken)
            ? TypedResults.Problem(title: "Period closed", detail: Message(date), statusCode: StatusCodes.Status409Conflict)
            : null;

    public static async Task CheckAsync(
        ErpDbContext db, string field, DateOnly date, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (await IsClosedAsync(db, date, cancellationToken)) errors[field] = [Message(date)];
    }

    private static async Task<bool> IsClosedAsync(ErpDbContext db, DateOnly date, CancellationToken cancellationToken) =>
        date <= await ClosedThroughAsync(db, cancellationToken);

    private static string Message(DateOnly date) =>
        $"{date.ToString("MMMM yyyy", CultureInfo.InvariantCulture)} is closed. Choose a date in an open month.";
}
