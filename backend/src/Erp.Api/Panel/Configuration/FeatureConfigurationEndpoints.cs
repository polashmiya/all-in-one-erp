using Erp.Core.Validation;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Panel.Configuration;

public static class FeatureConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapFeatureConfigurationEndpoints(this IEndpointRouteBuilder panel)
    {
        var configuration = panel.MapGroup("/accounts/{id:guid}/configuration/{code}").WithTags("PanelFeatureConfiguration");

        configuration.MapGet("", Get)
            .WithName("GetFeatureConfiguration")
            .WithSummary("Fields of a screen with their defaults and the account's (or business unit's) changes")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapPost("/fields", Create)
            .WithName("CreateCustomField")
            .WithSummary("Add a custom field to a screen of the account; the key comes from the English label")
            .Validate<CreateCustomFieldRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapPut("/fields/{key}/status", SetStatus)
            .WithName("SetCustomFieldStatus")
            .WithSummary("Deactivate or activate a custom field; saved values are kept")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapPut("/fields/{key}", Save)
            .WithName("SaveFieldSetting")
            .WithSummary("Change one field for the account, or for one business unit with businessUnitId")
            .Validate<FieldSettingValues>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapDelete("/fields/{key}", Reset)
            .WithName("ResetFieldSetting")
            .WithSummary("Remove the account's (or business unit's) changes to one field")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapGet("/preview", Preview)
            .WithName("PreviewFeatureConfiguration")
            .WithSummary("The screen as the ERP gets it for the account, or for one business unit")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapPut("/layout", Arrange)
            .WithName("ArrangeFeatureLayout")
            .WithSummary("Save the order of tabs, sections and fields and where each one sits")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapDelete("/layout", ResetLayout)
            .WithName("ResetFeatureLayout")
            .WithSummary("Drop a business unit's own layout so it follows the account again, or put the account's layout back to the standard one")
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapPut("/sections/{key}", SaveSection)
            .WithName("SaveSectionSetting")
            .WithSummary("Rename, hide or resize a tab or section for the account, or for one business unit")
            .Validate<SectionSettingValues>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapDelete("/sections/{key}", ResetSection)
            .WithName("ResetSectionSetting")
            .WithSummary("Remove the account's (or business unit's) changes to one tab or section")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<Ok<FeatureConfig>, NotFound>> Preview(
        Guid id, string code, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        await service.PreviewAsync(id, businessUnitId, code, ct) is { } config ? TypedResults.Ok(config) : TypedResults.NotFound();

    private static Task<Results<Ok<FeatureConfigEditor>, NotFound, ValidationProblem>> Arrange(
        Guid id, string code, Guid? businessUnitId, LayoutRequest layout, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.ArrangeAsync(id, businessUnitId, code, layout, ct));

    private static Task<Results<Ok<FeatureConfigEditor>, NotFound, ValidationProblem>> ResetLayout(
        Guid id, string code, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.ResetLayoutAsync(id, businessUnitId, code, ct));

    private static Task<Results<Ok<SectionConfigRow>, NotFound, ValidationProblem>> SaveSection(
        Guid id, string code, string key, Guid? businessUnitId, SectionSettingValues values,
        FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveSectionAsync(id, businessUnitId, code, key, values, ct));

    private static Task<Results<Ok<SectionConfigRow>, NotFound, ValidationProblem>> ResetSection(
        Guid id, string code, string key, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveSectionAsync(id, businessUnitId, code, key, new SectionSettingValues(), ct));

    private static async Task<Results<Ok<FeatureConfigEditor>, NotFound>> Get(
        Guid id, string code, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        await service.GetAsync(id, businessUnitId, code, ct) is { } editor ? TypedResults.Ok(editor) : TypedResults.NotFound();

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Create(
        Guid id, string code, CreateCustomFieldRequest request, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.CreateAsync(id, code, request, ct));

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> SetStatus(
        Guid id, string code, string key, CustomFieldStatusRequest request, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SetStatusAsync(id, code, key, request.IsActive, ct));

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Save(
        Guid id, string code, string key, Guid? businessUnitId, FieldSettingValues values,
        FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveAsync(id, businessUnitId, code, key, values, ct));

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Reset(
        Guid id, string code, string key, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveAsync(id, businessUnitId, code, key, new FieldSettingValues(), ct));

    private static async Task<Results<Ok<T>, NotFound, ValidationProblem>> Respond<T>(
        Task<(T? Row, Dictionary<string, string[]> Errors)> saving) where T : class
    {
        var (row, errors) = await saving;
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        return row is null ? TypedResults.NotFound() : TypedResults.Ok(row);
    }
}
