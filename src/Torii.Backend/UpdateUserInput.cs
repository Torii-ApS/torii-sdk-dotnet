using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Torii.Backend;

/// <summary>
/// Input for <c>Users.UpdateAsync</c>. Each field uses <see cref="Patch{T}"/>
/// to express three states: omit (default), set to a value, or set to
/// <c>null</c> (clear). The wrapper mirrors the server-side
/// <c>PatchValue&lt;T&gt;</c> exactly.
/// </summary>
/// <example>
/// <code>
/// var input = new UpdateUserInput
/// {
///     FirstName = Patch&lt;string&gt;.Set("Ada"),
///     LastName = Patch&lt;string&gt;.Set(null),   // clear
///     // Locale omitted — left untouched on the server
/// };
/// await client.Users.UpdateAsync(userId, input);
/// </code>
/// </example>
public sealed record UpdateUserInput
{
    public Patch<string> FirstName { get; init; } = Patch<string>.Omit;
    public Patch<string> LastName { get; init; } = Patch<string>.Omit;
    public Patch<string> Locale { get; init; } = Patch<string>.Omit;
    /// <summary>Tri-state: omit to leave the server's metadata untouched (never clobbered), set to replace, null to clear.</summary>
    public Patch<IReadOnlyDictionary<string, object>> UnsafeMetadata { get; init; } = Patch<IReadOnlyDictionary<string, object>>.Omit;

    internal JsonObject ToJsonBody()
    {
        var obj = new JsonObject();
        Add(obj, "firstName", FirstName);
        Add(obj, "lastName", LastName);
        Add(obj, "locale", Locale);
        if (!UnsafeMetadata.IsOmitted)
        {
            obj["unsafeMetadata"] = UnsafeMetadata.Value is null
                ? null
                : JsonSerializer.SerializeToNode(UnsafeMetadata.Value);
        }
        return obj;
    }

    private static void Add(JsonObject obj, string key, Patch<string> p)
    {
        if (p.IsOmitted) return;
        // Patch<T>.Set(value) emits the key; null value → JSON null (clear).
        obj[key] = p.Value is null ? null : JsonValue.Create(p.Value);
    }
}
