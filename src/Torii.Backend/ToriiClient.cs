using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Torii.Backend.Generated.Api;
using Torii.Backend.Generated.Client;
using Torii.Backend.Generated.Model;

namespace Torii.Backend;

/// <summary>
/// Entry point for the torii backend REST API. Holds long-lived HTTP machinery;
/// dispose at app shutdown. Most apps create one instance at startup and reuse it.
/// </summary>
public sealed class ToriiClient : IDisposable
{
    private const string DefaultApiUrl = "https://api.torii.so";

    /// <summary>User management endpoints (CRUD, search, ban/unban).</summary>
    public UsersClient Users { get; }
    /// <summary>Session management endpoints (list, revoke).</summary>
    public SessionsClient Sessions { get; }

    private readonly HttpClient? _ownedHttp;
    private readonly bool _ownsHttp;

    private ToriiClient(UsersClient users, SessionsClient sessions, HttpClient? ownedHttp, bool ownsHttp)
    {
        Users = users;
        Sessions = sessions;
        _ownedHttp = ownedHttp;
        _ownsHttp = ownsHttp;
    }

    /// <summary>
    /// Construct a torii backend client.
    /// </summary>
    /// <param name="secretKey">Backend secret key (sk_live_... or sk_test_...).</param>
    /// <param name="apiUrl">Backend API base URL. Defaults to <c>https://api.torii.so</c>. Override for staging or self-hosted.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> to reuse (e.g., from <c>IHttpClientFactory</c>). If omitted, an HttpClient is created and owned by this instance.</param>
    public static ToriiClient Create(string secretKey, string? apiUrl = null, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("secretKey is required", nameof(secretKey));

        var basePath = (apiUrl ?? DefaultApiUrl).TrimEnd('/');
        var config = new Configuration { BasePath = basePath };
        // torii's /api/server/** uses bearer secret-key auth.
        config.DefaultHeaders["Authorization"] = $"Bearer {secretKey}";
        config.UserAgent = $"torii-backend-dotnet/{ToriiVersion.SdkVersion}";

        HttpClient http;
        bool owns;
        if (httpClient is null)
        {
            http = new HttpClient();
            owns = true;
        }
        else
        {
            http = httpClient;
            owns = false;
        }

        var usersApi = new ServerUsersApi(http, config);
        var sessionsApi = new ServerSessionsApi(http, config);

        return new ToriiClient(
            users: new UsersClient(usersApi),
            sessions: new SessionsClient(sessionsApi),
            ownedHttp: http,
            ownsHttp: owns);
    }

    public void Dispose()
    {
        if (_ownsHttp) _ownedHttp?.Dispose();
    }
}

internal static class ToriiVersion
{
    public const string SdkVersion = "0.0.1";
}

/// <summary>Wraps the generated <see cref="ServerUsersApi"/> with the public SDK surface.</summary>
public sealed class UsersClient
{
    private readonly ServerUsersApi _api;
    internal UsersClient(ServerUsersApi api) { _api = api; }

