using System;
using System.Collections.Generic;
using Torii.Backend.Generated.Model;

namespace Torii.Backend;

/// <summary>
/// A user record as returned by the torii backend API. Re-exported from the
/// generated client so consumers don't have to import the <c>.Generated</c>
/// namespace.
/// </summary>
public sealed class User
{
    public Guid Id { get; init; }
    public Guid EnvironmentId { get; init; }
    public string? Name { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Locale { get; init; }
    /// <summary>One of: <c>pending_verification</c>, <c>active</c>, <c>banned</c>, <c>deleted</c>.</summary>
    public string Status { get; init; } = "active";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? EmailVerifiedAt { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public IReadOnlyDictionary<string, object>? PublicMetadata { get; init; }
    public IReadOnlyDictionary<string, object>? PrivateMetadata { get; init; }
    public IReadOnlyDictionary<string, object>? UnsafeMetadata { get; init; }

    internal static User FromGenerated(ServerUserResponse r) => new()
    {
        Id = r.Id,
        EnvironmentId = r.EnvironmentId,
        Name = r.Name,
        FirstName = r.FirstName,
        LastName = r.LastName,
        Email = r.Email,
        Locale = r.Locale?.ToString()?.ToLowerInvariant(),
        Status = r.Status.ToString().ToLowerInvariant(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        EmailVerifiedAt = r.EmailVerifiedAt,
        DeletedAt = r.DeletedAt,
        PublicMetadata = r.PublicMetadata,
        PrivateMetadata = r.PrivateMetadata,
        UnsafeMetadata = r.UnsafeMetadata,
    };
}

/// <summary>
/// A user session as returned by the torii backend API.
/// </summary>
public sealed class Session
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid EnvironmentId { get; init; }
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset LastUsedAt { get; init; }

    internal static Session FromGenerated(UserSessionResponse r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        EnvironmentId = r.EnvironmentId,
        UserAgent = r.UserAgent,
        IpAddress = r.IpAddress,
        CreatedAt = r.CreatedAt,
        ExpiresAt = r.ExpiresAt,
        LastUsedAt = r.LastUsedAt,
    };
}

/// <summary>
/// Cursor-paginated response. Walk by calling the list method again with
/// <c>cursor=page.NextCursor</c> until <c>HasMore</c> is <c>false</c>.
/// </summary>
public sealed class CursorPage<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public Guid? NextCursor { get; init; }
    public required bool HasMore { get; init; }
}

/// <summary>Input for <c>Users.CreateAsync</c>.</summary>
public sealed record CreateUserInput(
    string? Email = null,
    string? Password = null,
    string? FirstName = null,
    string? LastName = null,
    IReadOnlyDictionary<string, object>? PublicMetadata = null,
    IReadOnlyDictionary<string, object>? PrivateMetadata = null,
    IReadOnlyDictionary<string, object>? UnsafeMetadata = null);

// UpdateUserInput lives in UpdateUserInput.cs (uses Patch<T> tri-state wrappers).
