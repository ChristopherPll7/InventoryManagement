using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Inventory;

namespace InventoryManagement.Api.Tests;

public sealed class InventoryReadHttpTests
{
    [Fact]
    public async Task BalanceReturnsTheSpecifiedFields()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync(BalancePath(factory));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(factory.Inventory.Inventory, await response.Content.ReadFromJsonAsync<ProductInventoryDto>());
    }

    [Fact]
    public async Task EmptyHistoryReturnsDefaultPagination()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAuthenticatedClient();
        var page = await client.GetFromJsonAsync<PagedResult<InventoryMovementDto>>(HistoryPath(factory));
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task FiltersAreBoundAndMovementTypeIsSerializedAsString()
    {
        await using var factory = CreateFactory();
        var id = factory.Inventory.Inventory!.ProductId;
        factory.Inventory.Items = [new InventoryMovementDto(Guid.NewGuid(), id, InventoryMovementType.Entry, 1, "Reason", DateTimeOffset.UtcNow)];
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync(HistoryPath(factory) + "?type=Entry&startDate=2026-01-01T00:00:00Z&endDate=2026-02-01T00:00:00Z&page=2&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var filter = factory.Inventory.LastFilter!;
        Assert.Equal(InventoryMovementType.Entry, filter.Type);
        Assert.Equal(2, filter.Page);
        Assert.Equal(5, filter.PageSize);
        Assert.Equal(1, filter.StartDate!.Value.Month);
        Assert.Equal(2, filter.EndDate!.Value.Month);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Entry", body.GetProperty("items")[0].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("page=0", "INVALID_PAGINATION")]
    [InlineData("page=-1", "INVALID_PAGINATION")]
    [InlineData("page=10001", "INVALID_PAGINATION")]
    [InlineData("pageSize=0", "INVALID_PAGINATION")]
    [InlineData("pageSize=101", "INVALID_PAGINATION")]
    [InlineData("page=invalid", "INVALID_REQUEST")]
    [InlineData("page=999999999999", "INVALID_REQUEST")]
    [InlineData("type=Unknown", "INVALID_REQUEST")]
    [InlineData("type=1", "INVALID_REQUEST")]
    [InlineData("startDate=invalid", "INVALID_REQUEST")]
    [InlineData("startDate=2026-02-01T00:00:00Z&endDate=2026-01-01T00:00:00Z", "INVALID_DATE_RANGE")]
    public async Task InvalidFiltersReturnConsistentBadRequest(string query, string code)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAuthenticatedClient();
        await AssertErrorAsync(await client.GetAsync(HistoryPath(factory) + "?" + query), HttpStatusCode.BadRequest, code);
        Assert.Null(factory.Inventory.LastFilter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/movements")]
    public async Task MissingProductReturnsNotFound(string suffix)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateAuthenticatedClient();
        await AssertErrorAsync(await client.GetAsync($"/api/inventory/products/{Guid.NewGuid()}{suffix}"), HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("/movements")]
    public async Task InventoryQueriesRequireAuthentication(string suffix)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AssertErrorAsync(await client.GetAsync(BalancePath(factory) + suffix), HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Fact]
    public async Task SwaggerDescribesBothQueriesAndFilters()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var schema = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = schema.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/inventory/products/{productId}").TryGetProperty("get", out _));
        var history = paths.GetProperty("/api/inventory/products/{productId}/movements").GetProperty("get");
        var names = history.GetProperty("parameters").EnumerateArray().Select(parameter => parameter.GetProperty("name").GetString()).ToArray();
        foreach (var name in new[] { "type", "startDate", "endDate", "page", "pageSize" })
            Assert.Contains(names, actual => string.Equals(actual, name, StringComparison.OrdinalIgnoreCase));
    }

    private static ContractApiFactory CreateFactory()
    {
        var factory = new ContractApiFactory();
        factory.Inventory.Inventory = new ProductInventoryDto(Guid.NewGuid(), "SKU", "Product", 0);
        return factory;
    }

    private static string BalancePath(ContractApiFactory factory) => $"/api/inventory/products/{factory.Inventory.Inventory!.ProductId}";
    private static string HistoryPath(ContractApiFactory factory) => BalancePath(factory) + "/movements";

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
    }
}
