#!/bin/bash
# Generate Unity .meta files for native plugins

set -e

generate_plugin_meta_windows() {
    local file="$1"
    local guid=$(echo -n "$file" | md5sum | cut -d' ' -f1)
    
    cat > "${file}.meta" << METAEOF
fileFormatVersion: 2
guid: ${guid}
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        DefaultValueInitialized: true
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData: 
  assetBundleName: 
  assetBundleVariant: 
METAEOF
}

generate_plugin_meta_linux() {
    local file="$1"
    local guid=$(echo -n "$file" | md5sum | cut -d' ' -f1)
    
    cat > "${file}.meta" << METAEOF
fileFormatVersion: 2
guid: ${guid}
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        DefaultValueInitialized: true
  - first:
      Standalone: Linux64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData: 
  assetBundleName: 
  assetBundleVariant: 
METAEOF
}

generate_plugin_meta_macos() {
    local file="$1"
    local guid=$(echo -n "$file" | md5sum | cut -d' ' -f1)
    
    cat > "${file}.meta" << METAEOF
fileFormatVersion: 2
guid: ${guid}
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        DefaultValueInitialized: true
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
  userData: 
  assetBundleName: 
  assetBundleVariant: 
METAEOF
}

generate_folder_meta() {
    local folder="$1"
    local guid=$(echo -n "$folder" | md5sum | cut -d' ' -f1)
    
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

UNITY_PKG="$1"
NATIVE_LIBS="$2"

if [ -z "$UNITY_PKG" ] || [ -z "$NATIVE_LIBS" ]; then
    echo "Usage: $0 <unity_pkg_path> <native_libs_path>"
    exit 1
fi

# Create Plugins folder structure with meta files
mkdir -p "$UNITY_PKG/Plugins"
generate_folder_meta "$UNITY_PKG/Plugins"

# Windows x64
if [ -d "$NATIVE_LIBS/unity-native-win-x64" ]; then
    mkdir -p "$UNITY_PKG/Plugins/Windows/x86_64"
    generate_folder_meta "$UNITY_PKG/Plugins/Windows"
    generate_folder_meta "$UNITY_PKG/Plugins/Windows/x86_64"
    cp "$NATIVE_LIBS/unity-native-win-x64"/*.dll "$UNITY_PKG/Plugins/Windows/x86_64/"
    for f in "$UNITY_PKG/Plugins/Windows/x86_64"/*.dll; do
        generate_plugin_meta_windows "$f"
    done
    echo "Copied Windows x64 native library"
fi

# Linux x64
if [ -d "$NATIVE_LIBS/unity-native-linux-x64" ]; then
    mkdir -p "$UNITY_PKG/Plugins/Linux/x86_64"
    generate_folder_meta "$UNITY_PKG/Plugins/Linux"
    generate_folder_meta "$UNITY_PKG/Plugins/Linux/x86_64"
    # Rename libzenoh_ffi.so to zenoh_ffi.so (Unity expects no lib prefix)
    for src in "$NATIVE_LIBS/unity-native-linux-x64"/*.so; do
        dst="$UNITY_PKG/Plugins/Linux/x86_64/$(basename "$src" | sed 's/^lib//')"
        cp "$src" "$dst"
    done
    for f in "$UNITY_PKG/Plugins/Linux/x86_64"/*.so; do
        generate_plugin_meta_linux "$f"
    done
    echo "Copied Linux x64 native library"
fi

# macOS (Universal - supports both x64 and ARM64)
if [ -d "$NATIVE_LIBS/unity-native-osx-x64" ] || [ -d "$NATIVE_LIBS/unity-native-osx-arm64" ]; then
    mkdir -p "$UNITY_PKG/Plugins/macOS"
    generate_folder_meta "$UNITY_PKG/Plugins/macOS"
    
    # Prefer ARM64, fallback to x64
    # Note: rename libzenoh_ffi.dylib to zenoh_ffi.dylib (Unity expects no lib prefix)
    if [ -d "$NATIVE_LIBS/unity-native-osx-arm64" ]; then
        for src in "$NATIVE_LIBS/unity-native-osx-arm64"/*.dylib; do
            dst="$UNITY_PKG/Plugins/macOS/$(basename "$src" | sed 's/^lib//')"
            cp "$src" "$dst"
        done
        echo "Copied macOS ARM64 native library"
    elif [ -d "$NATIVE_LIBS/unity-native-osx-x64" ]; then
        for src in "$NATIVE_LIBS/unity-native-osx-x64"/*.dylib; do
            dst="$UNITY_PKG/Plugins/macOS/$(basename "$src" | sed 's/^lib//')"
            cp "$src" "$dst"
        done
        echo "Copied macOS x64 native library"
    fi
    
    for f in "$UNITY_PKG/Plugins/macOS"/*.dylib; do
        if [ -f "$f" ]; then
            generate_plugin_meta_macos "$f"
        fi
    done
fi

# List what we have
echo "=== Native libraries in Unity package ==="
find "$UNITY_PKG/Plugins" -type f 2>/dev/null || echo "No plugins found"

echo "=== Meta file content sample (macOS) ==="
cat "$UNITY_PKG/Plugins/macOS/zenoh_ffi.dylib.meta" 2>/dev/null || echo "No macOS meta"
