# MemoryKit Scripts

This directory contains utility scripts for development, testing, and deployment workflows.

## Build & Packaging

### `build-executables.ps1` / `build-executables.sh`

Builds standalone executables of the MemoryKit API for different platforms.

**PowerShell (Windows):**

```powershell
.\build-executables.ps1
```

**Bash (Linux/Mac):**

```bash
chmod +x build-executables.sh
./build-executables.sh
```

**Output:**

- `publish/win-x64/` - Windows 64-bit executable
- `publish/linux-x64/` - Linux 64-bit executable
- `publish/osx-arm64/` - macOS ARM64 executable

---

## Testing Scripts

### `test-persistence.ps1`

Tests SQLite persistence functionality for MemoryKit API.

**Usage:**

```powershell
.\test-persistence.ps1
```

**What it tests:**

- Stores a working memory via REST API
- Retrieves memories from storage
- Verifies SQLite database file exists

**Prerequisites:**

- MemoryKit API running on `http://localhost:5555`
- Valid API key configured (`dev-key-12345` by default)

---

### `test-semantic.ps1`

Tests heuristic semantic fact extraction from natural language.

**Usage:**

```powershell
.\test-semantic.ps1
```

**What it tests:**

- Extracts entities from multiple message types (names, technologies, goals, decisions)
- Verifies semantic facts are stored in PostgreSQL
- Checks fact types and confidence scores

**Prerequisites:**

- Docker containers running (`docker-compose up -d`)
- PostgreSQL accessible on port 5432

**Expected results:**

- 2-4 extracted facts per message
- Facts stored in `SemanticFacts` table with proper typing

---

### `docker-test.ps1`

Comprehensive Docker + PostgreSQL integration test.

**Usage:**

```powershell
.\docker-test.ps1
```

**What it tests:**

1. Docker containers startup (API, PostgreSQL, Redis)
2. API health check
3. Memory storage in PostgreSQL
4. Memory retrieval
5. **Data persistence across API restart** (critical test)
6. Direct PostgreSQL connectivity

**Key validation:**

- Data survives API container restart
- PostgreSQL tables are properly created
- All services are healthy

**Duration:** ~30 seconds (includes 15s container startup wait)

---

## Utility Scripts

### `git-branch-commands.sh`

Git workflow commands for creating feature branches with comprehensive commit messages.

**Usage:**

```bash
# Review the commands first
cat git-branch-commands.sh

# Execute interactively (copy/paste relevant sections)
```

**Includes:**

- Feature branch creation
- Staging MCP-related changes
- Conventional commit message template
- Push to remote with tracking

**Note:** This is a reference/template script, not meant to be executed directly.

---

### `query-facts.sql`

SQL query to inspect extracted semantic facts in PostgreSQL.

**Usage:**

```powershell
# Via psql in Docker
docker exec -it memorykit-postgres psql -U memorykit -d memorykit -f scripts/query-facts.sql

# Or copy/paste into pgAdmin query editor
```

**Query:**

```sql
SELECT * FROM "SemanticFacts" ORDER BY "CreatedAt" DESC LIMIT 10;
```

**Output columns:**

- `Id`, `UserId`, `ConversationId`
- `Content` (extracted fact text)
- `FactType` (Person, Technology, Goal, Decision, Other)
- `Confidence` (0.0-1.0 score)
- `Embedding` (1536-dimension vector for semantic search)
- `CreatedAt`, `UpdatedAt`

---

## Reorganization Script

### `reorganize-root.ps1`

Cleanup script for reorganizing root directory structure (moves files to appropriate subdirectories).

**Usage:**

```powershell
# Review what will be moved
Get-Content .\reorganize-root.ps1

# Execute reorganization
.\reorganize-root.ps1

# Commit changes
git status
git commit -m "refactor: reorganize root directory for better project structure"
```

**Moves:**

- Test scripts → `/scripts/`
- Internal docs → `/SECRETS/`
- Documentation → `/docs/`
- Config files → `/.claude/`

---

## Common Workflows

### Running Full Test Suite

```powershell
# 1. Start Docker environment
docker-compose up -d

# 2. Wait for services to be ready
Start-Sleep -Seconds 15

# 3. Run Docker integration tests
.\scripts\docker-test.ps1

# 4. Run semantic extraction tests
.\scripts\test-semantic.ps1

# 5. Stop containers
docker-compose down
```

### Local Development Testing

```powershell
# 1. Start API locally (not in Docker)
cd src/MemoryKit.API
dotnet run

# 2. In new terminal, run persistence tests
cd ../..
.\scripts\test-persistence.ps1
```

### Debugging PostgreSQL Data

```powershell
# Connect to PostgreSQL
docker exec -it memorykit-postgres psql -U memorykit -d memorykit

# List tables
\dt

# Query working memories
SELECT "Id", LEFT("Content", 50), "CreatedAt" FROM "WorkingMemories" ORDER BY "CreatedAt" DESC;

# Query semantic facts
\i scripts/query-facts.sql

# Exit
\q
```

---

## Script Maintenance

When adding new scripts:

1. **Naming convention**: Use kebab-case for shell scripts, PascalCase for PowerShell
2. **Documentation**: Add entry to this README with usage instructions
3. **Error handling**: Include try-catch blocks and meaningful error messages
4. **Prerequisites check**: Validate required services/tools are available before running
5. **Exit codes**: Return 0 for success, non-zero for failures
6. **Color coding**: Use Write-Host with colors for better readability (PowerShell)

**Template for new PowerShell script:**

```powershell
Write-Host "=== Script Name ===" -ForegroundColor Cyan

# Prerequisites check
if (-not (Test-Path "required-file")) {
    Write-Host "ERROR: Required file not found" -ForegroundColor Red
    exit 1
}

# Main logic
try {
    Write-Host "Step 1: Doing something..." -ForegroundColor Yellow
    # ... your code ...
    Write-Host "   SUCCESS" -ForegroundColor Green
} catch {
    Write-Host "   FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`nAll done!" -ForegroundColor Green
```

---

## Troubleshooting

**PowerShell script execution disabled:**

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

**Docker not found:**

- Ensure Docker Desktop is installed and running
- Check: `docker --version`

**Port conflicts:**

- Check running containers: `docker ps`
- Stop conflicting services or change ports in `docker-compose.yml`

**Permission denied (Linux/Mac):**

```bash
chmod +x *.sh
```

---

For more detailed documentation, see:

- [DEVELOPMENT_GUIDE.md](../docs/DEVELOPMENT_GUIDE.md) - Full development workflow
- [TESTING.md](../docs/TESTING.md) - Comprehensive testing guide
- [DOCKER_SETUP.md](../docs/DOCKER_SETUP.md) - Docker configuration details
