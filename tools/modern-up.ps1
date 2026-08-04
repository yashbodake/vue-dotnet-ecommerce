#!/usr/bin/env pwsh
# modern-up.ps1 - Build and start the ecommerce stack in Docker

$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot ".." "docker-compose.yml"

Write-Host "Building and starting ecommerce stack..." -ForegroundColor Green
docker compose -f $composeFile up --build -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start services" -ForegroundColor Red
    exit 1
}

Write-Host "`nServices started:" -ForegroundColor Green
Write-Host "  Gateway: http://localhost:5000" -ForegroundColor Cyan
Write-Host "  API:     http://localhost:5100" -ForegroundColor Cyan
Write-Host "  Web:     http://localhost:5173" -ForegroundColor Cyan

Write-Host "`nWaiting for services to warm up..." -ForegroundColor Green
Start-Sleep -Seconds 5

function Test-Endpoint($uri, $description) {
    try {
        $response = Invoke-WebRequest -Uri $uri -TimeoutSec 10 -UseBasicParsing
        Write-Host "$description OK ($($response.StatusCode))" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "$description FAILED: $_" -ForegroundColor Red
        return $false
    }
}

$ok = $true
$ok = (Test-Endpoint "http://127.0.0.1:5000/api/health" "Health check") -and $ok
$ok = (Test-Endpoint "http://127.0.0.1:5000/api/products?page=1&pageSize=1" "Products check") -and $ok
$ok = (Test-Endpoint "http://127.0.0.1:5000/" "Gateway root") -and $ok

Write-Host "`nDemo URL: http://localhost:5000" -ForegroundColor Cyan

if (-not $ok) {
    exit 1
}
