using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Torii.Backend.AspNetCore;

/// <summary>
/// ASP.NET Core authentication handler that verifies torii bearer tokens via
/// <see cref="TokenVerifier.VerifyTokenAsync"/>. On success, the resulting
/// <see cref="Auth"/> is materialised as a <see cref="ClaimsPrincipal"/> and
/// also stashed on <c>HttpContext.Items["Torii:Auth"]</c> for direct access.
/// </summary>
public sealed class ToriiAuthenticationHandler : AuthenticationHandler<ToriiAuthenticationOptions>
{
    /// <summary>Key under which the <see cref="Auth"/> object is stashed on <c>HttpContext.Items</c>.</summary>
    public const string AuthItemKey = "Torii:Auth";

    public ToriiAuthenticationHandler(
        IOptionsMonitor<ToriiAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.Issuer))
            return AuthenticateResult.Fail("Torii: Issuer must be configured.");

        var headerName = string.IsNullOrEmpty(Options.HeaderName) ? "Authorization" : Options.HeaderName;
        var headerValues = Request.Headers[headerName];
        if (headerValues.Count == 0 || string.IsNullOrWhiteSpace(headerValues.ToString()))
        {
            // No header — let the next handler (or anonymous fallback) try.
            return AuthenticateResult.NoResult();
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [headerName] = headerValues.ToString(),
        };

        var verifyOpts = new VerifyOptions(
            Issuer: Options.Issuer,
            Audiences: Options.Audiences,
            ClockSkew: Options.ClockSkew);

        Auth auth;
        try
        {
            auth = await TokenVerifier.AuthenticateRequestAsync(headers, verifyOpts, headerName, Context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (ToriiAuthException ex)
        {
            return AuthenticateResult.Fail(ex.Message);
        }

        Context.Items[AuthItemKey] = auth;

        var identity = new ClaimsIdentity(Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, auth.UserId));
        identity.AddClaim(new Claim("sub", auth.UserId));
        identity.AddClaim(new Claim("pid", auth.EnvironmentId));
        identity.AddClaim(new Claim("iss", auth.Issuer));
        identity.AddClaim(new Claim("email_verified", auth.EmailVerified ? "true" : "false"));
        identity.AddClaim(new Claim("profile_complete", auth.ProfileComplete ? "true" : "false"));
        identity.AddClaim(new Claim("impersonating", auth.Impersonating ? "true" : "false"));
        if (!string.IsNullOrEmpty(auth.Locale))
            identity.AddClaim(new Claim("locale", auth.Locale));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
