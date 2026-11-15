using Erp.Api.UserManagement.Employees;
using Erp.Api.UserManagement.Roles;
using Erp.Api.UserManagement.Users;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.UserManagement;

public static class UserManagementModule
{
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services)
    {
        services.AddSingleton<IErpMenuProvider, UserManagementMenu>();
        services.AddSingleton<IErpEndpoints, UserManagementEndpoints>();
        services.AddSingleton<IFeatureManifestProvider, EmployeeManifest>();
        services.AddSingleton<ILookupProvider, EmployeeLookups>();
        return services;
    }
}

public sealed class UserManagementEndpoints : IErpEndpoints
{
    public void Map(IEndpointRouteBuilder erp)
    {
        var group = erp.MapGroup("/user-management").WithTags("UserManagement");
        RoleEndpoints.Map(group);
        EmployeeEndpoints.Map(group);
        UserEndpoints.Map(group);
    }
}
