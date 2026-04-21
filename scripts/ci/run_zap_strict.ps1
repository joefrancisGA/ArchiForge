#Requires -Version 7.0
<#
.SYNOPSIS
    Runs OWASP ZAP baseline in strict mode against a local API container (same intent as
    .github/workflows/zap-baseline-strict-scheduled.yml).

.DESCRIPTION
    For staging, set $env:ARCHLUCID_ZAP_TARGET to the staging base URL (https host) and ensure
    the API exposes /health/live and the same OpenAPI path as local (/openapi/v1.json), then
    adapt zap-baseline.py -t accordingly (this script defaults to the CI docker-compose shape).

    Default: builds ArchLucid.Api Docker image, runs API on http://127.0.0.1:8080, runs ZAP with infra/zap/config/baseline-pr.tsv
#>
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $root

$image = "archlucid-api:zap-strict-local"
docker build -f ArchLucid.Api/Dockerfile -t $image .

$net = "zapstrict-local-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
docker network create $net | Out-Null
try {
    docker run -d --name archlucid-zap-api-strict --network $net -p 8080:8080 `
        -e ASPNETCORE_URLS=http://+:8080 `
        -e ASPNETCORE_ENVIRONMENT=Development `
        -e ArchLucid__StorageProvider=InMemory `
        -e ArchLucidAuth__Mode=DevelopmentBypass `
        -e IntegrationEvents__TransactionalOutboxEnabled=false `
        -e Demo__SeedOnStartup=false `
        $image | Out-Null

    $ready = $false
    for ($i = 0; $i -lt 90; $i++) {
        try {
            Invoke-WebRequest -Uri "http://127.0.0.1:8080/health/live" -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $ready) {
        docker logs archlucid-zap-api-strict 2>&1 | Select-Object -Last 80
        throw "API did not become healthy on http://127.0.0.1:8080/health/live"
    }

    $zapWrk = Join-Path $env:TEMP "zap-wrk-$net"
    New-Item -ItemType Directory -Force -Path $zapWrk | Out-Null

    docker run --rm --network $net `
        -v "${zapWrk}:/zap/wrk" `
        -v "${root}/infra/zap:/zap/wrk/config:ro" `
        ghcr.io/zaproxy/zaproxy:stable `
        zap-baseline.py -t http://archlucid-zap-api-strict:8080 -c config/baseline-pr.tsv
}
finally {
    docker rm -f archlucid-zap-api-strict 2>$null | Out-Null
    docker network rm $net 2>$null | Out-Null
}
