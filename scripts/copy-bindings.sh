#!/bin/bash
# Copy generated C# bindings and native libraries to the appropriate locations

set -e

echo "======================================"
echo "Copying Bindings and Native Libraries"
echo "======================================"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Check if generated bindings exist
GENERATED_CS="$ROOT_DIR/native/output/generated/NativeMethods.g.cs"
if [ ! -f "$GENERATED_CS" ]; then
    echo "Error: Generated C# bindings not found at $GENERATED_CS"
    echo "Please run './scripts/build-native.sh' first to generate bindings."
    exit 1
fi

# Copy generated C# code to ZenohDotNet.Native
echo ""
echo "Copying generated C# bindings..."
cp "$GENERATED_CS" "$ROOT_DIR/src/ZenohDotNet.Native/NativeMethods.g.cs"
echo "✓ Copied NativeMethods.g.cs to src/ZenohDotNet.Native/"

# Copy native libraries to runtimes directories
echo ""
echo "Copying native libraries..."

# Define source and destination mappings
declare -A LIB_MAP=(
    ["native/output/win-x64"]="src/ZenohDotNet.Native/runtimes/win-x64/native"
    ["native/output/win-arm64"]="src/ZenohDotNet.Native/runtimes/win-arm64/native"
    ["native/output/linux-x64"]="src/ZenohDotNet.Native/runtimes/linux-x64/native"
    ["native/output/linux-arm64"]="src/ZenohDotNet.Native/runtimes/linux-arm64/native"
    ["native/output/osx-x64"]="src/ZenohDotNet.Native/runtimes/osx-x64/native"
    ["native/output/osx-arm64"]="src/ZenohDotNet.Native/runtimes/osx-arm64/native"
)

for src in "${!LIB_MAP[@]}"; do
    dest="${LIB_MAP[$src]}"
    src_path="$ROOT_DIR/$src"
    dest_path="$ROOT_DIR/$dest"

    if [ -d "$src_path" ] && [ "$(ls -A "$src_path")" ]; then
        # Source directory exists and is not empty
        mkdir -p "$dest_path"
        cp "$src_path"/* "$dest_path/" 2>/dev/null || true

        # Check if copy was successful
        if [ "$(ls -A "$dest_path")" ]; then
            echo "✓ Copied libraries from $src to $dest"
        else
            echo "⚠ Warning: No files copied from $src to $dest"
        fi
    else
        echo "⚠ Skipping $src (directory not found or empty)"
    fi
done

# Copy Android libraries to Unity package
echo ""
echo "Copying Android libraries to Unity package..."

UNITY_ANDROID_DIR="$ROOT_DIR/packages/com.zenohdotnet.native/Plugins/Android"

declare -A ANDROID_MAP=(
    ["native/output/android-arm64"]="$UNITY_ANDROID_DIR/arm64-v8a"
    ["native/output/android-arm"]="$UNITY_ANDROID_DIR/armeabi-v7a"
    ["native/output/android-x86_64"]="$UNITY_ANDROID_DIR/x86_64"
)

for src in "${!ANDROID_MAP[@]}"; do
    dest="${ANDROID_MAP[$src]}"
    src_path="$ROOT_DIR/$src"
    
    if [ -d "$src_path" ] && [ "$(ls -A "$src_path")" ]; then
        mkdir -p "$dest"
        cp "$src_path"/*.so "$dest/" 2>/dev/null || true
        echo "✓ Copied Android library from $src to $dest"
    else
        echo "⚠ Skipping $src (directory not found or empty)"
    fi
done

# Copy iOS library to Unity package
echo ""
echo "Copying iOS libraries to Unity package..."

UNITY_IOS_DIR="$ROOT_DIR/packages/com.zenohdotnet.native/Plugins/iOS"

# Prefer the device build for iOS
IOS_SRC="$ROOT_DIR/native/output/ios-arm64"
if [ -d "$IOS_SRC" ] && [ "$(ls -A "$IOS_SRC")" ]; then
    mkdir -p "$UNITY_IOS_DIR"
    cp "$IOS_SRC"/*.a "$UNITY_IOS_DIR/" 2>/dev/null || true
    echo "✓ Copied iOS library from ios-arm64 to $UNITY_IOS_DIR"
else
    echo "⚠ Skipping iOS (directory not found or empty)"
fi

# Copy Source Generator DLLs to Unity package
echo ""
echo "Copying Source Generator DLLs to Unity package..."

UNITY_EDITOR_DIR="$ROOT_DIR/packages/com.zenohdotnet.native/Editor"
UNITY_RUNTIME_DIR="$ROOT_DIR/packages/com.zenohdotnet.native/Runtime"

# Build Generator and Abstractions first
echo "Building Generator and Abstractions..."
dotnet build "$ROOT_DIR/src/ZenohDotNet.Generator" -c Release --verbosity quiet 2>/dev/null || {
    echo "⚠ Generator build failed, skipping DLL copy"
}

GENERATOR_DLL="$ROOT_DIR/src/ZenohDotNet.Generator/bin/Release/netstandard2.0/ZenohDotNet.Generator.dll"
ABSTRACTIONS_DLL="$ROOT_DIR/src/ZenohDotNet.Abstractions/bin/Release/netstandard2.0/ZenohDotNet.Abstractions.dll"

if [ -f "$GENERATOR_DLL" ]; then
    mkdir -p "$UNITY_EDITOR_DIR"
    cp "$GENERATOR_DLL" "$UNITY_EDITOR_DIR/"
    echo "✓ Copied ZenohDotNet.Generator.dll to Unity Editor folder"
else
    echo "⚠ Generator DLL not found at $GENERATOR_DLL"
fi

if [ -f "$ABSTRACTIONS_DLL" ]; then
    mkdir -p "$UNITY_EDITOR_DIR"
    mkdir -p "$UNITY_RUNTIME_DIR"
    cp "$ABSTRACTIONS_DLL" "$UNITY_EDITOR_DIR/"
    cp "$ABSTRACTIONS_DLL" "$UNITY_RUNTIME_DIR/"
    echo "✓ Copied ZenohDotNet.Abstractions.dll to Unity Editor and Runtime folders"
else
    echo "⚠ Abstractions DLL not found at $ABSTRACTIONS_DLL"
fi

echo ""
echo "======================================"
echo "Copy complete!"
echo "======================================"
echo ""
echo "Next step:"
echo "  Build C# projects: dotnet build ZenohDotNet.slnx"
