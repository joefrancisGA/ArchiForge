# Record k6 hot-path baseline against Docker Compose full-stack (see docs/LOAD_TEST_BASELINE.md).
# Requires: Docker Desktop (Linux engine), Python 3 on PATH.
# From repo root: pwsh ./scripts/load/record_baseline.ps1
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $RepoRoot

Write-Host "Starting full-stack Compose..."
docker compose --profile full-stack up -d --build

$Deadline = (Get-Date).AddMinutes(12)
$Healthy = $false
while ((Get-Date) -lt $Deadline) {
    try {
        $Response = Invoke-WebRequest -Uri "http://127.0.0.1:5000/health/live" -UseBasicParsing -TimeoutSec 5
        if ($Response.StatusCode -eq 200) {
            $Healthy = $true
            break
        }
    }
    catch {
        Write-Host "Waiting for API health..."
    }
    Start-Sleep -Seconds 3
}

if (-not $Healthy) {
    throw "GET http://127.0.0.1:5000/health/live did not return 200 before timeout."
}

Write-Host "Running k6 in container (host.docker.internal -> API on host port 5000)..."
docker run --rm `
    -v "${RepoRoot}:/work" `
    -w /work `
    -e BASE_URL=http://host.docker.internal:5000 `
    -e VUS=5 `
    -e DURATION=2m `
    -e SLEEP_SEC=1 `
    -e K6_SUMMARY_TREND_STATS=med,p(95),p(99) `
    grafana/k6:latest run scripts/load/hotpaths.js --summary-export /work/k6-summary.json

python scripts/ci/print_k6_summary_metrics.py k6-summary.json

Write-Host "Tearing down Compose..."
docker compose --profile full-stack down --remove-orphans

Write-Host "Done. Update docs/LOAD_TEST_BASELINE.md with printed p50/p95/p99/rate and adjust scripts/load/hotpaths.js p(95) using the suggested threshold line."
