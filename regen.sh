#!/usr/bin/env bash
# Regenerate the generated REST client under src/Torii.Backend/Generated/ from
# spec/server-v1.json. The generator emits a standalone project, so we generate
# into a staging dir and sync only the Api/Client/Model source subtrees.
# Idempotent; safe to re-run after a spec bump.
#
#   - library=httpclient            use System.Net.Http (not RestSharp)
#   - packageName=Torii.Backend.Generated  keeps namespaces stable
#   - useDateTimeOffset=true        emit DateTimeOffset? for timestamp fields
#     (Spring sends Instant -> ISO-8601 with offset; without this the generator
#     falls back to DateTime?, losing tz info and breaking ToriiClient.cs).
set -euo pipefail
cd "$(dirname "$0")"

STAGE=$(mktemp -d)
trap 'rm -rf "$STAGE"' EXIT

npx -y @openapitools/openapi-generator-cli generate \
  -i spec/server-v1.json -g csharp -o "$STAGE" \
  --additional-properties=library=httpclient,packageName=Torii.Backend.Generated,useDateTimeOffset=true

GEN="$STAGE/src/Torii.Backend.Generated"
# Validate all three subtrees exist and are non-empty BEFORE deleting the
# committed one, so a generator that exits 0 with a drifted/empty layout can't
# leave src/Torii.Backend/Generated gutted.
for sub in Api Client Model; do
  if [ ! -d "$GEN/$sub" ] || [ -z "$(ls -A "$GEN/$sub")" ]; then
    echo "✗ dotnet: generator produced no $sub/ at $GEN; leaving committed tree intact" >&2
    exit 1
  fi
done
rm -rf src/Torii.Backend/Generated
mkdir -p src/Torii.Backend/Generated
cp -r "$GEN/Api"    src/Torii.Backend/Generated/
cp -r "$GEN/Client" src/Torii.Backend/Generated/
cp -r "$GEN/Model"  src/Torii.Backend/Generated/

# Tri-state PATCH fields: rewrite every nullable `string` property (and its
# constructor parameter) on UpdateUserRequest to a tri-state `Patch<string>`, so
# callers can distinguish omit / clear / set. PatchSerialization wires the
# Newtonsoft resolver + converter that turns that into the wire contract
# (omit a field left null/Patch.Omit, emit a value or an explicit null). Generic
# within the model: a new nullable-string field is converted automatically, no
# hand edits. The csharp generator can't express this and System.Text.Json can't
# omit a property from a value-converter, so it is post-processed here.
PATCH_MODEL=src/Torii.Backend/Generated/Model/UpdateUserRequest.cs
perl -pi -e 's/public string (\w+) \{ get; set; \}/public Torii.Backend.Patch<string> $1 { get; set; }/g; s/\bstring (\w+) = default/Torii.Backend.Patch<string> $1 = default/g' "$PATCH_MODEL"

echo "✓ regenerated src/Torii.Backend/Generated/ from spec/server-v1.json"
