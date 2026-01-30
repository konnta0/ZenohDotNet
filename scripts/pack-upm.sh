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

# Copy package.json
cp "$UPM_SRC/package.json" "$UPM_DIR/"

# Copy Runtime files
cp -R "$UPM_SRC/Runtime" "$UPM_DIR/"

# Copy Editor files
cp -R "$UPM_SRC/Editor" "$UPM_DIR/"

# Copy Tests (for package testables)
cp -R "$UPM_SRC/Tests" "$UPM_DIR/"

# Copy README and documentation
cp "$UPM_SRC/README.md" "$UPM_DIR/"
cp "$UPM_SRC/THIRD_PARTY_NOTICES.md" "$UPM_DIR/"
cp "$UPM_SRC/CHANGELOG.md" "$UPM_DIR/"
cp "$UPM_SRC/LICENSE.md" "$UPM_DIR/LICENSE.md"

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
