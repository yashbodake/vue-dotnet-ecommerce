#!/usr/bin/env pwsh
# modern-up.ps1 - Build and start the modern ecommerce stack in Docker

$ErrorActionPreference = "Stop"

$composeFile = Join-Path $PSScriptRoot ".." "modern" "docker-compose.yml"

Write-Host "Building and starting modern ecommerce stack..." -ForegroundColor Green
docker compose -f $composeFile up --build -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nServices started:" -ForegroundColor Green
    Write-Host "  Gateway: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "  API:     http://localhost:5100" -ForegroundColor Cyan
    Write-Host "  Web:     http://localhost:5173" -ForegroundColor Cyan
    
    Write-Host "`nChecking health..." -ForegroundColor Green
    Start-Sleep -Seconds 3
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "Gateway is healthy!" -ForegroundColor Green
        }
    } catch {
        Write-Host "Gateway health check pending - may need more time to start" -ForegroundColor Yellow
    }
} else {
    Write-Host "Failed to start services" -ForegroundColor Red
    exit 1
}
