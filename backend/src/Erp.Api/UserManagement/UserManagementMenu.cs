using Erp.Core.Modules;

namespace Erp.Api.UserManagement;

public sealed class UserManagementMenu : IErpMenuProvider
{
    public const string Module = "system-setup";

    public const string User = "system-setup.user";
    public const string Employee = "system-setup.employee";
    public const string UserRole = "system-setup.user-role";

    private static readonly string[] UserActions = [FeatureActions.View, FeatureActions.Create, FeatureActions.Edit, FeatureActions.Export];
    private static readonly string[] RoleActions =
        [FeatureActions.View, FeatureActions.Create, FeatureActions.Edit, FeatureActions.Delete, FeatureActions.Export];

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("System Setup", "সিস্টেম সেটআপ"), "setting", 10,
        [
            new("user-management", new("User Management", "ইউজার ব্যবস্থাপনা"), "team", 20,
            [
                new("user", new("User", "ইউজার"), 10, UserActions),
                new("employee", new("Employee", "কর্মী"), 20, UserActions),
                new("user-role", new("User Role", "ইউজার রোল"), 30, RoleActions),
            ]),
        ]),
    ];
}
