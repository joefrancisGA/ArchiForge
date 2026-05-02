#Requires -Version 7.0
<#
.SYNOPSIS
    Staging smoke test: health, version, architecture create/execute/poll/commit, authority manifest.

.DESCRIPTION
    Uses ARCHLUCID_BASE_URL or ARCHLUCID_API_BASE_URL, optional ARCHLUCID_API_KEY (X-Api-Key).
    Writes staging-smoke-results.json or STAGING_SMOKE_RESULTS_FILE.
#>
param(
    [string] $BaseUrl = ""
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

function Get-ResolvedBase {
    param([string] $B)
    if (-not [string]::IsNullOrWhiteSpace($B)) { return $B.TrimEnd("/") }
    if (-not [string]::IsNullOrWhiteSpace($env:ARCHLUCID_BASE_URL)) { return $env:ARCHLUCID_BASE_URL.TrimEnd("/") }
    if (-not [string]::IsNullOrWhiteSpace($env:ARCHLUCID_API_BASE_URL)) { return $env:ARCHLUCID_API_BASE_URL.TrimEnd("/") }
    return "http://127.0.0.1:5000"
}

function New-SmokeHeaders {
    $h = @{}
    if (-not [string]::IsNullOrWhiteSpace($env:ARCHLUCID_API_KEY)) {
        $h["X-Api-Key"] = $env:ARCHLUCID_API_KEY
    }
    return $h
}

function NowMs {
    [int64]([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
}

$resolved = Get-ResolvedBase -B $BaseUrl
$outFile = if ($env:STAGING_SMOKE_RESULTS_FILE) { $env:STAGING_SMOKE_RESULTS_FILE } else { "staging-smoke-results.json" }
$headers = New-SmokeHeaders

function Fail([string] $msg) {
    Write-Error "STAGING SMOKE FAIL: $msg"
    exit 1
}

$timings = @{}

$t0 = NowMs
$r = Invoke-WebRequest -Uri "$resolved/health/live" -Headers $headers -SkipHttpErrorCheck
$timings["health_live"] = (NowMs) - $t0
if ($r.StatusCode -ne 200) { Fail "GET /health/live returned $($r.StatusCode)" }

$t0 = NowMs
$r = Invoke-WebRequest -Uri "$resolved/health/ready" -Headers $headers -SkipHttpErrorCheck
$timings["health_ready"] = (NowMs) - $t0
if ($r.StatusCode -ne 200) { Fail "GET /health/ready returned $($r.StatusCode)" }

$t0 = NowMs
$r = Invoke-WebRequest -Uri "$resolved/version" -Headers $headers -SkipHttpErrorCheck
$timings["version"] = (NowMs) - $t0
if ($r.StatusCode -ne 200) { Fail "GET /version returned $($r.StatusCode)" }
$versionJson = $r.Content

$reqId = [guid]::NewGuid().ToString("N")
$createBody = @{
    requestId            = $reqId
    systemName           = "Staging Smoke"
    description          = "Automated staging smoke run (PowerShell)."
    environment          = "staging"
    cloudProvider        = "Azure"
    constraints          = @("smoke-test")
    requiredCapabilities = @("web", "sql")
    assumptions          = @("staging-smoke.ps1")
} | ConvertTo-Json -Depth 10 -Compress

$t0 = NowMs
$rCreate = Invoke-WebRequest -Uri "$resolved/v1/architecture/request" -Method Post -ContentType "application/json" `
    -Body $createBody -Headers $headers -SkipHttpErrorCheck
$timings["create_run"] = (NowMs) - $t0
if ($rCreate.StatusCode -ne 200 -and $rCreate.StatusCode -ne 201) {
    Fail "POST /v1/architecture/request returned $($rCreate.StatusCode) $($rCreate.Content)"
}
$createPayload = $rCreate.Content | ConvertFrom-Json
$runId = [string] $createPayload.run.runId
if ([string]::IsNullOrWhiteSpace($runId)) { Fail "missing runId" }

try {
    $null = Invoke-WebRequest -Uri "$resolved/v1/architecture/run/$([uri]::EscapeDataString($runId))/execute" -Method Post `
        -ContentType "application/json" -Body "{}" -Headers $headers -SkipHttpErrorCheck
} catch {
    # best-effort execute
}

$t0 = NowMs
$deadline = (NowMs) + 300000
$status = $null
while ((NowMs) -lt $deadline) {
    $g = Invoke-WebRequest -Uri "$resolved/v1/architecture/run/$([uri]::EscapeDataString($runId))" -Headers $headers -SkipHttpErrorCheck
    if ($g.StatusCode -ne 200) { Fail "poll returned $($g.StatusCode)" }
    $detail = $g.Content | ConvertFrom-Json
    $status = [string] $detail.run.status
    if ($status -eq "ReadyForCommit") { break }
    if ($status -eq "Failed") { Fail "run Failed" }
    Start-Sleep -Seconds 2
}
$timings["poll_ready"] = (NowMs) - $t0
if ($status -ne "ReadyForCommit") { Fail "timeout lastStatus=$status" }

$t0 = NowMs
$rCommit = Invoke-WebRequest -Uri "$resolved/v1/architecture/run/$([uri]::EscapeDataString($runId))/commit" -Method Post `
    -ContentType "application/json" -Body "{}" -Headers $headers -SkipHttpErrorCheck
$timings["commit"] = (NowMs) - $t0
if ($rCommit.StatusCode -ne 200) { Fail "commit returned $($rCommit.StatusCode) $($rCommit.Content)" }

$t0 = NowMs
$rMan = Invoke-WebRequest -Uri "$resolved/v1/authority/runs/$([uri]::EscapeDataString($runId))/manifest" -Headers $headers -SkipHttpErrorCheck
$timings["get_manifest"] = (NowMs) - $t0
if ($rMan.StatusCode -ne 200) { Fail "manifest $($rMan.StatusCode) $($rMan.Content)" }
$manRaw = $rMan.Content

$manOk = $false
$manObj = $null
try {
    $manObj = $manRaw | ConvertFrom-Json
    $manOk = $true
} catch {
    $manOk = $false
}

$manifestId = $null
if ($null -ne $manObj -and $manObj.PSObject.Properties.Match("manifestId").Count -gt 0) {
    $manifestId = [string] $manObj.manifestId
}

$verObj = $null
try {
    $verObj = $versionJson | ConvertFrom-Json
} catch {
    $verObj = $versionJson
}

$payload = [ordered]@{
    ok         = $true
    baseUrl    = $resolved
    runId      = $runId
    requestId  = $reqId
    timingsMs  = $timings
    version    = $verObj
    manifest   = @{
        jsonLength = $manRaw.Length
        parseOk    = $manOk
        manifestId = $manifestId
    }
}

($payload | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $outFile -Encoding utf8
Write-Output ($payload | ConvertTo-Json -Depth 12)
Write-Output "STAGING SMOKE OK runId=$runId -> $outFile"
