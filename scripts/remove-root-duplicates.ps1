# Remove duplicate files from root directory
# New versions already exist in proper subdirectories
# Created: 2026-04-01

$rootDir = "c:\Users\rapoz\Documents\web-dev\memorykit"
Set-Location $rootDir

Write-Host "🧹 Cleaning up root directory duplicates..." -ForegroundColor Cyan
Write-Host ""

$filesToRemove = @(
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

$removed = 0
foreach ($file in $filesToRemove) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "  ✓ Removed: $file" -ForegroundColor Green
        $removed++
    } else {
        Write-Host "  - Already gone: $file" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "✅ Cleanup complete! Removed $removed files" -ForegroundColor Green
Write-Host ""
Write-Host "Files now in correct locations:" -ForegroundColor Cyan
Write-Host "  scripts/    - test scripts and utilities" -ForegroundColor White
Write-Host "  docs/       - user documentation" -ForegroundColor White  
Write-Host "  SECRETS/    - internal documentation" -ForegroundColor White
Write-Host "  .claude/    - Claude configuration" -ForegroundColor White
Write-Host ""
Write-Host "Run 'git status' to review changes" -ForegroundColor Yellow
