using System;

namespace Torii.Backend;

/// <summary>
/// Options for <see cref="TokenVerifier.VerifyTokenAsync"/>.
/// </summary>
/// <param name="Issuer">Expected issuer URL (per-tenant), e.g. <c>https://acme.torii.so</c> or a verified custom domain. Strict iss validation is the point of OIDC-style verification.</param>
/// <param name="Audiences">Optional audience claim(s) to enforce. torii tokens don't set <c>aud</c> today, so leaving this null skips the check. Reserved for future-compat.</param>
/// <param name="ClockSkew">Clock-skew tolerance for exp/nbf. Defaults to 30 seconds.</param>
public sealed record VerifyOptions(string Issuer, string[]? Audiences = null, TimeSpan? ClockSkew = null);
