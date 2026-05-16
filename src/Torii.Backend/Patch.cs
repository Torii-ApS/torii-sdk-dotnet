namespace Torii.Backend;

/// <summary>
/// Tri-state wrapper for PATCH body fields. Mirrors the server-side
/// <c>PatchValue&lt;T&gt;</c> exactly: a "present" state carrying a
/// (possibly null) value, and an "omitted" state that drops the field
/// from the request body.
/// <list type="bullet">
///   <item><description><see cref="Omit"/> — field is not included in the request body (the default).</description></item>
///   <item><description><see cref="Set(T?)"/> with a non-null value — field is updated to that value.</description></item>
///   <item><description><see cref="Set(T?)"/> with <c>null</c> — field is cleared (server-side null).</description></item>
/// </list>
/// </summary>
public sealed record Patch<T>
{
    public bool IsOmitted { get; }
    public T? Value { get; }

    private Patch(bool isOmitted, T? value)
    {
        IsOmitted = isOmitted;
        Value = value;
    }

    /// <summary>Update the field to <paramref name="value"/>. Pass <c>null</c> to clear.</summary>
    public static Patch<T> Set(T? value) => new(false, value);

    /// <summary>Omit the field entirely (default).</summary>
    public static Patch<T> Omit { get; } = new(true, default);
}
