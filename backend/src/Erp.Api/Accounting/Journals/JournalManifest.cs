using Erp.Api.Approvals;
using Erp.Core.Domain.Approvals;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;

namespace Erp.Api.Accounting.Journals;

public sealed class JournalManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(AccountingMenu.Journal,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Journal", "জার্নাল"), 10, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Journal no.", "জার্নাল নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("journalDate", FieldTypes.Date, new("Date", "তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("narration", FieldTypes.TextArea, new("Narration", "বিবরণ"), "basic", 30)
            {
                Locked = true,
                MaxLength = 1500,
                ListOrder = 30,
            },
        ]),
    ];
}

internal sealed class JournalApprovalSource : IApprovalSource
{
    public string Feature => AccountingMenu.Journal;

    public IQueryable<ApprovalDocumentInfo> Documents(ErpDbContext db, ApprovalStatus? status, IReadOnlyCollection<int>? ids) =>
        db.Journals
            .Where(j => j.IsActive && j.IsManual && (status == null || j.Status == status) && (ids == null || ids.Contains(j.Id)))
            .Select(j => new ApprovalDocumentInfo(
                j.Id,
                j.Code,
                j.JournalDate,
                null,
                j.TotalAmount,
                j.Status,
                db.ErpUsers.Where(u => u.Id == j.CreatedById).Select(u => u.FullName).FirstOrDefault(),
                j.CreatedAt));
}
