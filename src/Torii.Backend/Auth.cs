using System.Collections.Generic;

namespace Torii.Backend;

/// <summary>
/// Subset of fields the backend SDK exposes from a verified torii access token.
/// For full claim access (custom claims, audience, etc.) read <see cref="Raw"/>.
/// </summary>
/// <param name="UserId">End-user ID (JWT <c>sub</c>).</param>
/// <param name="EnvironmentId">Environment ID this token was issued in (JWT <c>pid</c>).</param>
/// <param name="Issuer">Issuer (JWT <c>iss</c>) — the canonical FAPI URL for this environment.</param>
/// <param name="EmailVerified">True if the end-user has verified at least one email.</param>
/// <param name="ProfileComplete">True if all environment-required profile fields are filled. Defaults to true when claim absent.</param>
/// <param name="Impersonating">True if the token is being used for admin impersonation.</param>
/// <param name="Locale">End-user preferred locale, when set on the profile.</param>
/// <param name="Raw">Raw decoded JWT payload — escape hatch for custom claims, audience checks, etc.</param>
public sealed record Auth(
    string UserId,
    string EnvironmentId,
    string Issuer,
    bool EmailVerified,
    bool ProfileComplete,
    bool Impersonating,
    string? Locale,
    IReadOnlyDictionary<string, object?> Raw);
