# Stops the ArchLucid Docker demo stack and removes volumes (Azurite data, etc.).

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ComposeBase = Join-Path $RepoRoot "docker-compose.yml"
$ComposeDemo = Join-Path $RepoRoot "docker-compose.demo.yml"

Set-Location $RepoRoot

Write-Host "Stopping ArchLucid demo stack and removing volumes..." -ForegroundColor Cyan
docker compose -f $ComposeBase -f $ComposeDemo --profile full-stack down -v
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose down failed with exit code $LASTEXITCODE"
}

Write-Host "Done." -ForegroundColor Green
