using System.Reflection;
using InventoryManagement.Api.Contracts;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Api.Tests;

public sealed class BusinessErrorResponseTests
{
    [Fact]
    public void EveryDeclaredBusinessErrorHasAnExplicitHttpStatus()
    {
        var codes = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.NotEmpty(codes);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        foreach (var code in codes)
        {
            var response = BusinessErrorResponse.From(new Error(code, "Business rule rejected."));
            Assert.Contains(response.StatusCode!.Value, new[] { 400, 404, 409 });
            var body = Assert.IsType<ApiError>(response.Value);
            Assert.Equal(code, body.Code);
            Assert.Equal("Business rule rejected.", body.Message);
        }
    }

    [Theory]
    [InlineData("PRODUCT_NOT_FOUND", 404)]
    [InlineData("CATEGORY_NOT_FOUND", 404)]
    [InlineData("DUPLICATE_PRODUCT_SKU", 409)]
    [InlineData("INSUFFICIENT_STOCK", 409)]
    [InlineData("CATEGORY_IN_USE", 409)]
    [InlineData("INVALID_PRODUCT_PRICE", 400)]
    [InlineData("INVALID_PAGINATION", 400)]
    public void PublicErrorCodesKeepTheirHttpContract(string code, int expected) =>
        Assert.Equal(expected, BusinessErrorResponse.From(new Error(code, "Rejected.")).StatusCode);

    [Theory]
    [InlineData("NEW_UNMAPPED_CODE")]
    [InlineData("product_not_found")]
    public void UnknownCodesAreProgrammingErrorsInsteadOfBadRequests(string code) =>
        Assert.Throws<InvalidOperationException>(() => BusinessErrorResponse.From(new Error(code, "Rejected.")));
}
