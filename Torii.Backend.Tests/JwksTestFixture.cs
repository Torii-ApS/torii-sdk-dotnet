// Spin up an in-process JWKS server bound to a random port; mint ES256 JWTs
// against a generated keypair. No external network involved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Torii.Backend.Tests;

public sealed class JwksTestFixture : IAsyncLifetime
{
    public string Issuer { get; private set; } = "";
    public string Kid { get; } = "test-key-1";
    public ECDsa PrivateEc { get; }
    public ECDsa PublicEc { get; }

    private WebApplication? _app;

    public JwksTestFixture()
    {
        PrivateEc = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        // Public-key clone for the JWKS payload.
        var pubParams = PrivateEc.ExportParameters(false);
        PublicEc = ECDsa.Create(pubParams);
    }

    public async Task InitializeAsync()
    {
        TokenVerifier.ClearJwksCacheForTests();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(opts => opts.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();
        var app = builder.Build();

        app.MapGet("/_torii/.well-known/jwks.json", () =>
        {
            var pubParams = PublicEc.ExportParameters(false);
            var jwk = new JsonWebKey
            {
                Kty = "EC",
                Crv = "P-256",
                Alg = "ES256",
                Use = "sig",
                Kid = Kid,
                X = Base64UrlEncoder.Encode(pubParams.Q.X!),
                Y = Base64UrlEncoder.Encode(pubParams.Q.Y!),
            };
            return Results.Json(new { keys = new[] { jwk } });
        });

        await app.StartAsync();
        _app = app;

        var server = app.Services.GetRequiredService<IServer>();
        var addr = server.Features.Get<IServerAddressesFeature>()!.Addresses;
        foreach (var a in addr)
        {
            // Trim trailing slash so it matches what we pass via VerifyOptions.
            Issuer = a.TrimEnd('/');
            break;
        }
    }

    public async Task DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
        PrivateEc.Dispose();
        PublicEc.Dispose();
    }

    public string SignToken(IDictionary<string, object> claims)
    {
        var key = new ECDsaSecurityKey(PrivateEc) { KeyId = Kid };
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(claims),
            Issuer = Issuer,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256),
        };
        return handler.CreateToken(descriptor);
    }

    public string SignWithKey(ECDsa key, IDictionary<string, object> claims, string? issuerOverride = null, DateTime? expires = null, DateTime? issuedAt = null)
    {
        var secKey = new ECDsaSecurityKey(key) { KeyId = Kid };
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(claims),
            Issuer = issuerOverride ?? Issuer,
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(secKey, SecurityAlgorithms.EcdsaSha256),
        };
        return handler.CreateToken(descriptor);
    }
}
