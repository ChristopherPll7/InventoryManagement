using InventoryManagement.Api;
using InventoryManagement.Api.Contracts;
using InventoryManagement.Api.Middleware;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Inventory;
using InventoryManagement.Application.Products;
using InventoryManagement.Application.Categories;
using InventoryManagement.Infrastructure;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Api.Authorization;
using Microsoft.OpenApi.Models;

var migrationOnly = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--migrate").ToArray());
var connectionString = builder.Configuration.GetConnectionString("InventoryDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:InventoryDatabase is required.");

if (migrationOnly)
{
    await new DatabaseInitializer(new SqlConnectionFactory(connectionString)).InitializeAsync(CancellationToken.None);
    return;
}
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ICommandHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateProductCommand, ProductDto>, UpdateProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteProductCommand, CommandCompleted>, DeleteProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateCategoryCommand, Guid>, CreateCategoryCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateCategoryCommand, CategoryDto>, UpdateCategoryCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteCategoryCommand, CommandCompleted>, DeleteCategoryCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetCategoryByIdQuery, CategoryDto?>, GetCategoryByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>, GetCategoriesQueryHandler>();
builder.Services.AddScoped<ICommandHandler<RegisterInventoryMovementCommand, InventoryMovementResult>, RegisterInventoryMovementCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductInventoryQuery, ProductInventoryDto>, GetProductInventoryQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductInventoryMovementsQuery, PagedResult<InventoryMovementDto>>, GetProductInventoryMovementsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductDto?>, GetProductByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>>, GetProductsQueryHandler>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddApiContracts();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory Management API", Version = "v1" });
    var authority = builder.Configuration["Authentication:Authority"]
        ?? throw new InvalidOperationException("Authentication:Authority is required.");
    options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string> { ["openid"] = "Authenticate with Keycloak" }
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" } }] = ["openid"]
    });
});
builder.Services.AddInventoryAuthentication(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseApiStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.OAuthClientId(builder.Configuration["Authentication:SwaggerClientId"]
        ?? throw new InvalidOperationException("Authentication:SwaggerClientId is required."));
    options.OAuthUsePkce();
    options.OAuthScopes("openid");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
}

await app.RunAsync();
