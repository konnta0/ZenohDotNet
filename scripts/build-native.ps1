# PowerShell script to build Rust FFI for all target platforms

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Building Zenoh FFI for all platforms" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$nativeDir = Join-Path $scriptDir ".." "native" "zenoh-ffi"
Set-Location $nativeDir

# Ensure cargo is available
if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Host "Error: cargo not found. Please install Rust: https://rustup.rs/" -ForegroundColor Red
    exit 1
}

# Check for cross (optional)
$crossAvailable = $null -ne (Get-Command cross -ErrorAction SilentlyContinue)
if (-not $crossAvailable) {
    Write-Host "Warning: 'cross' not found. Install with: cargo install cross" -ForegroundColor Yellow
    Write-Host "Will try using regular cargo for native target only..." -ForegroundColor Yellow
}

# Build for native target first
Write-Host "`nBuilding for native target..." -ForegroundColor Green
cargo build --release

# Detect native target
$nativeTarget = (rustc -vV | Select-String "host:" | ForEach-Object { $_.Line.Split(":")[1].Trim() })
Write-Host "Detected native target: $nativeTarget" -ForegroundColor Cyan

# Copy native build to output
$outputBaseDir = Join-Path $nativeDir ".." "output"

if ($nativeTarget -like "*windows*") {
    if ($nativeTarget -like "*aarch64*") {
        $rid = "win-arm64"
        $libName = "zenoh_ffi.dll"
    } else {
        $rid = "win-x64"
        $libName = "zenoh_ffi.dll"
    }

    $outputDir = Join-Path $outputBaseDir $rid
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    Copy-Item "target\release\$libName" $outputDir -Force
    Write-Host "Copied to $outputDir\" -ForegroundColor Green
}

# Cross-compilation (if cross is available)
if ($crossAvailable) {
    Write-Host "`nCross-compiling for other platforms..." -ForegroundColor Green

    # Define all Windows targets (Linux/macOS should be built on their respective platforms)
    $targets = @(
        @{Target="x86_64-pc-windows-msvc"; Rid="win-x64"; LibName="zenoh_ffi.dll"},
        @{Target="aarch64-pc-windows-msvc"; Rid="win-arm64"; LibName="zenoh_ffi.dll"}
    )

    foreach ($targetInfo in $targets) {
        $target = $targetInfo.Target
        $rid = $targetInfo.Rid
        $libName = $targetInfo.LibName

        # Skip if this is the native target (already built)
        if ($target -eq $nativeTarget) {
            continue
        }

        Write-Host "`nBuilding for $target..." -ForegroundColor Green

        # Check if target is installed
        $targetInstalled = rustup target list | Select-String "$target \(installed\)"
        if (-not $targetInstalled) {
            Write-Host "Installing target $target..." -ForegroundColor Yellow
            rustup target add $target
        }

        # Try building
        try {
            cross build --release --target $target 2>&1 | Out-Null

            $outputDir = Join-Path $outputBaseDir $rid
            New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
            Copy-Item "target\$target\release\$libName" $outputDir -Force -ErrorAction SilentlyContinue
            Write-Host "Successfully built and copied to $outputDir\" -ForegroundColor Green
        } catch {
            Write-Host "Warning: Failed to build for $target (skipping)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "`nSkipping cross-compilation (cross not available)" -ForegroundColor Yellow
    Write-Host "Only native target has been built" -ForegroundColor Yellow
}

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Build complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: native\output\" -ForegroundColor White
Write-Host "Generated C# bindings: native\output\generated\" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Run: .\scripts\copy-bindings.ps1" -ForegroundColor White
Write-Host "  2. Build C# projects: dotnet build ZenohForCSharp.sln" -ForegroundColor White
