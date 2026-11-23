using Erp.Core.Modules;

namespace Erp.Api.Approvals;

public sealed class ApprovalMenu : IErpMenuProvider
{
    public const string Module = "approval";
    public const string MyApprovals = "approval.my-approvals";

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("Approval", "অনুমোদন"), "audit", 90,
        [
            new("approvals", new("Approvals", "অনুমোদনসমূহ"), "audit", 10,
            [
                new("my-approvals", new("My Approvals", "আমার অনুমোদন"), 10, [FeatureActions.View]),
            ]),
        ]),
    ];
}
