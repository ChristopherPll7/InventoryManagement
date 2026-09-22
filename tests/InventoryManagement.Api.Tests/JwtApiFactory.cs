using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using InventoryManagement.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Api.Tests;

public sealed class JwtApiFactory : ContractApiFactory
{
    private readonly RSA rsa = RSA.Create(2048);
    private const string Issuer = "https://issuer.example.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options => options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme);
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(new RsaSecurityKey(rsa) { KeyId = "test-key" });
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }

    public string CreateToken(string? permission, string defect = "none")
    {
        using var otherKey = RSA.Create(2048);
        var key = new RsaSecurityKey(defect == "signature" ? otherKey : rsa) { KeyId = "test-key" };
        var now = DateTime.UtcNow;
        var claims = permission is null ? Array.Empty<Claim>() : [new Claim(Permissions.ClaimType, permission)];
        var token = new JwtSecurityToken(
            issuer: defect == "issuer" ? "https://wrong.example.test" : Issuer,
            audience: defect == "audience" ? "other-api" : "inventory-api",
            claims: claims,
            notBefore: defect == "future" ? now.AddHours(1) : now.AddHours(-2),
            expires: defect == "expired" ? now.AddHours(-1) : now.AddHours(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        if (defect == "expiration")
            token.Payload.Remove("exp");
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            rsa.Dispose();
    }
}
