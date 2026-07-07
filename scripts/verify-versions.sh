#!/usr/bin/env bash
# Verify ZenohVersion, package Version, Cargo.toml, and UPM package.json stay in sync.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSIONS_PROPS="$ROOT/build/Versions.props"
CARGO_TOML="$ROOT/native/zenoh-ffi/Cargo.toml"
PACKAGE_JSON="$ROOT/src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity/package.json"

fail() {
  echo "verify-versions: $*" >&2
  exit 1
}

[[ -f "$VERSIONS_PROPS" ]] || fail "missing $VERSIONS_PROPS"

read_prop() {
  local file="$1" name="$2"
  grep -E "<${name}>" "$file" | head -1 | sed -E "s/.*<${name}>([^<]+)<.*/\1/" | tr -d '[:space:]'
}

ZENOH_VERSION="$(read_prop "$VERSIONS_PROPS" ZenohVersion)"
PACKAGE_VERSION="$(read_prop "$VERSIONS_PROPS" Version)"

[[ -n "$ZENOH_VERSION" ]] || fail "ZenohVersion is empty in Versions.props"
[[ -n "$PACKAGE_VERSION" ]] || fail "Version is empty in Versions.props"

if [[ ! "$PACKAGE_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  fail "Version must be four numeric segments (e.g. 1.9.0.0), got '$PACKAGE_VERSION'"
fi

if [[ ! "$PACKAGE_VERSION" =~ ^${ZENOH_VERSION//./\\.}\.[0-9]+$ ]]; then
  fail "Version '$PACKAGE_VERSION' must start with ZenohVersion '$ZENOH_VERSION.' (e.g. ${ZENOH_VERSION}.0)"
fi

CARGO_ZENOH="$(grep -E '^\s*zenoh\s*=' "$CARGO_TOML" | sed -E 's/.*version = "=([^"]+)".*/\1/')"
[[ "$CARGO_ZENOH" == "$ZENOH_VERSION" ]] || fail "Cargo.toml zenoh ($CARGO_ZENOH) != ZenohVersion ($ZENOH_VERSION)"

read_json_version() {
  python3 -c "import json, sys; print(json.load(open(sys.argv[1]))['version'])" "$1"
}

UPM_VERSION="$(read_json_version "$PACKAGE_JSON")"
EXPECTED_UPM_VERSION="$("$ROOT/scripts/upm-version.sh" "$PACKAGE_VERSION")"
[[ "$UPM_VERSION" == "$EXPECTED_UPM_VERSION" ]] || fail "package.json ($UPM_VERSION) != expected UPM version ($EXPECTED_UPM_VERSION) from Version ($PACKAGE_VERSION)"

for notice in "$ROOT/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Native/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Client/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity/THIRD_PARTY_NOTICES.md"; do
  [[ -f "$notice" ]] || fail "missing $notice"
  grep -q "release \`${ZENOH_VERSION}\`" "$notice" || fail "$notice does not mention Zenoh release ${ZENOH_VERSION}"
done

for csproj in "$ROOT"/src/*/*.csproj; do
  if grep -q '<Version>' "$csproj"; then
    fail "per-project <Version> override found in $csproj (use build/Versions.props)"
  fi
done

echo "verify-versions: OK (Zenoh ${ZENOH_VERSION}, packages ${PACKAGE_VERSION})"
