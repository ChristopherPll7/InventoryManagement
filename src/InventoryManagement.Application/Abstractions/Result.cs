namespace InventoryManagement.Application.Abstractions;

public sealed class Result<T>
{
    private readonly T? value;
    private readonly Error? error;

    private Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");
    public Error Error => IsFailure ? error! : throw new InvalidOperationException("A successful result has no error.");
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
