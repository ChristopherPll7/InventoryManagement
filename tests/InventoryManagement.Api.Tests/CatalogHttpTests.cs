using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryManagement.Application.Abstractions;

namespace InventoryManagement.Api.Tests;

public sealed class CatalogHttpTests
{
    [Fact]
    public async Task CategoryLifecycleHasExpectedContracts()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/categories", new { name = " Tools ", description = "Original" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/categories/{id}", created.Headers.Location!.ToString());
        var initial = await client.GetFromJsonAsync<CategoryDto>($"/api/categories/{id}");
        Assert.Equal("Tools", initial!.Name);
        Assert.True(initial.IsActive);
        var updated = await client.PutAsJsonAsync($"/api/categories/{id}", new { name = "Updated", description = "Details" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Updated", (await updated.Content.ReadFromJsonAsync<CategoryDto>())!.Name);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categories/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categories/{id}")).StatusCode);
        Assert.False((await client.GetFromJsonAsync<CategoryDto>($"/api/categories/{id}"))!.IsActive);
        Assert.Contains((await client.GetFromJsonAsync<CategoryDto[]>("/api/categories"))!, item => item.Id == id && !item.IsActive);
    }

    [Fact]
    public async Task ProductUpdateAndDeleteKeepStockAndIdentity()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/products", ProductBody());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var updated = await client.PutAsJsonAsync($"/api/products/{id}", ProductBody("Updated", "NEW-SKU"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var product = (await updated.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(id, product.Id);
        Assert.Equal("NEW-SKU", product.Sku);
        Assert.Equal(10, product.CurrentStock);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{id}")).StatusCode);
        var inactive = (await client.GetFromJsonAsync<ProductDto>($"/api/products/{id}"))!;
        Assert.False(inactive.IsActive);
        Assert.Equal(10, inactive.CurrentStock);
    }

    [Theory]
    [InlineData("POST", "/api/categories")]
    [InlineData("GET", "/api/categories")]
    [InlineData("GET", "/api/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("PUT", "/api/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/categories/11111111-1111-1111-1111-111111111111")]
    [InlineData("PUT", "/api/products/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/products/11111111-1111-1111-1111-111111111111")]
    public async Task NewEndpointsRequireAuthentication(string method, string path)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateClient();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { name = "Tools" }) });
        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Theory]
    [InlineData("GET", "categories", "CATEGORY_NOT_FOUND")]
    [InlineData("PUT", "categories", "CATEGORY_NOT_FOUND")]
    [InlineData("DELETE", "categories", "CATEGORY_NOT_FOUND")]
    [InlineData("PUT", "products", "PRODUCT_NOT_FOUND")]
    [InlineData("DELETE", "products", "PRODUCT_NOT_FOUND")]
    public async Task MissingResourcesReturnNotFound(string method, string resource, string code)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), $"/api/{resource}/{Guid.NewGuid()}")
        { Content = resource == "products" ? JsonContent.Create(ProductBody()) : JsonContent.Create(new { name = "Tools" }) });
        await AssertErrorAsync(response, HttpStatusCode.NotFound, code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CategoryInvalidNameReturnsBadRequest(string name)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        await AssertErrorAsync(await client.PostAsJsonAsync("/api/categories", new { name }), HttpStatusCode.BadRequest, "INVALID_CATEGORY_NAME");
        Assert.Equal(0, factory.Categories.WriteCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateCategoryNameReturnsConflictAtPrecheckOrWrite(bool writeRace)
    {
        await using var factory = new ContractApiFactory();
        factory.Categories.DuplicateName = !writeRace;
        factory.Categories.WriteError = writeRace ? "DUPLICATE_CATEGORY_NAME" : null;
        using var client = factory.CreateAuthenticatedClient();
        await AssertErrorAsync(await client.PostAsJsonAsync("/api/categories", new { name = "Tools" }), HttpStatusCode.Conflict, "DUPLICATE_CATEGORY_NAME");
    }

    [Fact]
    public async Task CategoryWithActiveProductsCannotBeDeleted()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/categories", new { name = "Tools" });
        factory.Categories.HasActiveProducts = true;
        await AssertErrorAsync(await client.DeleteAsync($"/api/categories/{factory.Categories.Category!.Id}"), HttpStatusCode.Conflict, "CATEGORY_IN_USE");
    }

    [Theory]
    [InlineData(null, HttpStatusCode.NotFound, "CATEGORY_NOT_FOUND")]
    [InlineData(false, HttpStatusCode.Conflict, "CATEGORY_INACTIVE")]
    public async Task ProductUpdateRejectsUnavailableCategory(bool? active, HttpStatusCode status, string code)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products", ProductBody());
        factory.Persistence.CategoryIsActive = active;
        await AssertErrorAsync(await client.PutAsJsonAsync($"/api/products/{factory.Persistence.Product!.Id}", ProductBody()), status, code);
    }

    [Fact]
    public async Task ProductUpdateDuplicateSkuReturnsConflict()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/products", ProductBody());
        factory.Persistence.DuplicateSku = true;
        await AssertErrorAsync(await client.PutAsJsonAsync($"/api/products/{factory.Persistence.Product!.Id}", ProductBody()), HttpStatusCode.Conflict, "DUPLICATE_PRODUCT_SKU");
    }

    [Fact]
    public async Task SwaggerIncludesAllNewOperations()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateClient();
        var schema = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = schema.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/categories").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/categories").TryGetProperty("get", out _));
        foreach (var operation in new[] { "get", "put", "delete" })
            Assert.True(paths.GetProperty("/api/categories/{id}").TryGetProperty(operation, out _));
        Assert.True(paths.GetProperty("/api/products/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/products/{id}").TryGetProperty("delete", out _));
    }

    private static object ProductBody(string name = "Product", string sku = "SKU") =>
        new { name, sku, price = 1, categoryId = Guid.NewGuid() };

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(2, body.EnumerateObject().Count());
    }
}
