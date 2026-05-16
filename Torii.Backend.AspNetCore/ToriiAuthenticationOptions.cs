using System;
using Microsoft.AspNetCore.Authentication;

namespace Torii.Backend.AspNetCore;

/// <summary>
/// Options for <see cref="ToriiAuthenticationHandler"/>. Configured via
/// <c>services.AddAuthentication().AddTorii(opts =&gt; opts.Issuer = "...")</c>.
/// </summary>
public sealed class ToriiAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Default scheme name when calling <c>AddTorii()</c> without an explicit scheme.</summary>
    public const string DefaultScheme = "Torii";

    /// <summary>Expected issuer URL (per-tenant), e.g. <c>https://acme.torii.so</c>. Required.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Optional audience claim(s) to enforce. Leave empty to skip the check.</summary>
    public string[]? Audiences { get; set; }

    /// <summary>Clock-skew tolerance for exp/nbf. Defaults to 30 seconds.</summary>
    public TimeSpan? ClockSkew { get; set; }

    /// <summary>
    /// Header to read the bearer token from. Defaults to <c>Authorization</c>.
    /// Override for gateways that forward the token elsewhere.
    /// </summary>
    public string HeaderName { get; set; } = "Authorization";
}
