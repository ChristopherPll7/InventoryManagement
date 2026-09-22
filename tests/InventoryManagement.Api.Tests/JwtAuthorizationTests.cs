using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryManagement.Api.Contracts;

namespace InventoryManagement.Api.Tests;

public sealed class JwtAuthorizationTests(JwtApiFactory factory) : IClassFixture<JwtApiFactory>
{
    public static IEnumerable<object[]> Endpoints()
    {
        const string id = "22222222-2222-2222-2222-222222222222";
        foreach (var resource in new[] { "products", "categories" })
        {
            yield return ["GET", $"/api/{resource}", $"{resource}.read", 200];
            yield return ["GET", $"/api/{resource}/{id}", $"{resource}.read", 404];
            yield return ["POST", $"/api/{resource}", $"{resource}.write", 400];
            yield return ["PUT", $"/api/{resource}/{id}", $"{resource}.write", 400];
            yield return ["DELETE", $"/api/{resource}/{id}", $"{resource}.write", 404];
        }
        yield return ["POST", "/api/inventory/movements", "inventory.write", 400];
        yield return ["GET", $"/api/inventory/products/{id}", "inventory.read", 404];
        yield return ["GET", $"/api/inventory/products/{id}/movements", "inventory.read", 404];
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Every_endpoint_requires_its_permission(string method, string path, string permission, int authorizedStatus)
    {
        await AssertResponseAsync(method, path, null, 401);
        await AssertResponseAsync(method, path, factory.CreateToken(null), 403);
        await AssertResponseAsync(method, path, factory.CreateToken("unrelated.permission"), 403);
        await AssertResponseAsync(method, path, factory.CreateToken(permission), authorizedStatus);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("expiration")]
    [InlineData("malformed")]
    public Task Invalid_tokens_are_challenged(string defect) =>
        AssertResponseAsync("GET", "/api/products", defect == "malformed" ? "invalid-token" : factory.CreateToken("products.read", defect), 401);

    private async Task AssertResponseAsync(string method, string path, string? token, int expected)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)expected, response.StatusCode);
        if (expected is 401 or 403)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.NotNull(error);
            Assert.False(string.IsNullOrWhiteSpace(error.Code));
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        }
        if (expected == 401)
            Assert.Contains(response.Headers.WwwAuthenticate, challenge => challenge.Scheme == "Bearer");
    }
}
