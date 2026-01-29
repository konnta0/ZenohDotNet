#!/bin/bash
# Build Rust FFI for all target platforms

set -e

echo "======================================"
echo "Building Zenoh FFI for all platforms"
echo "======================================"

cd "$(dirname "$0")/../native/zenoh-ffi"

# Ensure cargo is available
if ! command -v cargo &> /dev/null; then
    echo "Error: cargo not found. Please install Rust: https://rustup.rs/"
    exit 1
fi

# Ensure cross is available for cross-compilation (optional)
if ! command -v cross &> /dev/null; then
    echo "Warning: 'cross' not found. Install with: cargo install cross"
    echo "Will try using regular cargo for native target only..."
    CROSS_AVAILABLE=false
else
    CROSS_AVAILABLE=true
fi

# Build for native target first
echo ""
echo "Building for native target..."
cargo build --release

# Detect native target
NATIVE_TARGET=$(rustc -vV | grep host | awk '{print $2}')
echo "Detected native target: $NATIVE_TARGET"

# Copy native build to output
case "$NATIVE_TARGET" in
    *darwin*)
        if [[ "$NATIVE_TARGET" == *"aarch64"* ]]; then
            mkdir -p ../output/osx-arm64
            cp target/release/libzenoh_ffi.dylib ../output/osx-arm64/
            echo "Copied to ../output/osx-arm64/"
        else
            mkdir -p ../output/osx-x64
            cp target/release/libzenoh_ffi.dylib ../output/osx-x64/
            echo "Copied to ../output/osx-x64/"
        fi
        ;;
    *linux*)
        if [[ "$NATIVE_TARGET" == *"aarch64"* ]]; then
            mkdir -p ../output/linux-arm64
            cp target/release/libzenoh_ffi.so ../output/linux-arm64/
            echo "Copied to ../output/linux-arm64/"
        else
            mkdir -p ../output/linux-x64
            cp target/release/libzenoh_ffi.so ../output/linux-x64/
            echo "Copied to ../output/linux-x64/"
        fi
        ;;
    *windows*)
        if [[ "$NATIVE_TARGET" == *"aarch64"* ]]; then
            mkdir -p ../output/win-arm64
            cp target/release/zenoh_ffi.dll ../output/win-arm64/
            echo "Copied to ../output/win-arm64/"
        else
            mkdir -p ../output/win-x64
            cp target/release/zenoh_ffi.dll ../output/win-x64/
            echo "Copied to ../output/win-x64/"
        fi
        ;;
esac

# Cross-compilation (if cross is available)
if [ "$CROSS_AVAILABLE" = true ]; then
    echo ""
    echo "Cross-compiling for other platforms..."

    # Define all targets
    TARGETS=(
        "x86_64-pc-windows-msvc:win-x64:zenoh_ffi.dll"
        "aarch64-pc-windows-msvc:win-arm64:zenoh_ffi.dll"
        "x86_64-unknown-linux-gnu:linux-x64:libzenoh_ffi.so"
        "aarch64-unknown-linux-gnu:linux-arm64:libzenoh_ffi.so"
        "x86_64-apple-darwin:osx-x64:libzenoh_ffi.dylib"
        "aarch64-apple-darwin:osx-arm64:libzenoh_ffi.dylib"
    )

    for target_info in "${TARGETS[@]}"; do
        IFS=':' read -r target rid libname <<< "$target_info"

        # Skip if this is the native target (already built)
        if [ "$target" = "$NATIVE_TARGET" ]; then
            continue
        fi

        echo ""
        echo "Building for $target..."

        # Check if target is installed
        if ! rustup target list | grep -q "$target (installed)"; then
            echo "Installing target $target..."
            rustup target add "$target" || echo "Warning: Could not add target $target"
        fi

        # Try building
        if cross build --release --target "$target" 2>/dev/null; then
            mkdir -p "../output/$rid"
            cp "target/$target/release/$libname" "../output/$rid/" 2>/dev/null || true
            echo "Successfully built and copied to ../output/$rid/"
        else
            echo "Warning: Failed to build for $target (skipping)"
        fi
    done
else
    echo ""
    echo "Skipping cross-compilation (cross not available)"
    echo "Only native target has been built"
fi

echo ""
echo "======================================"
echo "Build complete!"
echo "======================================"
echo ""
echo "Output directory: native/output/"
echo "Generated C# bindings: native/output/generated/"
echo ""
echo "Next steps:"
echo "  1. Run: ./scripts/copy-bindings.sh"
echo "  2. Build C# projects: dotnet build ZenohForCSharp.sln"
