using System.Collections.Frozen;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Contracts;

public static class BusinessErrorResponse
{
    private static readonly FrozenDictionary<string, int> StatusCodesByError = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [ErrorCodes.CategoryInactive] = StatusCodes.Status409Conflict,
        [ErrorCodes.CategoryInUse] = StatusCodes.Status409Conflict,
        [ErrorCodes.CategoryNotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.DuplicateCategoryName] = StatusCodes.Status409Conflict,
        [ErrorCodes.DuplicateProductSku] = StatusCodes.Status409Conflict,
        [ErrorCodes.InsufficientStock] = StatusCodes.Status409Conflict,
        [ErrorCodes.InvalidCategoryDescription] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidCategoryId] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidCategoryName] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidDateRange] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidInventoryQuantity] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidInventoryReason] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidInventoryStock] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidInventoryType] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidPagination] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidProductDescription] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidProductId] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidProductName] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidProductPrice] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InvalidProductSku] = StatusCodes.Status400BadRequest,
        [ErrorCodes.InventoryStockOverflow] = StatusCodes.Status409Conflict,
        [ErrorCodes.ProductInactive] = StatusCodes.Status409Conflict,
        [ErrorCodes.ProductNotFound] = StatusCodes.Status404NotFound,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static ObjectResult From(Error error)
    {
        if (!StatusCodesByError.TryGetValue(error.Code, out var statusCode))
            throw new InvalidOperationException($"Business error code '{error.Code}' has no HTTP status mapping.");
        return new ObjectResult(new ApiError(error.Code, error.Message)) { StatusCode = statusCode };
    }
}
