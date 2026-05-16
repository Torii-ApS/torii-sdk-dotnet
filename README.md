# Torii.Backend

Backend SDK for [torii](https://torii.so) — verify end-user JWTs without a per-request round trip and manage users from your .NET server.

> **v0.x — API may still change.**

## Setup

1. Sign in to [app.torii.so](https://app.torii.so) and from your dashboard copy:
   - your **issuer URL** (e.g. `https://acme.torii.so`)
   - a **secret key** (`sk_test_…` for development, `sk_live_…` for production)

2. Install the package:

   ```sh
   dotnet add package Torii.Backend
   # ASP.NET Core adapter (optional)
   dotnet add package Torii.Backend.AspNetCore
   ```

   Targets `net8.0`.

3. Verify an end-user JWT:

   ```csharp
   using Torii.Backend;

   var auth = await TokenVerifier.VerifyTokenAsync(
       token,
       new VerifyOptions(Issuer: "https://acme.torii.so"));

   Console.WriteLine($"{auth.UserId} {auth.EnvironmentId} {auth.EmailVerified}");
   ```

   The first call fetches the issuer's JWKS; subsequent calls reuse the cache and rotate keys automatically via `Microsoft.IdentityModel`'s `ConfigurationManager`. No round-trip per request.

4. Call the backend REST API:

   ```csharp
   using Torii.Backend;

   using var torii = ToriiClient.Create(
       secretKey: Environment.GetEnvironmentVariable("TORII_SECRET_KEY")!);

   var user = await torii.Users.GetAsync(userId);
   ```

   Default base URL is `https://api.torii.so`. Override with the `apiUrl` argument for staging or self-hosted. Pass an `HttpClient` (e.g., from `IHttpClientFactory`) to share connection pooling or inject test fakes.

## ASP.NET Core

```csharp
using Torii.Backend.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(ToriiAuthenticationOptions.DefaultScheme)
    .AddTorii(opts =>
    {
        opts.Issuer = "https://acme.torii.so";
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/me", (HttpContext ctx) =>
{
    var auth = (Auth)ctx.Items[ToriiAuthenticationHandler.AuthItemKey]!;
    return Results.Ok(new { auth.UserId, auth.EnvironmentId });
}).RequireAuthorization();

app.Run();
```

The handler maps the verified JWT into a `ClaimsPrincipal` (claims: `sub`, `pid`, `iss`, `email_verified`, `profile_complete`, `impersonating`, `locale`) and also stashes the raw `Auth` object on `HttpContext.Items[ToriiAuthenticationHandler.AuthItemKey]` for direct access.

## Backend REST API

```csharp
var page = await torii.Users.ListAsync(limit: 50);

var user = await torii.Users.CreateAsync(new CreateUserInput(Email: "x@y.com"));
await torii.Users.BanAsync(user.Id);

var sessions = await torii.Sessions.ListForUserAsync(user.Id);
await torii.Sessions.RevokeAllForUserAsync(user.Id);
```

### Partial updates (`Users.UpdateAsync`)

`UpdateUserInput` uses `Patch<T>` so each field has three states:

- **Set** — change the field to a value: `Patch<string>.Set("Ada")`
- **Clear** — explicitly null the field server-side: `Patch<string>.Set(null)`
- **Omit** — don't touch the field (default): leave the property at its initial value

```csharp
await torii.Users.UpdateAsync(user.Id, new UpdateUserInput
{
    Name = Patch<string>.Set("Ada Lovelace"),
    Phone = Patch<string>.Set(null),          // sends "phone": null
    // AvatarUrl omitted — left untouched
    DateOfBirth = Patch<string>.Set("1815-12-10"),
});
```

C# nullable reference types only distinguish two states (`null` vs value), which conflates "leave alone" with "clear". `Patch<T>` is the third state.

## Building from source

```sh
git clone https://github.com/Torii-ApS/torii-sdk-dotnet
cd torii-sdk-dotnet
dotnet build
dotnet test
```

## License

MIT
