using System;
using System.Text.Json.Nodes;

namespace Torii.Backend;

/// <summary>
/// Input for <c>Users.UpdateAsync</c>. Each field uses <see cref="Patch{T}"/> to express
/// three states: omit (default), set to a value, or explicitly clear to <c>null</c>.
/// </summary>
/// <example>
/// <code>
/// var input = new UpdateUserInput
/// {
///     Name = Patch&lt;string&gt;.Set("Ada"),
///     Phone = Patch&lt;string&gt;.Clear(),
///     // AvatarUrl omitted — left untouched on the server
/// };
/// await client.Users.UpdateAsync(userId, input);
/// </code>
/// </example>
public sealed record UpdateUserInput
{
    public Patch<string> Name { get; init; } = Patch<string>.Omit;
    public Patch<string> Phone { get; init; } = Patch<string>.Omit;
    public Patch<string> AvatarUrl { get; init; } = Patch<string>.Omit;
    public Patch<string> Locale { get; init; } = Patch<string>.Omit;
    public Patch<string> Address { get; init; } = Patch<string>.Omit;
    /// <summary>ISO date string, e.g. <c>"1990-07-15"</c>.</summary>
    public Patch<string> DateOfBirth { get; init; } = Patch<string>.Omit;

    internal JsonObject ToJsonBody()
    {
        var obj = new JsonObject();
        Add(obj, "name", Name);
        Add(obj, "phone", Phone);
        Add(obj, "avatarUrl", AvatarUrl);
        Add(obj, "locale", Locale);
        Add(obj, "address", Address);
        Add(obj, "dateOfBirth", DateOfBirth);
        return obj;
    }

    private static void Add(JsonObject obj, string key, Patch<string> p)
    {
        switch (p.Kind)
        {
            case Patch<string>.State.Omitted:
                return;
            case Patch<string>.State.Clear:
                obj[key] = null;
                return;
            case Patch<string>.State.Set:
                obj[key] = JsonValue.Create(p.Value);
                return;
        }
    }
}
