using InventoryManagement.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Api.Tests;

public class ContractApiFactory : WebApplicationFactory<Program>
{
    public ContractPersistenceStub Persistence { get; } = new();
    public CategoryPersistenceStub Categories { get; } = new();
    public InventoryQueriesStub Inventory { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("ConnectionStrings:InventoryDatabase", "Server=unused;Database=unused");
        builder.UseSetting("Authentication:Authority", "https://issuer.example.test");
        builder.UseSetting("Authentication:Audience", "inventory-api");
        builder.UseSetting("Authentication:SwaggerClientId", "inventory-swagger");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProductCommandStore>(Persistence);
            services.AddSingleton<IProductValidationQueries>(Persistence);
            services.AddSingleton<IProductQueries>(Persistence);
            services.AddSingleton<IInventoryCommandStore>(Persistence);
            services.AddSingleton<IInventoryQueries>(Inventory);
            services.AddSingleton<ICategoryCommandStore>(Categories);
            services.AddSingleton<ICategoryQueries>(Categories);
            services.AddControllers().AddApplicationPart(typeof(ContractProbeController).Assembly);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ContractTests";
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("ContractTests", _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "true");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', InventoryManagement.Api.Authorization.Permissions.All));
        return client;
    }
}
