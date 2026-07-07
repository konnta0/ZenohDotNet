#!/usr/bin/env bash
# Bundle ZenohDotNet.Native as C# source into a Unity UPM package.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UNITY_PKG="${1:?unity package path}"

generate_cs_meta() {
  local file="$1"
  local guid
  guid=$(echo -n "$file" | md5sum | cut -d' ' -f1)

  cat > "${file}.meta" << METAEOF
fileFormatVersion: 2
guid: ${guid}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
METAEOF
}

generate_folder_meta() {
  local folder="$1"
  local guid
  guid=$(echo -n "$folder" | md5sum | cut -d' ' -f1)

  cat > "${folder}.meta" << METAEOF
fileFormatVersion: 2
guid: ${guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
METAEOF
}

rm -f "$UNITY_PKG/Runtime/ZenohDotNet.Native.dll" "$UNITY_PKG/Runtime/ZenohDotNet.Native.dll.meta"
mkdir -p "$UNITY_PKG/Runtime/Native"
cp "$ROOT_DIR/src/ZenohDotNet.Native"/*.cs "$UNITY_PKG/Runtime/Native/"
if [ ! -f "$UNITY_PKG/Runtime/Native.meta" ]; then
  generate_folder_meta "$UNITY_PKG/Runtime/Native"
fi
for f in "$UNITY_PKG/Runtime/Native"/*.cs; do
  if [ ! -f "${f}.meta" ]; then
    generate_cs_meta "$f"
  fi
done

echo "Bundled ZenohDotNet.Native source into $UNITY_PKG/Runtime/Native/"
