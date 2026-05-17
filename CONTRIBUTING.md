# Contributing

Thanks for your interest in `torii-sdk-dotnet`!

## Reporting bugs

Open an issue with:

- The version of `Torii.Backend` (and `Torii.Backend.AspNetCore`, if applicable) you're using.
- A minimal reproduction — a few lines that exhibit the bug.
- What you expected to happen vs. what actually happened.

For security-sensitive issues (anything that could let an attacker forge or bypass token verification), please email **security@torii.so** instead of filing a public issue.

## Development

```sh
git clone https://github.com/Torii-ApS/torii-sdk-dotnet
cd torii-sdk-dotnet
dotnet restore
dotnet build
dotnet test
```

The REST client under `src/Torii.Backend/Generated/` is produced by [`openapi-generator`](https://openapi-generator.tech/) from `spec/server-v1.json`. Don't hand-edit it. To regenerate after a spec update, run the generator into a staging dir (the generator emits a standalone project we don't want), then sync only the source subtree:

```sh
# 1. Regenerate into a clean staging dir. The flags matter:
#    - library=httpclient            → use System.Net.Http (not RestSharp)
#    - packageName=Torii.Backend.Generated → keeps namespaces stable
#    - useDateTimeOffset=true        → emit DateTimeOffset? for timestamp
#      fields. Spring sends `Instant` → ISO-8601 with offset. Without this
#      flag the generator falls back to DateTime?, which loses tz info
#      and breaks the hand-written wrapper in ToriiClient.cs.
rm -rf /tmp/oag-stage && mkdir /tmp/oag-stage
npx -y @openapitools/openapi-generator-cli generate \
  -i spec/server-v1.json -g csharp -o /tmp/oag-stage \
  --additional-properties=library=httpclient,packageName=Torii.Backend.Generated,useDateTimeOffset=true

# 2. Replace src/Torii.Backend/Generated/ with the regenerated subtree only
#    (drop the generator's Torii.Backend.Generated.csproj, .sln, README, etc).
rm -rf src/Torii.Backend/Generated
mkdir -p src/Torii.Backend/Generated
cp -r /tmp/oag-stage/src/Torii.Backend.Generated/Api    src/Torii.Backend/Generated/
cp -r /tmp/oag-stage/src/Torii.Backend.Generated/Client src/Torii.Backend/Generated/
cp -r /tmp/oag-stage/src/Torii.Backend.Generated/Model  src/Torii.Backend/Generated/

# 3. Verify.
dotnet build && dotnet test
```

The hand-written surface is where bug reports and PRs typically land:

- `Torii.Backend/ToriiClient.cs`
- `Torii.Backend/Auth.cs`
- `Torii.Backend/Verify.cs`
- `Torii.Backend/VerifyOptions.cs`
- `Torii.Backend/Errors.cs`
- `Torii.Backend/Types.cs`
- the entire `Torii.Backend.AspNetCore/` project

## Pull requests

1. Open an issue first for non-trivial changes so we can discuss the shape.
2. Branch off `main`, name it `fix/<short>` or `feat/<short>`.
3. Run `dotnet build` and `dotnet test` before pushing — CI checks both.
4. Keep PRs small and focused. One concern per PR.
5. Update `README.md` if you change the public surface.

## Releases

Tagged off `main`. Bump the `<Version>` in `Torii.Backend/Torii.Backend.csproj` and `Torii.Backend.AspNetCore/Torii.Backend.AspNetCore.csproj`, then:

```sh
git tag v0.0.2
git push origin v0.0.2
```

Consumers pick up the new version via `dotnet add package Torii.Backend --version 0.0.2`.

## Code of Conduct

Be kind. Disagreements happen; argue the position, not the person.
