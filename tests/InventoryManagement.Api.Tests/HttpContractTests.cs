using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace InventoryManagement.Api.Tests;

public sealed class HttpContractTests
{
    [Theory]
    [InlineData("Entry", 15)]
    [InlineData("Exit", 5)]
    public async Task MovementAcceptsSpecifiedStringTypes(string type, int expectedStock)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/inventory/movements", new { productId = Guid.NewGuid(), type, quantity = 5 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStock, body.GetProperty("currentStock").GetInt32());
        Assert.Equal(1, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("\"Unknown\"")]
    [InlineData("null")]
    public async Task InvalidMovementTypeHasConsistentValidationError(string type)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var json = $$"""{"productId":"{{Guid.NewGuid()}}","type":{{type}},"quantity":1}""";
        var response = await client.PostAsync("/api/inventory/movements", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("{}", "INVALID_REQUEST")]
    [InlineData("{", "INVALID_REQUEST")]
    [InlineData("{\"productId\":\"not-a-guid\"}", "INVALID_REQUEST")]
    public async Task InvalidMovementBodyDoesNotWrite(string json, string code)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsync("/api/inventory/movements", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("PRODUCT_NOT_FOUND", HttpStatusCode.NotFound)]
    [InlineData("PRODUCT_INACTIVE", HttpStatusCode.Conflict)]
    [InlineData("INSUFFICIENT_STOCK", HttpStatusCode.Conflict)]
    [InlineData("INVENTORY_STOCK_OVERFLOW", HttpStatusCode.Conflict)]
    public async Task InventoryFailureHasSpecifiedStatus(string code, HttpStatusCode status)
    {
        await using var factory = new ContractApiFactory();
        factory.Persistence.InventoryError = code;
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/inventory/movements", new { productId = Guid.NewGuid(), type = "Entry", quantity = 1 });
        await AssertErrorAsync(response, status, code);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.NotFound, "CATEGORY_NOT_FOUND")]
    [InlineData(false, HttpStatusCode.Conflict, "CATEGORY_INACTIVE")]
    public async Task MissingAndInactiveCategoriesAreDistinguished(bool? active, HttpStatusCode status, string code)
    {
        await using var factory = new ContractApiFactory();
        factory.Persistence.CategoryIsActive = active;
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/products", ValidProduct());
        await AssertErrorAsync(response, status, code);
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateSkuIsConflictIncludingWriteTimeRace(bool failsAtWrite)
    {
        await using var factory = new ContractApiFactory();
        factory.Persistence.DuplicateSku = !failsAtWrite;
        factory.Persistence.ProductWriteError = failsAtWrite ? "DUPLICATE_PRODUCT_SKU" : null;
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/products", ValidProduct());
        await AssertErrorAsync(response, HttpStatusCode.Conflict, "DUPLICATE_PRODUCT_SKU");
    }

    [Fact]
    public async Task MissingProductFieldsUseErrorEnvelope()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/products", new { });
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
    }

    [Fact]
    public async Task UnauthenticatedRequestPreservesBearerChallenge()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        Assert.Contains(response.Headers.WwwAuthenticate, challenge => challenge.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("/contract-probe/forbidden", HttpStatusCode.Forbidden, "FORBIDDEN")]
    [InlineData("/contract-probe/failure", HttpStatusCode.InternalServerError, "INTERNAL_ERROR")]
    [InlineData("/does-not-exist", HttpStatusCode.NotFound, "NOT_FOUND")]
    [InlineData("/api/products/11111111-1111-1111-1111-111111111111", HttpStatusCode.NotFound, "PRODUCT_NOT_FOUND")]
    public async Task ErrorsUseSameEnvelope(string path, HttpStatusCode status, string code)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.GetAsync(path);
        await AssertErrorAsync(response, status, code);
        Assert.DoesNotContain("Internal details", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SwaggerDocumentsStringMovementTypes()
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateClient();
        var schema = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var type = schema.GetProperty("components").GetProperty("schemas").GetProperty("InventoryMovementType");
        Assert.Equal("string", type.GetProperty("type").GetString());
        Assert.Equal(new[] { "Entry", "Exit" }, type.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
    }

    private static object ValidProduct() => new { name = "Product", sku = "SKU", price = 1, categoryId = Guid.NewGuid() };

    [Theory]
    [InlineData("name")]
    [InlineData("sku")]
    [InlineData("price")]
    [InlineData("categoryId")]
    public async Task RequiredProductPropertiesCannotBeOmitted(string property)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var body = new Dictionary<string, object> { ["name"] = "Product", ["sku"] = "SKU", ["price"] = 0, ["categoryId"] = Guid.NewGuid() };
        body.Remove(property);
        var response = await client.PostAsJsonAsync("/api/products", body);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("name", 151, "INVALID_PRODUCT_NAME")]
    [InlineData("description", 1001, "INVALID_PRODUCT_DESCRIPTION")]
    [InlineData("sku", 81, "INVALID_PRODUCT_SKU")]
    public async Task OversizedProductValuesAreClientErrors(string property, int length, string code)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var body = new Dictionary<string, object> { ["name"] = "Product", ["sku"] = "SKU", ["price"] = 0, ["categoryId"] = Guid.NewGuid() };
        body[property] = new string('x', length);
        var response = await client.PostAsJsonAsync("/api/products", body);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000")]
    public async Task InvalidPricesAreClientErrors(string value)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var price = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var response = await client.PostAsJsonAsync("/api/products", new { name = "Product", sku = "SKU", price, categoryId = Guid.NewGuid() });
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_PRODUCT_PRICE");
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveMovementQuantityIsClientError(int quantity)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/inventory/movements", new { productId = Guid.NewGuid(), type = "Entry", quantity });
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_INVENTORY_QUANTITY");
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    [Theory]
    [InlineData("type")]
    [InlineData("quantity")]
    [InlineData("productId")]
    public async Task RequiredMovementPropertiesCannotBeOmitted(string property)
    {
        await using var factory = new ContractApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        var body = new Dictionary<string, object> { ["productId"] = Guid.NewGuid(), ["type"] = "Entry", ["quantity"] = 1 };
        body.Remove(property);
        var response = await client.PostAsJsonAsync("/api/inventory/movements", body);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
        Assert.Equal(0, factory.Persistence.WriteCount);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        Assert.Equal(2, body.EnumerateObject().Count());
    }
}
