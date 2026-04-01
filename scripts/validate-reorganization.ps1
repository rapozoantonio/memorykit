# Reorganization Validation Report
# Generated: 2026-04-01

Write-Host "=== MemoryKit Root Reorganization Validation ===" -ForegroundColor Cyan
Write-Host ""

$rootDir = "c:\Users\rapoz\Documents\web-dev\memorykit"
Set-Location $rootDir

$passed = 0
$failed = 0
$warnings = 0

# Test 1: Verify new file locations exist
Write-Host "[Test 1] Checking files moved to correct locations..." -ForegroundColor Yellow
$expectedFiles = @(
    "scripts/test-persistence.ps1",
    "scripts/test-semantic.ps1",
    "scripts/docker-test.ps1",
    "scripts/git-branch-commands.sh",
    "scripts/query-facts.sql",
    "docs/DEVELOPMENT_GUIDE.md",
    "docs/DOCKER_SETUP.md",
    "docs/QUICKSTART.md",
    "docs/TESTING.md",
    "SECRETS/PROJECT_STATUS.md",
    "SECRETS/IMPLEMENTATION_SUCCESS.md",
    ".claude/mcp-config.json"
)

foreach ($file in $expectedFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  ✗ MISSING: $file" -ForegroundColor Red
        $failed++
    }
}

# Test 2: Verify old files removed from root
Write-Host "`n[Test 2] Checking old files removed from root..." -ForegroundColor Yellow
$oldFiles = @(
    "test-persistence.ps1",
    "test-semantic.ps1",
    "docker-test.ps1",
    "git-branch-commands.sh",
    "query-facts.sql",
    "IMPLEMENTATION_SUCCESS.md",
    "PROJECT_STATUS.md",
    "DEVELOPMENT_GUIDE.md",
    "DOCKER_SETUP.md",
    "QUICKSTART.md",
    "PERSISTENCE_TESTING_GUIDE.md",
    "claude-mcp-config.json"
)

foreach ($file in $oldFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "  ✓ Removed: $file" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  ✗ STILL EXISTS: $file" -ForegroundColor Red
        $failed++
    }
}

# Test 3: Check for broken documentation links
Write-Host "`n[Test 3] Validating documentation links..." -ForegroundColor Yellow
$docFiles = @("README.md", "CONTRIBUTING.md", "CHANGELOG.md", "docs/README.md")
$brokenLinks = @()

foreach ($docFile in $docFiles) {
    if (Test-Path $docFile) {
        $content = Get-Content $docFile -Raw
        $links = [regex]::Matches($content, '\[([^\]]+)\]\((?!http|#|mailto:)([^)]+\.md)\)')
        
        foreach ($match in $links) {
            $linkPath = $match.Groups[2].Value
            $basePath = Split-Path $docFile -Parent
            if ($basePath) {
                $fullPath = Join-Path $basePath $linkPath
            } else {
                $fullPath = $linkPath
            }
            
            # Normalize path
            $fullPath = $fullPath -replace '\\', '/'
            $fullPath = [System.IO.Path]::GetFullPath((Join-Path $rootDir $fullPath))
            
            if (-not (Test-Path $fullPath)) {
                Write-Host "  ⚠ Broken link in $docFile : $linkPath" -ForegroundColor Yellow
                $brokenLinks += "$docFile -> $linkPath"
                $warnings++
            }
        }
    }
}

if ($brokenLinks.Count -eq 0) {
    Write-Host "  ✓ All documentation links valid" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  Found $($brokenLinks.Count) broken links" -ForegroundColor Yellow
}

# Test 4: Verify root directory is clean
Write-Host "`n[Test 4] Checking root directory cleanliness..." -ForegroundColor Yellow
$rootFiles = Get-ChildItem -Path . -File | Where-Object { $_.Extension -in @('.md', '.ps1', '.sh', '.sql', '.json') }
$allowedRootFiles = @('README.md', 'CONTRIBUTING.md', 'CHANGELOG.md', 'LICENSE')

$unexpectedFiles = $rootFiles | Where-Object { $_.Name -notin $allowedRootFiles -and $_.Name -notin $oldFiles }
if ($unexpectedFiles.Count -eq 0) {
    Write-Host "  ✓ Root directory is clean" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  ⚠ Unexpected files in root:" -ForegroundColor Yellow
    foreach ($file in $unexpectedFiles) {
        Write-Host "    - $($file.Name)" -ForegroundColor Gray
    }
    $warnings++
}

# Test 5: Build test
Write-Host "`n[Test 5] Testing build..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Build successful" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  ✗ Build failed" -ForegroundColor Red
        $failed++
    }
} catch {
    Write-Host "  ⚠ Could not test build: $_" -ForegroundColor Yellow
    $warnings++
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "  Passed:   $passed" -ForegroundColor Green
Write-Host "  Failed:   $failed" -ForegroundColor Red
Write-Host "  Warnings: $warnings" -ForegroundColor Yellow
Write-Host ""

if ($failed -eq 0 -and $warnings -eq 0) {
    Write-Host "✅ All tests passed! Reorganization complete." -ForegroundColor Green
    exit 0
} elseif ($failed -eq 0) {
    Write-Host "⚠️  Tests passed with warnings. Review issues above." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "❌ Some tests failed. Please fix issues above." -ForegroundColor Red
    exit 1
}
