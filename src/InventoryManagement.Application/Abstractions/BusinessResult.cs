using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Abstractions;

internal static class BusinessResult
{
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation)
    {
        try
        {
            return await operation();
        }
        catch (DomainException exception)
        {
            return Result<T>.Failure(new Error(exception.Code, exception.Message));
        }
    }
}
