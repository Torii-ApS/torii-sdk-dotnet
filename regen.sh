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

echo "✓ regenerated src/Torii.Backend/Generated/ from spec/server-v1.json"
