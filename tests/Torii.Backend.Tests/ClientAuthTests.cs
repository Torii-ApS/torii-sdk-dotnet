using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torii.Backend;
using Torii.Backend.Generated.Model;
using Xunit;

namespace Torii.Backend.Tests;

// The secret key must reach the wire as `Authorization: Bearer <key>` on every
// call — the generated path (Get/Create) via the config default header, and the
// hand-rolled PATCH (Update) via its explicit header. And create must omit
// unset metadata bags so the server defaults them to {}.
public class ClientAuthTests
{
    private const string UserJson = """
        {
          "id": "11111111-1111-1111-1111-111111111111",
          "environmentId": "22222222-2222-2222-2222-222222222222",
          "status": "active",
          "createdAt": "2024-01-01T00:00:00Z",
          "updatedAt": "2024-01-01T00:00:00Z",
          "publicMetadata": {},
          "privateMetadata": {},
          "unsafeMetadata": {}
        }
        """;

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? LastAuth;
        public string? LastBody;
        public string? LastMethod;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method.Method;
            LastAuth = request.Headers.TryGetValues("Authorization", out var values)
                ? string.Concat(values)
                : null;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(UserJson, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (ToriiClient client, CapturingHandler handler) NewClient()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler);
        return (ToriiClient.Create("sk_test_abc", "https://api.example", http), handler);
    }

    [Fact]
    public async Task Generated_call_sends_bearer_token()
    {
        var (client, handler) = NewClient();
        await client.Users.GetAsync(Guid.NewGuid());
        Assert.Equal("Bearer sk_test_abc", handler.LastAuth);
    }

    [Fact]
    public async Task Create_sends_bearer_and_omits_unset_metadata()
    {
        var (client, handler) = NewClient();
        await client.Users.CreateAsync(new CreateUserInput { Email = "ada@example.com" });
        Assert.Equal("Bearer sk_test_abc", handler.LastAuth);
        Assert.Equal("POST", handler.LastMethod);
        Assert.DoesNotContain("Metadata", handler.LastBody);
    }

    [Fact]
    public async Task Update_sends_bearer_token()
    {
        var (client, handler) = NewClient();
        await client.Users.UpdateAsync(
            Guid.NewGuid(),
            new UpdateUserRequest { FirstName = Patch<string>.Set("Ada") });
        Assert.Equal("Bearer sk_test_abc", handler.LastAuth);
    }
}
