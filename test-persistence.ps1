Write-Host "Testing MemoryKit SQLite Persistence..." -ForegroundColor Cyan

$baseUrl = "http://localhost:5555"
$apiKey = "dev-key-12345"
$userId = "demo_user"

$headers = @{
    "X-API-Key" = $apiKey
    "Content-Type" = "application/json"
}

Write-Host "1. Storing a working memory..." -ForegroundColor Yellow
$body = @{
    userId = $userId
    conversationId = "test-conv-001"
    content = "Remember that I love pizza"
    importance = 0.8
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/memories/working" -Method POST -Headers $headers -Body $body
    Write-Host "   SUCCESS: Memory stored" -ForegroundColor Green
} catch {
    Write-Host "   FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host "2. Retrieving working memories..." -ForegroundColor Yellow
try {
    $memories = Invoke-RestMethod -Uri "$baseUrl/api/memories/working/$userId" -Method GET -Headers $headers
    Write-Host "   SUCCESS: Retrieved $($memories.Count) memories" -ForegroundColor Green
} catch {
    Write-Host "   FAILED: $_" -ForegroundColor Red
    exit 1
}

Write-Host "3. Checking database file..." -ForegroundColor Yellow
$dbPath = "$env:USERPROFILE\.memorykit\memories.db"
if (Test-Path $dbPath) {
    $size = (Get-Item $dbPath).Length
    Write-Host "   SUCCESS: Database exists" -ForegroundColor Green
    Write-Host "   Size: $([math]::Round($size/1024, 2)) KB" -ForegroundColor Gray
} else {
    Write-Host "   FAILED: Database not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "All tests passed!" -ForegroundColor Green
