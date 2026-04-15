# Starts the ArchLucid Docker demo stack (SQL, Azurite, Redis, API, UI) with demo seed + simulator agents.
# Prerequisites: Docker Desktop or Docker Engine. No .NET SDK or Node.js required.
# Repository root is the parent of the scripts/ directory.

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ComposeBase = Join-Path $RepoRoot "docker-compose.yml"
$ComposeDemo = Join-Path $RepoRoot "docker-compose.demo.yml"

if (-not (Test-Path $ComposeBase)) {
    Write-Error "Expected docker-compose.yml at $ComposeBase"
}

if (-not (Test-Path $ComposeDemo)) {
    Write-Error "Expected docker-compose.demo.yml at $ComposeDemo"
}

docker info 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker does not appear to be running. Start Docker Desktop or the Docker daemon, then retry."
}

Set-Location $RepoRoot

Write-Host "Building and starting ArchLucid demo stack (full-stack profile + demo overlay)..." -ForegroundColor Cyan
docker compose -f $ComposeBase -f $ComposeDemo --profile full-stack up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose up failed with exit code $LASTEXITCODE"
}

$apiReadyUrl = "http://127.0.0.1:5000/health/ready"
$deadline = (Get-Date).AddSeconds(120)
$ok = $false

while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri $apiReadyUrl -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) {
            $ok = $true
            break
        }
    }
    catch {
        # API still starting
    }

    Start-Sleep -Seconds 5
}

if (-not $ok) {
    Write-Host ""
    Write-Host "Timed out waiting for $apiReadyUrl (120s)." -ForegroundColor Yellow
    Write-Host "Check logs: docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack logs api"
    Write-Host "Ensure ports 1433, 3000, 5000, 10000-10002, 6379 are not in use by another process."
    exit 1
}

Write-Host "API is ready." -ForegroundColor Green
Write-Host "Operator UI: http://localhost:3000/runs/new" -ForegroundColor Green

$uiUrl = "http://localhost:3000/runs/new"
try {
    Start-Process $uiUrl
}
catch {
    Write-Host "Open manually: $uiUrl"
}
