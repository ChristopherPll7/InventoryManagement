using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryManagement.Api.Tests;

public sealed class CatalogPaginationTests
{
    [Theory]
    [InlineData("products")]
    [InlineData("categories")]
    public async Task DefaultListsReturnPageEnvelopes(string resource)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/{resource}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, page.GetProperty("totalCount").GetInt64());
        Assert.Equal(0, page.GetProperty("totalPages").GetInt64());
        Assert.Empty(page.GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData("products", "page=0")]
    [InlineData("categories", "page=10001")]
    [InlineData("products", "pageSize=0")]
    [InlineData("categories", "pageSize=101")]
    [InlineData("products", "pageSize=101")]
    [InlineData("categories", "pageSize=0")]
    [InlineData("products", "page=10001")]
    [InlineData("categories", "page=0")]
    public async Task OutOfRangeParametersReturnBusinessValidation(string resource, string query)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/{resource}?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_PAGINATION", body.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("products")]
    [InlineData("categories")]
    public async Task MaximumPageAndSizeAreAccepted(string resource)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/{resource}?page=10000&pageSize=100");
        Assert.Equal(10000, page.GetProperty("page").GetInt32());
        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
        Assert.Empty(page.GetProperty("items").EnumerateArray());
    }
}
