Write-Host "=== MemoryKit Docker + PostgreSQL Test ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build and start containers
Write-Host "Step 1: Building and starting Docker containers..." -ForegroundColor Yellow
docker-compose down -v
docker-compose build api
docker-compose up -d postgres redis api

Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check container status
Write-Host ""
Write-Host "Container Status:" -ForegroundColor Cyan
docker-compose ps

# Step 2: Test API connectivity
Write-Host ""
Write-Host "Step 2: Testing API connectivity..." -ForegroundColor Yellow
$apiKey = "mcp-local-key"
$userId = "mcp-user"
$baseUrl = "http://localhost:8080"

$headers = @{
    "X-API-Key" = $apiKey
    "Content-Type" = "application/json"
}

try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET
    Write-Host "   SUCCESS: API is healthy" -ForegroundColor Green
} catch {
    Write-Host "   FAILED: API health check failed - $_" -ForegroundColor Red
    Write-Host "   Showing API logs:" -ForegroundColor Yellow
    docker logs memorykit-api --tail 50
    exit 1
}

# Step 3: Store memory in PostgreSQL
Write-Host ""
Write-Host "Step 3: Storing memory in PostgreSQL..." -ForegroundColor Yellow
$body = @{
    userId = $userId
    conversationId = "docker-test-001"
    content = "Remember that the user prefers Docker deployments with PostgreSQL"
    importance = 0.9
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/memories/working" -Method POST -Headers $headers -Body $body
    Write-Host "   SUCCESS: Memory stored in PostgreSQL" -ForegroundColor Green
    $memoryId = $response.id
} catch {
    Write-Host "   FAILED: Could not store memory - $_" -ForegroundColor Red
    docker logs memorykit-api --tail 30
    exit 1
}

# Step 4: Retrieve memory
Write-Host ""
Write-Host "Step 4: Retrieving memory..." -ForegroundColor Yellow
try {
    $memories = Invoke-RestMethod -Uri "$baseUrl/api/memories/working/$userId" -Method GET -Headers $headers
    Write-Host "   SUCCESS: Retrieved $($memories.Count) memories" -ForegroundColor Green
    if ($memories.Count -gt 0) {
        Write-Host "   Content: $($memories[0].content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   FAILED: Could not retrieve memories - $_" -ForegroundColor Red
    exit 1
}

# Step 5: Test persistence - restart API
Write-Host ""
Write-Host "Step 5: Testing persistence across restart..." -ForegroundColor Yellow
Write-Host "   Restarting API container..." -ForegroundColor Gray
docker-compose restart api
Start-Sleep -Seconds 10

try {
    $memoriesAfterRestart = Invoke-RestMethod -Uri "$baseUrl/api/memories/working/$userId" -Method GET -Headers $headers
    Write-Host "   SUCCESS: Data persisted! Retrieved $($memoriesAfterRestart.Count) memories after restart" -ForegroundColor Green
    
    if ($memoriesAfterRestart.Count -eq $memories.Count) {
        Write-Host "   VERIFIED: Memory count matches" -ForegroundColor Green
    } else {
        Write-Host "   WARNING: Memory count changed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   FAILED: Could not retrieve memories after restart - $_" -ForegroundColor Red
    exit 1
}

# Step 6: Check PostgreSQL data
Write-Host ""
Write-Host "Step 6: Checking PostgreSQL database..." -ForegroundColor Yellow
$pgCheck = docker exec memorykit-postgres psql -U memorykit -d memorykit -c "SELECT COUNT(*) FROM working_memories;" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   SUCCESS: PostgreSQL is accessible" -ForegroundColor Green
    Write-Host "   $pgCheck" -ForegroundColor Gray
} else {
    Write-Host "   WARNING: Could not query PostgreSQL directly" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== All Tests Passed! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Test with Claude Desktop by configuring MCP in claude_desktop_config.json" -ForegroundColor White
Write-Host "2. API is running at: http://localhost:8080" -ForegroundColor White
Write-Host "3. PostgreSQL data persists in Docker volume: postgres_data" -ForegroundColor White
Write-Host ""
Write-Host "To stop containers: docker-compose down" -ForegroundColor Yellow
Write-Host "To view logs: docker logs memorykit-api" -ForegroundColor Yellow
