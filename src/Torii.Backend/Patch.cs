namespace Torii.Backend;

/// <summary>
/// Tri-state wrapper for PATCH body fields. Distinguishes three states:
/// <list type="bullet">
///   <item><description><see cref="State.Omitted"/> — field is not included in the request body (default).</description></item>
///   <item><description><see cref="State.Set"/> — field is included with <see cref="Value"/>.</description></item>
///   <item><description><see cref="State.Clear"/> — field is included as JSON <c>null</c>.</description></item>
/// </list>
/// </summary>
public sealed record Patch<T>
{
    public enum State { Omitted, Set, Clear }

    public State Kind { get; }
    public T? Value { get; }

    private Patch(State kind, T? value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Update the field to <paramref name="value"/>.</summary>
    public static Patch<T> Set(T value) => new(State.Set, value);

    /// <summary>Clear the field (sends JSON null).</summary>
    public static Patch<T> Clear() => new(State.Clear, default);

    /// <summary>Omit the field entirely (default).</summary>
    public static Patch<T> Omit { get; } = new(State.Omitted, default);
}
