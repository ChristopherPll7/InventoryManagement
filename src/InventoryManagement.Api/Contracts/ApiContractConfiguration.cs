using System.Text.Json.Serialization;
using InventoryManagement.Domain.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Contracts;

public static class ApiContractConfiguration
{
    public static IServiceCollection AddApiContracts(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter<InventoryMovementType>(allowIntegerValues: false)))
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
                    new ApiError("INVALID_REQUEST", "The request contains missing or invalid values.")));
        return services;
    }

    public static IApplicationBuilder UseApiStatusCodePages(this IApplicationBuilder app) =>
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            var error = response.StatusCode switch
            {
                400 => new ApiError("INVALID_REQUEST", "The request contains missing or invalid values."),
                401 => new ApiError("UNAUTHORIZED", "Authentication is required or the access token is invalid."),
                403 => new ApiError("FORBIDDEN", "You do not have permission to perform this operation."),
                404 => new ApiError("NOT_FOUND", "The requested resource was not found."),
                405 => new ApiError("METHOD_NOT_ALLOWED", "The HTTP method is not allowed for this resource."),
                415 => new ApiError("UNSUPPORTED_MEDIA_TYPE", "The request media type is not supported."),
                _ => new ApiError("HTTP_ERROR", "The request could not be processed.")
            };
            await response.WriteAsJsonAsync(error);
        });
}
