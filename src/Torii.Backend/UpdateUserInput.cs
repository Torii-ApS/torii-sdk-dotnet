using System;
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
///     Name = Patch&lt;string&gt;.Set("Ada"),
///     Phone = Patch&lt;string&gt;.Set(null),   // clear
///     // Locale omitted — left untouched on the server
/// };
/// await client.Users.UpdateAsync(userId, input);
/// </code>
/// </example>
public sealed record UpdateUserInput
{
    public Patch<string> Name { get; init; } = Patch<string>.Omit;
    public Patch<string> Phone { get; init; } = Patch<string>.Omit;
    public Patch<string> Locale { get; init; } = Patch<string>.Omit;
    public Patch<string> Address { get; init; } = Patch<string>.Omit;
    /// <summary>ISO date string, e.g. <c>"1990-07-15"</c>. Pass <c>Patch&lt;string&gt;.Set(null)</c> to clear.</summary>
    public Patch<string> DateOfBirth { get; init; } = Patch<string>.Omit;

    internal JsonObject ToJsonBody()
    {
        var obj = new JsonObject();
        Add(obj, "name", Name);
        Add(obj, "phone", Phone);
        Add(obj, "locale", Locale);
        Add(obj, "address", Address);
        Add(obj, "dateOfBirth", DateOfBirth);
        return obj;
    }

    private static void Add(JsonObject obj, string key, Patch<string> p)
    {
        if (p.IsOmitted) return;
        // Patch<T>.Set(value) emits the key; null value → JSON null (clear).
        obj[key] = p.Value is null ? null : JsonValue.Create(p.Value);
    }
}
