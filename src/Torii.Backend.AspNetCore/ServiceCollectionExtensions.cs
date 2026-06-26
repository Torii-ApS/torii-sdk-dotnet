using System;
using Microsoft.AspNetCore.Authentication;

namespace Torii.Backend.AspNetCore;

/// <summary>
/// Extension methods to register the torii authentication handler.
/// </summary>
public static class ToriiAuthenticationExtensions
{
    /// <summary>
    /// Register the torii authentication handler under the default scheme name
    /// (<see cref="ToriiAuthenticationOptions.DefaultScheme"/>).
    /// </summary>
    public static AuthenticationBuilder AddTorii(
        this AuthenticationBuilder builder,
        Action<ToriiAuthenticationOptions> configure)
        => builder.AddTorii(ToriiAuthenticationOptions.DefaultScheme, configure);

    /// <summary>
    /// Register the torii authentication handler under the supplied scheme name.
    /// </summary>
    public static AuthenticationBuilder AddTorii(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<ToriiAuthenticationOptions> configure)
        => builder.AddScheme<ToriiAuthenticationOptions, ToriiAuthenticationHandler>(authenticationScheme, configure);
}
