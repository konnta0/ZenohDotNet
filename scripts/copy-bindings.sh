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

# Copy generated C# code to Zenoh.Native
echo ""
echo "Copying generated C# bindings..."
cp "$GENERATED_CS" "$ROOT_DIR/src/Zenoh.Native/NativeMethods.cs"
echo "✓ Copied NativeMethods.cs to src/Zenoh.Native/"

# Copy native libraries to runtimes directories
echo ""
echo "Copying native libraries..."

# Define source and destination mappings
declare -A LIB_MAP=(
    ["native/output/win-x64"]="src/Zenoh.Native/runtimes/win-x64/native"
    ["native/output/win-arm64"]="src/Zenoh.Native/runtimes/win-arm64/native"
    ["native/output/linux-x64"]="src/Zenoh.Native/runtimes/linux-x64/native"
    ["native/output/linux-arm64"]="src/Zenoh.Native/runtimes/linux-arm64/native"
    ["native/output/osx-x64"]="src/Zenoh.Native/runtimes/osx-x64/native"
    ["native/output/osx-arm64"]="src/Zenoh.Native/runtimes/osx-arm64/native"
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

echo ""
echo "======================================"
echo "Copy complete!"
echo "======================================"
echo ""
echo "Next step:"
echo "  Build C# projects: dotnet build ZenohForCSharp.sln"
