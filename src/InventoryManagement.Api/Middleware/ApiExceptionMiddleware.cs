using InventoryManagement.Domain.Common;
using InventoryManagement.Api.Contracts;

namespace InventoryManagement.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = MapStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(new ApiError(exception.Code, exception.Message));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unhandled error occurred.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiError("INTERNAL_ERROR", "An unexpected error occurred."));
        }
    }

    private static int MapStatusCode(string code) => code switch
    {
        "PRODUCT_NOT_FOUND" or "CATEGORY_NOT_FOUND" => StatusCodes.Status404NotFound,
        "DUPLICATE_PRODUCT_SKU" or "DUPLICATE_CATEGORY_NAME" or "INSUFFICIENT_STOCK" or "PRODUCT_INACTIVE" or "CATEGORY_IN_USE" or "CATEGORY_INACTIVE" or "INVENTORY_STOCK_OVERFLOW" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
