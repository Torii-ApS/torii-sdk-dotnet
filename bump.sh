#!/usr/bin/env bash
# Set the published package version. Called by the torii release train (and
# `just sdk-release`) right before tagging. release.yml asserts the tag against
# BOTH csproj <Version> values, so bump both packages in lockstep.
set -euo pipefail
cd "$(dirname "$0")"

VERSION="${1:?usage: ./bump.sh <version>  (e.g. 0.0.5)}"
VERSION="${VERSION#v}"
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.]+)?$ ]]; then
	echo "✗ invalid version: '$VERSION'" >&2
	exit 1
fi

for f in src/Torii.Backend/Torii.Backend.csproj src/Torii.Backend.AspNetCore/Torii.Backend.AspNetCore.csproj; do
	perl -i -pe 's|(<Version>)[^<]*(</Version>)|${1}'"$VERSION"'${2}|' "$f"
	grep -q "<Version>$VERSION</Version>" "$f" || { echo "✗ $f not bumped to $VERSION" >&2; exit 1; }
done
echo "✓ torii-sdk-dotnet -> $VERSION (Torii.Backend + Torii.Backend.AspNetCore csproj)"
