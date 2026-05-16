using System;

namespace Torii.Backend;

/// <summary>
/// Thrown when the torii REST API returns a non-2xx response.
/// </summary>
public sealed class ToriiApiException : Exception
{
    public int Status { get; }
    public string? Code { get; }
    public string? SupportId { get; }
    public object? Body { get; }

    public ToriiApiException(string message, int status, string? code = null, string? supportId = null, object? body = null)
        : base(message)
    {
        Status = status;
        Code = code;
        SupportId = supportId;
        Body = body;
    }
}

/// <summary>
/// Thrown when JWT verification or bearer-token extraction fails.
/// </summary>
public sealed class ToriiAuthException : Exception
{
    public ToriiAuthException(string message) : base(message) { }
    public ToriiAuthException(string message, Exception? inner) : base(message, inner) { }
}