    /// <summary>
    /// Search users. Optional filters; cursor-paginated. Pass the previous
    /// page's <c>NextCursor</c> to advance.
    /// </summary>
    public async Task<CursorPage<User>> ListAsync(
        int? limit = null,
        Guid? cursor = null,
        string? name = null,
        string? email = null,
        IEnumerable<string>? statuses = null,
        DateTimeOffset? createdAfter = null,
        DateTimeOffset? createdBefore = null,
        CancellationToken ct = default)
    {
        var body = new ServerUserSearchRequest(
            name: name ?? (object?)null!,
            email: email ?? (object?)null!,
            statuses: statuses is null ? null! : (object)new List<string>(statuses),
            createdAfter: createdAfter,
            createdBefore: createdBefore);

        try
        {
            var resp = await _api.SearchUsersAsync(limit, cursor, body, ct).ConfigureAwait(false);
            var items = new List<User>(resp.Items?.Count ?? 0);
            if (resp.Items is not null)
                foreach (var u in resp.Items) items.Add(User.FromGenerated(u));
            return new CursorPage<User>
            {
                Items = items,
                NextCursor = resp.NextCursor,
                HasMore = resp.HasMore,
            };
        }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task<User> GetAsync(Guid userId, CancellationToken ct = default)
    {
        try { return User.FromGenerated(await _api.GetUserAsync(userId, ct).ConfigureAwait(false)); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task<User> CreateAsync(CreateUserInput input, CancellationToken ct = default)
    {
        var req = new CreateUserRequest(
            email: input.Email,
            password: input.Password,
            name: input.Name,
            phone: input.Phone,
            address: input.Address,
            dateOfBirth: input.DateOfBirth);
        try { return User.FromGenerated(await _api.CreateUserAsync(req, ct).ConfigureAwait(false)); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task<User> UpdateAsync(Guid userId, UpdateUserInput input, CancellationToken ct = default)
    {
        // UpdateUserRequest properties are typed `Object` (untyped JSON) so the
        // server can distinguish "field omitted" from "field set to null". We
        // only attach properties the caller provided.
        var req = new UpdateUserRequest();
        if (input.Name is not null) req.Name = input.Name;
        if (input.Phone is not null) req.Phone = input.Phone;
        if (input.AvatarUrl is not null) req.AvatarUrl = input.AvatarUrl;
        if (input.Locale is not null) req.Locale = input.Locale;
        if (input.Address is not null) req.Address = input.Address;
        if (input.DateOfBirth is not null) req.DateOfBirth = input.DateOfBirth.Value.ToString("yyyy-MM-dd");
        try { return User.FromGenerated(await _api.UpdateUserAsync(userId, req, ct).ConfigureAwait(false)); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        try { await _api.DeleteUserAsync(userId, ct).ConfigureAwait(false); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task<User> BanAsync(Guid userId, CancellationToken ct = default)
    {
        try { return User.FromGenerated(await _api.BanUserAsync(userId, ct).ConfigureAwait(false)); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    public async Task<User> UnbanAsync(Guid userId, CancellationToken ct = default)
    {
        try { return User.FromGenerated(await _api.UnbanUserAsync(userId, ct).ConfigureAwait(false)); }
        catch (ApiException ex) { throw Wrap(ex); }
    }

    internal static ToriiApiException Wrap(ApiException ex) => ToriiApiExceptionFactory.From(ex);
}

/// <summary>Wraps the generated <see cref="ServerSessionsApi"/> with the public SDK surface.</summary>
public sealed class SessionsClient
{
    private readonly ServerSessionsApi _api;
    internal SessionsClient(ServerSessionsApi api) { _api = api; }

    public async Task<IReadOnlyList<Session>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _api.ListSessionsAsync(userId, ct).ConfigureAwait(false);
            var items = new List<Session>(resp?.Count ?? 0);
            if (resp is not null) foreach (var s in resp) items.Add(Session.FromGenerated(s));
            return items;
        }
        catch (ApiException ex) { throw ToriiApiExceptionFactory.From(ex); }
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        try { await _api.RevokeAllSessionsAsync(userId, ct).ConfigureAwait(false); }
        catch (ApiException ex) { throw ToriiApiExceptionFactory.From(ex); }
    }

    public async Task RevokeAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        try { await _api.RevokeSessionAsync(userId, sessionId, ct).ConfigureAwait(false); }
        catch (ApiException ex) { throw ToriiApiExceptionFactory.From(ex); }
    }
}

internal static class ToriiApiExceptionFactory
{
    public static ToriiApiException From(ApiException ex)
    {
        string? code = null;
        string? supportId = null;
        var body = ex.ErrorContent;
        if (body is string s && !string.IsNullOrEmpty(s))
        {
            try
            {
                var parsed = Newtonsoft.Json.Linq.JObject.Parse(s);
                code = parsed.Value<string>("code");
                supportId = parsed.Value<string>("supportId");
            }
            catch
            {
                // body wasn't JSON; leave code/supportId null.
            }
        }
        return new ToriiApiException(ex.Message, ex.ErrorCode, code, supportId, body);
    }
}
