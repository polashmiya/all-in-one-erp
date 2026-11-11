using Erp.Core.Validation;
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

        configuration.MapPut("/fields/{key}", Save)
            .WithName("SaveFieldSetting")
            .WithSummary("Change one field for the account, or for one business unit with businessUnitId")
            .Validate<FieldSettingValues>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        configuration.MapDelete("/fields/{key}", Reset)
            .WithName("ResetFieldSetting")
            .WithSummary("Remove the account's (or business unit's) changes to one field")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<Ok<FeatureConfigEditor>, NotFound>> Get(
        Guid id, string code, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        await service.GetAsync(id, businessUnitId, code, ct) is { } editor ? TypedResults.Ok(editor) : TypedResults.NotFound();

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Save(
        Guid id, string code, string key, Guid? businessUnitId, FieldSettingValues values,
        FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveAsync(id, businessUnitId, code, key, values, ct));

    private static Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Reset(
        Guid id, string code, string key, Guid? businessUnitId, FeatureConfigurationService service, CancellationToken ct) =>
        Respond(service.SaveAsync(id, businessUnitId, code, key, new FieldSettingValues(), ct));

    private static async Task<Results<Ok<FieldConfigRow>, NotFound, ValidationProblem>> Respond(
        Task<(FieldConfigRow? Row, Dictionary<string, string[]> Errors)> saving)
    {
        var (row, errors) = await saving;
        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);
        return row is null ? TypedResults.NotFound() : TypedResults.Ok(row);
    }
}
