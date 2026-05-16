// Networkless JWT verification. The first call to VerifyTokenAsync() for a given
// issuer fetches that issuer's JWKS; subsequent calls reuse the cached JWKS until
// the cache TTL expires or kid rotation forces a re-fetch. This is the core DX win
// behind a backend SDK — no per-request round trip to torii.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Torii.Backend;

/// <summary>
/// Verifies torii-issued JWTs against the per-tenant JWKS. ES256 only; iss is
/// validated strictly. JWKS is cached per-issuer for the process lifetime, with
/// automatic key rotation when an unknown <c>kid</c> is presented.
/// </summary>
public static class TokenVerifier
{
    // Cache one ConfigurationManager per issuer. The framework's
    // ConfigurationManager handles TTL refresh + kid rotation internally,
    // so we want one long-lived instance per issuer rather than re-fetching
    // for every verify call.
    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> Configs = new();

    private static ConfigurationManager<OpenIdConnectConfiguration> ConfigForIssuer(string issuer)
    {
        var normalized = issuer.TrimEnd('/');
        return Configs.GetOrAdd(normalized, key =>
        {
            // Hard-coded path: torii's JWKS endpoint lives at /_torii/.well-known/jwks.json
            // for every tenant. Stable contract documented in OIDC discovery; we skip the
            // discovery round-trip on the cold path for that reason.
            var jwksUri = $"{key}/_torii/.well-known/jwks.json";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                jwksUri,
                new JwksRetriever(),
                new HttpDocumentRetriever { RequireHttps = false });
        });
    }

    /// <summary>
    /// Verify a torii-issued JWT against the issuer's JWKS.
    /// </summary>
    /// <exception cref="ToriiAuthException">Raised on any validation failure.</exception>
    public static async Task<Auth> VerifyTokenAsync(string token, VerifyOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ToriiAuthException("VerifyTokenAsync: token must be a non-empty string");
        if (options is null)
            throw new ToriiAuthException("VerifyTokenAsync: options is required");
        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new ToriiAuthException("VerifyTokenAsync: Issuer is required");

        var config = ConfigForIssuer(options.Issuer);

        OpenIdConnectConfiguration discovered;
        try
        {
            discovered = await config.GetConfigurationAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new ToriiAuthException($"JWT verification failed: unable to fetch JWKS ({ex.Message})", ex);
        }

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = options.Issuer.TrimEnd('/'),
            ValidateIssuer = true,
            // torii doesn't set `aud` today; only validate when caller opted in.
            ValidateAudience = options.Audiences is { Length: > 0 },
            ValidAudiences = options.Audiences,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = discovered.SigningKeys,
            // Strict: ES256 only.
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },
            ClockSkew = options.ClockSkew ?? TimeSpan.FromSeconds(30),
            // Don't auto-map .NET claim type URIs onto JWT claim names.
            NameClaimType = "sub",
        };

        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        TokenValidationResult result;
        try
        {
            result = await handler.ValidateTokenAsync(token, parameters).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new ToriiAuthException($"JWT verification failed: {ex.Message}", ex);
        }

        if (!result.IsValid || result.Exception is not null)
        {
            var msg = result.Exception?.Message ?? "invalid token";
            throw new ToriiAuthException($"JWT verification failed: {msg}", result.Exception);
        }

        // Extract claims. JsonWebTokenHandler normalizes parsed values into
        // result.Claims (a dictionary of object). We also walk the JWT to
        // collect the full payload as the `Raw` map.
        var claims = result.Claims;

        if (!TryString(claims, "sub", out var userId)
            || !TryString(claims, "iss", out var iss))
        {
            throw new ToriiAuthException("JWT is missing required claims (sub, iss)");
        }
        if (!TryString(claims, "pid", out var environmentId))
        {
            throw new ToriiAuthException("JWT is missing required claim (pid)");
        }
        // iat/exp validity is enforced via TokenValidationParameters; we still
        // require the claims be present for parity with Node/Python.
        if (!claims.ContainsKey("iat") || !claims.ContainsKey("exp"))
        {
            throw new ToriiAuthException("JWT is missing required claims (iat, exp)");
        }

        var emailVerified = ReadBool(claims, "email_verified", false);
        // profile_complete defaults to true if absent
        var profileComplete = ReadBoolNullable(claims, "profile_complete") ?? true;
        var impersonating = ReadBool(claims, "impersonating", false);
        TryString(claims, "locale", out var localeStr);

        var raw = BuildRaw(claims);

        return new Auth(
            UserId: userId!,
            EnvironmentId: environmentId!,
            Issuer: iss!,
            EmailVerified: emailVerified,
            ProfileComplete: profileComplete,
            Impersonating: impersonating,
            Locale: localeStr,
            Raw: raw);
    }

    /// <summary>
    /// Extract a bearer token from <paramref name="headers"/> and verify it.
    /// Header lookup is case-insensitive; <c>Authorization</c> by default.
    /// </summary>
    public static Task<Auth> AuthenticateRequestAsync(
        IDictionary<string, string> headers,
        VerifyOptions options,
        CancellationToken ct = default)
    {
        return AuthenticateRequestAsync(headers, options, "Authorization", ct);
    }

    /// <summary>
    /// Like <see cref="AuthenticateRequestAsync(IDictionary{string,string}, VerifyOptions, CancellationToken)"/>
    /// but allows overriding the header name (e.g., for gateways that forward the token elsewhere).
    /// </summary>
    public static async Task<Auth> AuthenticateRequestAsync(
        IDictionary<string, string> headers,
        VerifyOptions options,
        string headerName,
        CancellationToken ct = default)
    {
        if (headers is null) throw new ToriiAuthException($"Missing {headerName} header");
        string? raw = null;
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                raw = kv.Value;
                break;
            }
        }
        if (string.IsNullOrWhiteSpace(raw))
            throw new ToriiAuthException($"Missing {headerName} header");

        var parts = raw!.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !string.Equals(parts[0], "Bearer", StringComparison.OrdinalIgnoreCase))
            throw new ToriiAuthException($"{headerName} header is not in 'Bearer <token>' form");

        return await VerifyTokenAsync(parts[1].Trim(), options, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Test-only: clear the JWKS cache. Production code should never call this —
    /// the framework's ConfigurationManager handles rotation via <c>kid</c>
    /// lookup automatically.
    /// </summary>
    public static void ClearJwksCacheForTests() => Configs.Clear();

    private static bool TryString(IDictionary<string, object> claims, string key, out string? value)
    {
        if (claims.TryGetValue(key, out var obj) && obj is string s && !string.IsNullOrEmpty(s))
        {
            value = s;
            return true;
        }
        value = null;
        return false;
    }

    private static bool ReadBool(IDictionary<string, object> claims, string key, bool fallback)
        => ReadBoolNullable(claims, key) ?? fallback;

    private static bool? ReadBoolNullable(IDictionary<string, object> claims, string key)
    {
        if (!claims.TryGetValue(key, out var obj) || obj is null) return null;
        if (obj is bool b) return b;
        if (obj is string s && bool.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    private static IReadOnlyDictionary<string, object?> BuildRaw(IDictionary<string, object> claims)
    {
        var raw = new Dictionary<string, object?>(claims.Count);
        foreach (var kv in claims) raw[kv.Key] = kv.Value;
        return raw;
    }

    /// <summary>
    /// Custom retriever so we treat <c>jwks.json</c> as the OIDC config root.
    /// The default OpenIdConnectConfigurationRetriever expects a discovery
    /// document with <c>jwks_uri</c> pointing to the keys; we skip discovery
    /// and just deserialize the JWKS directly.
    /// </summary>
    private sealed class JwksRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var doc = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
            var config = new OpenIdConnectConfiguration();
            var jwks = new JsonWebKeySet(doc);
            foreach (var key in jwks.GetSigningKeys()) config.SigningKeys.Add(key);
            return config;
        }
    }
}

/// <summary>
/// Outbound webhook signature verification. <strong>Stub:</strong> torii's
/// outbound webhook subsystem has not shipped yet. This stub keeps the SDK
/// surface stable so adopting it later doesn't break callers.
/// Track progress on GitHub issue #424 (Phase 0.5).
/// </summary>
public static class WebhookVerifier
{
    public static object VerifyWebhook(string secret, IDictionary<string, string> headers, string payload)
    {
        throw new ToriiAuthException(
            "verifyWebhook: torii's outbound webhook subsystem has not shipped yet — see #424 Phase 0.5");
    }
}
