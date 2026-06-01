#!/usr/bin/env bash
# Bump Zenoh runtime and/or dotnet patch version across the repository.
#
# Examples:
#   ./scripts/bump-version.sh --zenoh 1.9.1 --patch 0   # -> 1.9.1.0
#   ./scripts/bump-version.sh --patch 1                   # -> 1.9.0.1 (increments dotnet patch)
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSIONS_PROPS="$ROOT/build/Versions.props"
CARGO_TOML="$ROOT/native/zenoh-ffi/Cargo.toml"
PACKAGE_JSON="$ROOT/src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity/package.json"

usage() {
  sed -n '2,6p' "$0"
  exit 1
}

read_prop() {
  local file="$1" name="$2"
  grep -E "<${name}>" "$file" | head -1 | sed -E "s/.*<${name}>([^<]+)<.*/\1/" | tr -d '[:space:]'
}

write_prop() {
  local file="$1" name="$2" value="$3"
  sed -i.bak -E "s|<${name}>[^<]+</${name}>|<${name}>${value}</${name}>|" "$file"
  rm -f "${file}.bak"
}

ZENOH=""
PATCH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --zenoh) ZENOH="$2"; shift 2 ;;
    --patch) PATCH="$2"; shift 2 ;;
    -h|--help) usage ;;
    *) echo "Unknown option: $1" >&2; usage ;;
  esac
done

CURRENT_ZENOH="$(read_prop "$VERSIONS_PROPS" ZenohVersion)"
CURRENT_VERSION="$(read_prop "$VERSIONS_PROPS" Version)"

if [[ -n "$ZENOH" ]]; then
  TARGET_ZENOH="$ZENOH"
else
  TARGET_ZENOH="$CURRENT_ZENOH"
fi

IFS='.' read -r v_major v_minor v_patch v_build <<< "$CURRENT_VERSION"
v_build="${v_build:-0}"

if [[ -n "$PATCH" ]]; then
  TARGET_BUILD="$PATCH"
elif [[ -n "$ZENOH" ]]; then
  TARGET_BUILD="0"
else
  echo "Specify --zenoh and/or --patch" >&2
  usage
fi

TARGET_VERSION="${TARGET_ZENOH}.${TARGET_BUILD}"

echo "Bumping: Zenoh ${CURRENT_ZENOH} -> ${TARGET_ZENOH}, packages ${CURRENT_VERSION} -> ${TARGET_VERSION}"

write_prop "$VERSIONS_PROPS" ZenohVersion "$TARGET_ZENOH"
write_prop "$VERSIONS_PROPS" Version "$TARGET_VERSION"

sed -i.bak -E "s|zenoh = \\{ version = \"=[^\"]+\",|zenoh = { version = \"=${TARGET_ZENOH}\",|" "$CARGO_TOML"
rm -f "${CARGO_TOML}.bak"

python3 - "$PACKAGE_JSON" "$TARGET_VERSION" <<'PY'
import json, sys
path, version = sys.argv[1], sys.argv[2]
with open(path, encoding="utf-8") as f:
    data = json.load(f)
data["version"] = version
with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
PY

for notice in "$ROOT/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Native/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Client/THIRD_PARTY_NOTICES.md" \
  "$ROOT/src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity/THIRD_PARTY_NOTICES.md"; do
  sed -i.bak -E "s|release \`[^`]+\`|release \`${TARGET_ZENOH}\`|" "$notice"
  rm -f "${notice}.bak"
done

if [[ -f "$ROOT/README.md" ]]; then
  sed -i.bak -E "s|badge/version-[^-]+-blue|badge/version-${TARGET_ZENOH}.x-blue|" "$ROOT/README.md"
  sed -i.bak -E "s|zenoh Rust \`[0-9.]+\`|zenoh Rust \`${TARGET_ZENOH}\`|g" "$ROOT/README.md"
  sed -i.bak -E "s|\| \*\*Package version\*\* \| \`[0-9.]+\.x\`.*|\| **Package version** | \`${TARGET_ZENOH}.x\` = Zenoh \`${TARGET_ZENOH}\` + dotnet release patch (see [Versioning](#versioning)) |" "$ROOT/README.md"
  rm -f "${ROOT}/README.md.bak"
fi

"$ROOT/scripts/verify-versions.sh"

echo "Done. Commit build/Versions.props and synced files, then tag: git tag v${TARGET_VERSION}"
