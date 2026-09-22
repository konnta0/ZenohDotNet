#!/bin/bash
# Create UPM package for Unity

set -e

echo "======================================"
echo "Creating UPM Package"
echo "======================================"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UPM_DIR="$ROOT_DIR/packages/upm/com.zenohdotnet.unity"
UPM_SRC="$ROOT_DIR/src/ZenohDotNet.Unity/Assets/Plugins/com.zenohdotnet.unity"

# Clean previous package
rm -rf "$UPM_DIR"
mkdir -p "$UPM_DIR"

echo ""
echo "Creating UPM package structure..."

# Copy package.json and its Unity metadata
cp "$UPM_SRC/package.json" "$UPM_SRC/package.json.meta" "$UPM_DIR/"

# Copy Runtime files
cp -R "$UPM_SRC/Runtime" "$UPM_DIR/"
cp "$UPM_SRC/Runtime.meta" "$UPM_DIR/"

chmod +x "$SCRIPT_DIR/bundle-upm-native-source.sh"
"$SCRIPT_DIR/bundle-upm-native-source.sh" "$UPM_DIR"

# Copy Editor files
cp -R "$UPM_SRC/Editor" "$UPM_DIR/"
cp "$UPM_SRC/Editor.meta" "$UPM_DIR/"

# Copy Tests (for package testables)
cp -R "$UPM_SRC/Tests" "$UPM_DIR/"
cp "$UPM_SRC/Tests.meta" "$UPM_DIR/"

# Copy README and documentation
cp "$UPM_SRC/README.md" "$UPM_SRC/README.md.meta" "$UPM_DIR/"
cp "$UPM_SRC/THIRD_PARTY_NOTICES.md" "$UPM_SRC/THIRD_PARTY_NOTICES.md.meta" "$UPM_DIR/"
cp "$UPM_SRC/CHANGELOG.md" "$UPM_SRC/CHANGELOG.md.meta" "$UPM_DIR/"
cp "$UPM_SRC/LICENSE.md" "$UPM_SRC/LICENSE.md.meta" "$UPM_DIR/"

# A package must never ship assets without their Unity metadata. Missing metadata
# makes Unity generate consumer-local GUIDs, which makes imports non-reproducible.
missing_meta=0
while IFS= read -r asset; do
  if [ ! -e "${asset}.meta" ]; then
    echo "ERROR: missing Unity metadata: ${asset}.meta" >&2
    missing_meta=1
  fi
done < <(find "$UPM_DIR" -mindepth 1 ! -name '*.meta' -print)

if [ "$missing_meta" -ne 0 ]; then
  exit 1
fi

echo ""
echo "======================================"
echo "UPM Package created!"
echo "======================================"
echo ""
echo "Package location: $UPM_DIR"
echo ""
echo "To install in Unity:"
echo "  1. Open Unity Package Manager"
echo "  2. Click '+' → 'Add package from disk...'"
echo "  3. Select: $UPM_DIR/package.json"
echo ""
echo "Or add to manifest.json:"
echo '  "com.zenohdotnet.unity": "file:'"$UPM_DIR"'"'
