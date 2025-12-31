#!/usr/bin/env pwsh
# Build MemoryKit API executables for MCP deployment

$ErrorActionPreference = "Stop"

Write-Host "Building MemoryKit API executables for MCP deployment..." -ForegroundColor Cyan

$OutputDir = "./dist/executables"
$Project = "./src/MemoryKit.API/MemoryKit.API.csproj"

# Clean output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build for each platform
$Platforms = @("linux-x64", "osx-x64", "osx-arm64", "win-x64")

foreach ($Platform in $Platforms) {
    Write-Host "`nBuilding for $Platform..." -ForegroundColor Green
    
    dotnet publish $Project `
        -c Release `
        -r $Platform `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:PublishReadyToRun=true `
        -o "$OutputDir/$Platform"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $Platform"
        exit 1
    }
    
    # Rename executable to memorykit-api
    $ExeName = if ($Platform -eq "win-x64") { "MemoryKit.API.exe" } else { "MemoryKit.API" }
    $NewName = if ($Platform -eq "win-x64") { "memorykit-api.exe" } else { "memorykit-api" }
    $PlatformDir = Join-Path $OutputDir $Platform
    $ExePath = Join-Path $PlatformDir $ExeName
    $NewPath = Join-Path $PlatformDir $NewName
    
    if (Test-Path $ExePath) {
        Move-Item -Path $ExePath -Destination $NewPath -Force
        
        # Show file size
        $FileInfo = Get-Item $NewPath
        $SizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
        Write-Host "  [OK] $NewName - $SizeMB MB" -ForegroundColor Green
    } else {
        Write-Warning "Executable not found at $ExePath"
    }
}

Write-Host "`n[OK] Build complete! Executables in $OutputDir" -ForegroundColor Cyan
Write-Host "`nPlatforms built:" -ForegroundColor White
foreach ($Platform in $Platforms) {
    $ExeName = if ($Platform -eq "win-x64") { "memorykit-api.exe" } else { "memorykit-api" }
    $PlatformDir = Join-Path $OutputDir $Platform
    $ExePath = Join-Path $PlatformDir $ExeName
    if (Test-Path $ExePath) {
        $FileInfo = Get-Item $ExePath
        $SizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
        Write-Host "  - $Platform`: $SizeMB MB" -ForegroundColor Gray
    }
}
