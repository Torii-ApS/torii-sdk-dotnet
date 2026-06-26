using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Torii.Backend.Tests;

public sealed class VerifyTokenTests : IClassFixture<JwksTestFixture>
{
    private readonly JwksTestFixture _fx;

    public VerifyTokenTests(JwksTestFixture fx)
    {
        _fx = fx;
        // Each test gets a clean JWKS cache so issuer-rebound runs don't bleed.
        TokenVerifier.ClearJwksCacheForTests();
    }

    [Fact]
    public async Task Verifies_well_formed_jwt_and_extracts_claims()
    {
        var token = _fx.SignToken(new Dictionary<string, object>
        {
            ["sub"] = "user_123",
            ["pid"] = "env_abc",
            ["email_verified"] = true,
            ["profile_complete"] = true,
            ["locale"] = "en",
        });

        var auth = await TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer));

        Assert.Equal("user_123", auth.UserId);
        Assert.Equal("env_abc", auth.EnvironmentId);
        Assert.Equal(_fx.Issuer, auth.Issuer);
        Assert.True(auth.EmailVerified);
        Assert.True(auth.ProfileComplete);
        Assert.False(auth.Impersonating);
        Assert.Equal("en", auth.Locale);
        Assert.True(auth.Raw.ContainsKey("sub"));
    }

    [Fact]
    public async Task Profile_complete_defaults_to_true_when_claim_absent()
    {
        var token = _fx.SignToken(new Dictionary<string, object>
        {
            ["sub"] = "u",
            ["pid"] = "e",
        });
        var auth = await TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer));
        Assert.True(auth.ProfileComplete);
    }

    [Fact]
    public async Task Rejects_jwt_signed_by_different_key()
    {
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var token = _fx.SignWithKey(other, new Dictionary<string, object>
        {
            ["sub"] = "u",
            ["pid"] = "e",
        });
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer)));
    }

    [Fact]
    public async Task Rejects_jwt_with_wrong_issuer()
    {
        var token = _fx.SignWithKey(_fx.PrivateEc, new Dictionary<string, object>
        {
            ["sub"] = "u",
            ["pid"] = "e",
        }, issuerOverride: "http://wrong-issuer.example");
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer)));
    }

    [Fact]
    public async Task Rejects_jwt_missing_required_claim_pid()
    {
        var token = _fx.SignToken(new Dictionary<string, object>
        {
            ["sub"] = "u",
            // no pid
        });
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer)));
    }

    [Fact]
    public async Task Rejects_expired_jwt()
    {
        var token = _fx.SignWithKey(_fx.PrivateEc,
            new Dictionary<string, object> { ["sub"] = "u", ["pid"] = "e" },
            expires: DateTime.UtcNow.AddMinutes(-5),
            issuedAt: DateTime.UtcNow.AddMinutes(-10));
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.VerifyTokenAsync(token, new VerifyOptions(_fx.Issuer)));
    }

    [Fact]
    public async Task Authenticate_request_reads_bearer_token()
    {
        var token = _fx.SignToken(new Dictionary<string, object>
        {
            ["sub"] = "u",
            ["pid"] = "e",
        });
        var headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };
        var auth = await TokenVerifier.AuthenticateRequestAsync(headers, new VerifyOptions(_fx.Issuer));
        Assert.Equal("u", auth.UserId);
    }

    [Fact]
    public async Task Authenticate_request_rejects_missing_header()
    {
        var headers = new Dictionary<string, string>();
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.AuthenticateRequestAsync(headers, new VerifyOptions(_fx.Issuer)));
    }

    [Fact]
    public async Task Authenticate_request_rejects_non_bearer()
    {
        var headers = new Dictionary<string, string> { ["authorization"] = "Basic abc" };
        await Assert.ThrowsAsync<ToriiAuthException>(() =>
            TokenVerifier.AuthenticateRequestAsync(headers, new VerifyOptions(_fx.Issuer)));
    }
}
