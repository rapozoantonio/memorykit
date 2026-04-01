# MemoryKit Root Directory Cleanup Script
# Moves files from root to appropriate subdirectories while preserving Git history
# Created: 2026-04-01

Write-Host "MemoryKit Root Directory Reorganization" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to repository root
$rootDir = "c:\Users\rapoz\Documents\web-dev\memorykit"
Set-Location $rootDir

# Phase 1: Move test scripts to /scripts/
Write-Host "[Phase 1] Moving test scripts to /scripts/" -ForegroundColor Yellow
git mv test-persistence.ps1 scripts/test-persistence.ps1
git mv test-semantic.ps1 scripts/test-semantic.ps1
git mv docker-test.ps1 scripts/docker-test.ps1
Write-Host "✓ Test scripts moved" -ForegroundColor Green

# Phase 2: Move internal documentation to /SECRETS/
Write-Host "[Phase 2] Moving internal docs to /SECRETS/" -ForegroundColor Yellow
git mv IMPLEMENTATION_SUCCESS.md SECRETS/IMPLEMENTATION_SUCCESS.md
git mv PROJECT_STATUS.md SECRETS/PROJECT_STATUS.md
Write-Host "✓ Internal docs moved" -ForegroundColor Green

# Phase 3: Move utility files to /scripts/
Write-Host "[Phase 3] Moving utility files to /scripts/" -ForegroundColor Yellow
git mv git-branch-commands.sh scripts/git-branch-commands.sh
git mv query-facts.sql scripts/query-facts.sql
Write-Host "✓ Utility files moved" -ForegroundColor Green

# Phase 4: Move documentation to /docs/
Write-Host "[Phase 4] Moving documentation to /docs/" -ForegroundColor Yellow
git mv DEVELOPMENT_GUIDE.md docs/DEVELOPMENT_GUIDE.md
git mv DOCKER_SETUP.md docs/DOCKER_SETUP.md
git mv QUICKSTART.md docs/QUICKSTART.md
git mv PERSISTENCE_TESTING_GUIDE.md docs/TESTING.md
Write-Host "✓ Documentation moved" -ForegroundColor Green

# Phase 5: Move config file to /.claude/
Write-Host "[Phase 5] Moving config to /.claude/" -ForegroundColor Yellow
git mv claude-mcp-config.json .claude/mcp-config.json
Write-Host "✓ Config file moved" -ForegroundColor Green

Write-Host ""
Write-Host "File reorganization complete!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run the documentation update script to fix references" -ForegroundColor White
Write-Host "  2. Review changes with: git status" -ForegroundColor White
Write-Host "  3. Commit with: git commit -m 'refactor: reorganize root directory for better project structure'" -ForegroundColor White
Write-Host ""
