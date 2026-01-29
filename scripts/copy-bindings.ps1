# PowerShell script to copy generated C# bindings and native libraries

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Copying Bindings and Native Libraries" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

# Check if generated bindings exist
$generatedCs = Join-Path $rootDir "native" "output" "generated" "NativeMethods.g.cs"
if (-not (Test-Path $generatedCs)) {
    Write-Host "Error: Generated C# bindings not found at $generatedCs" -ForegroundColor Red
    Write-Host "Please run '.\scripts\build-native.ps1' first to generate bindings." -ForegroundColor Red
    exit 1
}

# Copy generated C# code to Zenoh.Native
Write-Host "`nCopying generated C# bindings..." -ForegroundColor Green
$destCs = Join-Path $rootDir "src" "Zenoh.Native" "NativeMethods.cs"
Copy-Item $generatedCs $destCs -Force
Write-Host "✓ Copied NativeMethods.cs to src\Zenoh.Native\" -ForegroundColor Green

# Copy native libraries to runtimes directories
Write-Host "`nCopying native libraries..." -ForegroundColor Green

# Define source and destination mappings
$libMap = @{
    "native\output\win-x64" = "src\Zenoh.Native\runtimes\win-x64\native"
    "native\output\win-arm64" = "src\Zenoh.Native\runtimes\win-arm64\native"
    "native\output\linux-x64" = "src\Zenoh.Native\runtimes\linux-x64\native"
    "native\output\linux-arm64" = "src\Zenoh.Native\runtimes\linux-arm64\native"
    "native\output\osx-x64" = "src\Zenoh.Native\runtimes\osx-x64\native"
    "native\output\osx-arm64" = "src\Zenoh.Native\runtimes\osx-arm64\native"
}

foreach ($srcRel in $libMap.Keys) {
    $destRel = $libMap[$srcRel]
    $srcPath = Join-Path $rootDir $srcRel
    $destPath = Join-Path $rootDir $destRel

    if ((Test-Path $srcPath) -and (Get-ChildItem $srcPath -File).Count -gt 0) {
        # Source directory exists and is not empty
        New-Item -ItemType Directory -Force -Path $destPath | Out-Null

        try {
            Copy-Item "$srcPath\*" $destPath -Force

            if ((Get-ChildItem $destPath -File).Count -gt 0) {
                Write-Host "✓ Copied libraries from $srcRel to $destRel" -ForegroundColor Green
            } else {
                Write-Host "⚠ Warning: No files copied from $srcRel to $destRel" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "⚠ Warning: Failed to copy from $srcRel to $destRel" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠ Skipping $srcRel (directory not found or empty)" -ForegroundColor Yellow
    }
}

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Copy complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next step:" -ForegroundColor White
Write-Host "  Build C# projects: dotnet build ZenohForCSharp.sln" -ForegroundColor White
