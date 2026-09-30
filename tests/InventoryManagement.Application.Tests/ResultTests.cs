using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void SuccessExposesValueAndRejectsErrorAccess()
    {
        var result = Result<int>.Success(0);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(0, result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void FailureExposesErrorAndRejectsValueAccess()
    {
        var error = new Error("PRODUCT_NOT_FOUND", "The product was not found.");
        var result = Result<Guid>.Failure(error);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void MissingValueOrErrorCannotCreateAResult()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Theory]
    [InlineData("", "Message")]
    [InlineData("CODE", " ")]
    public void ErrorRequiresCodeAndMessage(string code, string message) =>
        Assert.Throws<ArgumentException>(() => new Error(code, message));
}
