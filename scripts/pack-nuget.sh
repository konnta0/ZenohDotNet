#!/bin/bash
# Pack NuGet packages for all projects

set -e

echo "======================================"
echo "Packing NuGet Packages"
echo "======================================"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$ROOT_DIR/packages/nuget"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Clean previous packages
rm -f "$OUTPUT_DIR"/*.nupkg
rm -f "$OUTPUT_DIR"/*.snupkg

echo ""
echo "Building and packing projects..."

# Pack ZenohDotNet.Native
echo ""
echo "Packing ZenohDotNet.Native..."
dotnet pack "$ROOT_DIR/src/ZenohDotNet.Native/ZenohDotNet.Native.csproj" \
    -c Release \
    -o "$OUTPUT_DIR" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg

# Pack ZenohDotNet.Client
echo ""
echo "Packing ZenohDotNet.Client..."
dotnet pack "$ROOT_DIR/src/ZenohDotNet.Client/ZenohDotNet.Client.csproj" \
    -c Release \
    -o "$OUTPUT_DIR" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg

# Pack ZenohDotNet.Unity
echo ""
echo "Packing ZenohDotNet.Unity..."
dotnet pack "$ROOT_DIR/src/ZenohDotNet.Unity/ZenohDotNet.Unity.csproj" \
    -c Release \
    -o "$OUTPUT_DIR" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg

echo ""
echo "======================================"
echo "Packing complete!"
echo "======================================"
echo ""
echo "Packages created in: $OUTPUT_DIR"
ls -lh "$OUTPUT_DIR"/*.nupkg

echo ""
echo "To publish to NuGet.org:"
echo "  dotnet nuget push $OUTPUT_DIR/*.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY"
