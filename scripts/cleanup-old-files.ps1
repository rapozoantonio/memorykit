# MemoryKit Root Directory Cleanup - Delete Old Files
# Run this ONLY after verifying new file locations work correctly
# Created: 2026-04-01

Write-Host "⚠️  WARNING: This will DELETE the old files from root directory" -ForegroundColor Yellow
Write-Host "Make sure you've verified the new file locations work first!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Files to be deleted:" -ForegroundColor Cyan
Write-Host "  - test-persistence.ps1" -ForegroundColor Gray
Write-Host "  - test-semantic.ps1" -ForegroundColor Gray
Write-Host "  - docker-test.ps1" -ForegroundColor Gray
Write-Host "  - git-branch-commands.sh" -ForegroundColor Gray
Write-Host "  - query-facts.sql" -ForegroundColor Gray
Write-Host "  - IMPLEMENTATION_SUCCESS.md" -ForegroundColor Gray
Write-Host "  - PROJECT_STATUS.md" -ForegroundColor Gray
Write-Host "  - DEVELOPMENT_GUIDE.md" -ForegroundColor Gray
Write-Host "  - DOCKER_SETUP.md" -ForegroundColor Gray
Write-Host "  - QUICKSTART.md" -ForegroundColor Gray
Write-Host "  - PERSISTENCE_TESTING_GUIDE.md" -ForegroundColor Gray
Write-Host "  - claude-mcp-config.json" -ForegroundColor Gray
Write-Host ""

$response = Read-Host "Type 'DELETE' to confirm deletion"

if ($response -ne "DELETE") {
    Write-Host "Cancelled - no files deleted" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Deleting old files..." -ForegroundColor Red

$rootDir = "c:\Users\rapoz\Documents\web-dev\memorykit"
Set-Location $rootDir

# Delete test scripts
Remove-Item -Path "test-persistence.ps1" -ErrorAction SilentlyContinue
Remove-Item -Path "test-semantic.ps1" -ErrorAction SilentlyContinue
Remove-Item -Path "docker-test.ps1" -ErrorAction SilentlyContinue

# Delete utility files
Remove-Item -Path "git-branch-commands.sh" -ErrorAction SilentlyContinue
Remove-Item -Path "query-facts.sql" -ErrorAction SilentlyContinue

# Delete internal docs
Remove-Item -Path "IMPLEMENTATION_SUCCESS.md" -ErrorAction SilentlyContinue
Remove-Item -Path "PROJECT_STATUS.md" -ErrorAction SilentlyContinue

# Delete documentation
Remove-Item -Path "DEVELOPMENT_GUIDE.md" -ErrorAction SilentlyContinue
Remove-Item -Path "DOCKER_SETUP.md" -ErrorAction SilentlyContinue
Remove-Item -Path "QUICKSTART.md" -ErrorAction SilentlyContinue
Remove-Item -Path "PERSISTENCE_TESTING_GUIDE.md" -ErrorAction SilentlyContinue

# Delete config file
Remove-Item -Path "claude-mcp-config.json" -ErrorAction SilentlyContinue

Write-Host "✅ Old files deleted successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Git status:" -ForegroundColor Cyan
git status

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review changes: git status" -ForegroundColor White
Write-Host "2. Commit: git add . && git commit -m 'refactor: reorganize root directory structure'" -ForegroundColor White
Write-Host "3. Push: git push" -ForegroundColor White
