#!/usr/bin/env bash
# Convert NuGet four-segment version to Unity Package Manager SemVer.
# Example: 1.9.0.0 -> 1.9.0-native.0

set -euo pipefail

to_upm_version() {
  local v="$1"
  if [[ "$v" =~ ^([0-9]+\.[0-9]+\.[0-9]+)\.([0-9]+)$ ]]; then
    echo "${BASH_REMATCH[1]}-native.${BASH_REMATCH[2]}"
  else
    echo "upm-version: expected four numeric segments (e.g. 1.9.0.0), got '$v'" >&2
    return 1
  fi
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  to_upm_version "${1:?version}"
fi
