$API_URL = "http://localhost:5555"
$API_KEY = "mcp-local-key"
$USER_ID = "test-user-semantic"
$CONV_ID = "test-conv-semantic"

Write-Host "=== Testing Heuristic Semantic Fact Extraction ===" -ForegroundColor Cyan

# Test 1
Write-Host "`nTest 1: Multiple entity types..." -ForegroundColor Yellow
$body1 = @{
    content = "My name is Alice Johnson. I prefer using Docker. We decided to use Redis for caching."
    role = 0
} | ConvertTo-Json

$r1 = Invoke-RestMethod -Uri "$API_URL/api/v1/conversations/$CONV_ID/messages" -Method POST -Headers @{ "X-API-Key" = $API_KEY; "Content-Type" = "application/json" } -Body $body1
Write-Host "Stored: $($r1.messageId)" -ForegroundColor Green

# Test 2
Write-Host "`nTest 2: Narrative content..." -ForegroundColor Yellow
$body2 = @{
    content = "The first time I programmed was when I was 12 years old in Barcelona."
    role = 0
} | ConvertTo-Json

$r2 = Invoke-RestMethod -Uri "$API_URL/api/v1/conversations/$CONV_ID/messages" -Method POST -Headers @{ "X-API-Key" = $API_KEY; "Content-Type" = "application/json" } -Body $body2
Write-Host "Stored: $($r2.messageId)" -ForegroundColor Green

# Test 3
Write-Host "`nTest 3: Goals and constraints..." -ForegroundColor Yellow
$body3 = @{
    content = "My goal is to deploy by Friday. We must stay under 1GB memory."
    role = 0
} | ConvertTo-Json

$r3 = Invoke-RestMethod -Uri "$API_URL/api/v1/conversations/$CONV_ID/messages" -Method POST -Headers @{ "X-API-Key" = $API_KEY; "Content-Type" = "application/json" } -Body $body3
Write-Host "Stored: $($r3.messageId)" -ForegroundColor Green

Write-Host "`nWaiting 2 seconds for processing..." -ForegroundColor Cyan
Start-Sleep -Seconds 2

# Retrieve
Write-Host "`nRetrieving context..." -ForegroundColor Yellow
$queryBody = @{
    userId = $USER_ID
    conversationId = $CONV_ID
    query = "What do you know about me?"
    maxResults = 10
} | ConvertTo-Json

$context = Invoke-RestMethod -Uri "$API_URL/api/v1/conversations/$CONV_ID/context?query=What+do+you+know+about+me&maxResults=10" -Method GET -Headers @{ "X-API-Key" = $API_KEY }

Write-Host "`n=== Results ===" -ForegroundColor Cyan
Write-Host "Working Memory: $($context.workingMemory.Count)" -ForegroundColor White
Write-Host "Episodic Events: $($context.episodicMemory.Count)" -ForegroundColor White
Write-Host "Semantic Facts: $($context.semanticFacts.Count)" -ForegroundColor White

if ($context.semanticFacts.Count -gt 0) {
    Write-Host "`n=== Extracted Facts ===" -ForegroundColor Green
    foreach ($f in $context.semanticFacts) {
        Write-Host "  [$($f.factType)] $($f.content)" -ForegroundColor White
    }
    Write-Host "`nSUCCESS!" -ForegroundColor Green
} else {
    Write-Host "`nNo facts found" -ForegroundColor Red
}

Write-Host "`nView in pgAdmin at http://localhost:5050" -ForegroundColor Yellow
