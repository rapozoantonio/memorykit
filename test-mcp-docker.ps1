# MemoryKit MCP Docker Integration Test
# This script tests the complete Docker-based MCP workflow

Write-Host "`n╔════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   MemoryKit MCP Docker Integration Test           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$testsPassed = 0
$testsFailed = 0

# Test 1: Verify Docker is running
Write-Host "[1/5] Checking Docker daemon..." -ForegroundColor Yellow
docker version --format "{{.Server.Version}}" 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Docker is running" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  ✗ Docker is not running" -ForegroundColor Red
    $testsFailed++
    exit 1
}

# Test 2: Verify image exists
Write-Host "`n[2/5] Checking Docker image..." -ForegroundColor Yellow
$image = docker images memorykit-api --format "{{.Repository}}" 2>$null | Select-Object -First 1
if ($image -eq "memorykit-api") {
    Write-Host "  ✓ Image memorykit-api:latest found" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  ✗ Image not found. Building..." -ForegroundColor Yellow
    Write-Host "  Building Docker image (this may take 2-3 minutes)..." -ForegroundColor Cyan
    docker-compose build mcp-api 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Image built successfully" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "  ✗ Image build failed" -ForegroundColor Red
        $testsFailed++
        exit 1
    }
}

# Test 3: Start container
Write-Host "`n[3/5] Starting MCP API container..." -ForegroundColor Yellow
docker-compose --profile mcp down 2>&1 | Out-Null
Start-Sleep -Seconds 2
docker-compose --profile mcp up -d mcp-api 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Container started" -ForegroundColor Green
    $testsPassed++
} else {
    Write-Host "  ✗ Container failed to start" -ForegroundColor Red
    $testsFailed++
    exit 1
}

# Test 4: Wait for health check and test API
Write-Host "`n[4/5] Testing API health endpoint..." -ForegroundColor Yellow
Write-Host "  Waiting for API to be ready..." -ForegroundColor Cyan

$maxRetries = 30
$retryCount = 0
$healthy = $false

while ($retryCount -lt $maxRetries -and -not $healthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5555/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            Write-Host "  ✓ Health check passed" -ForegroundColor Green
            Write-Host "  Response: $($response.Content)" -ForegroundColor Gray
            $testsPassed++
        }
    } catch {
        # Expected during startup
    }
    
    if (-not $healthy) {
        Start-Sleep -Seconds 1
        $retryCount++
        if ($retryCount % 5 -eq 0) {
            Write-Host "  Attempt $retryCount/$maxRetries..." -ForegroundColor Gray
        }
    }
}

if (-not $healthy) {
    Write-Host "  ✗ Health check failed after $maxRetries attempts" -ForegroundColor Red
    Write-Host "`nContainer logs:" -ForegroundColor Yellow
    docker-compose logs --tail=50 mcp-api
    $testsFailed++
    docker-compose --profile mcp down 2>&1 | Out-Null
    exit 1
}

# Test 5: Test conversation creation
Write-Host "`n[5/5] Testing conversation creation..." -ForegroundColor Yellow
try {
    $body = @{
        userId = "test-user"
    } | ConvertTo-Json

    $headers = @{
        "X-API-Key" = "mcp-local-key"
        "Content-Type" = "application/json"
    }

    $response = Invoke-WebRequest -Uri "http://localhost:5555/api/v1/conversations" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -UseBasicParsing `
        -TimeoutSec 5

    if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 201) {
        $conversation = $response.Content | ConvertFrom-Json
        Write-Host "  ✓ Conversation created successfully" -ForegroundColor Green
        Write-Host "  Conversation ID: $($conversation.conversationId)" -ForegroundColor Gray
        $testsPassed++
    } else {
        Write-Host "  ✗ Unexpected status code: $($response.StatusCode)" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "  ✗ Failed to create conversation: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Cleanup
Write-Host "`n[Cleanup] Stopping container..." -ForegroundColor Yellow
docker-compose --profile mcp down 2>&1 | Out-Null
Write-Host "  ✓ Container stopped" -ForegroundColor Green

# Summary
Write-Host "`n╔════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Test Summary                                     ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "  Passed: $testsPassed/5" -ForegroundColor Green
Write-Host "  Failed: $testsFailed/5" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })

if ($testsFailed -eq 0) {
    Write-Host "`n✓ All tests passed! Docker integration is working." -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Test MCP server: cd mcp-server && node test-docker.js" -ForegroundColor White
    Write-Host "  2. Link for Claude: cd mcp-server && npm link" -ForegroundColor White
    Write-Host "  3. Configure Claude Desktop with 'memorykit-mcp' command" -ForegroundColor White
    exit 0
} else {
    Write-Host "`n✗ Some tests failed. Check the output above for details." -ForegroundColor Red
    exit 1
}
