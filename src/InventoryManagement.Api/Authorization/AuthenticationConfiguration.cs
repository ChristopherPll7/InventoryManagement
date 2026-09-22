using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace InventoryManagement.Api.Authorization;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddInventoryAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required.");
        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is required.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            options.TokenValidationParameters.RequireSignedTokens = true;
            options.TokenValidationParameters.RequireExpirationTime = true;
        });
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireClaim(Permissions.ClaimType, permission));
        });
        return services;
    }
}
