#!/bin/bash
# Create UPM package for Unity

set -e

echo "======================================"
echo "Creating UPM Package"
echo "======================================"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
UPM_DIR="$ROOT_DIR/packages/upm/com.zenoh.unity"

# Clean previous package
rm -rf "$UPM_DIR"
mkdir -p "$UPM_DIR"

echo ""
echo "Creating UPM package structure..."

# Copy package.json
cp "$ROOT_DIR/src/Zenoh.Unity/package.json" "$UPM_DIR/"

# Create Runtime directory and copy Runtime files
mkdir -p "$UPM_DIR/Runtime"
cp "$ROOT_DIR"/src/Zenoh.Unity/Runtime/*.cs "$UPM_DIR/Runtime/"

# Create Editor directory and copy Editor files
mkdir -p "$UPM_DIR/Editor"
cp "$ROOT_DIR"/src/Zenoh.Unity/Editor/*.cs "$UPM_DIR/Editor/"

# Create assembly definition files
echo "Creating assembly definitions..."

# Runtime assembly definition
cat > "$UPM_DIR/Runtime/Zenoh.Unity.asmdef" << 'EOF'
{
  "name": "Zenoh.Unity",
  "rootNamespace": "Zenoh.Unity",
  "references": [
    "UniTask"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
EOF

# Editor assembly definition
cat > "$UPM_DIR/Editor/Zenoh.Unity.Editor.asmdef" << 'EOF'
{
  "name": "Zenoh.Unity.Editor",
  "rootNamespace": "Zenoh.Unity.Editor",
  "references": [
    "Zenoh.Unity"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
EOF

# Copy README and documentation
cp "$ROOT_DIR/src/Zenoh.Unity/README.md" "$UPM_DIR/"
cp "$ROOT_DIR/LICENSE" "$UPM_DIR/LICENSE.md"

# Create CHANGELOG
cat > "$UPM_DIR/CHANGELOG.md" << 'EOF'
# Changelog

All notable changes to this package will be documented in this file.

## [0.1.0] - 2026-01-30

### Added
- Initial release
- Basic Session, Publisher, Subscriber support
- UniTask integration
- Cross-platform support (Windows, Linux, macOS)
- Unity Editor extensions
EOF

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
echo '  "com.zenoh.unity": "file:'"$UPM_DIR"'"'
