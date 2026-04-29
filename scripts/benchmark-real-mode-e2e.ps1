#Requires -Version 7.0
<#
.SYNOPSIS
    Measures end-to-end wall-clock time from architecture request submission to committed manifest
    when the ArchLucid API is running in real Azure OpenAI mode.

.DESCRIPTION
    Dedicated real-mode benchmark companion to scripts/benchmark-e2e-time.ps1.
    Calls the public HTTP API through all three phases:
      Phase 1 — Create:   POST /v1/architecture/request  (Contoso Retail sample brief)
      Phase 2 — Execute:  POST /v1/architecture/run/{runId}/execute  (with pilot real-mode header)
                           + poll GET /v1/architecture/run/{runId} until ReadyForCommit or timeout
      Phase 3 — Commit:   POST /v1/architecture/run/{runId}/commit

    The script records wall-clock milliseconds for each phase and total end-to-end, then emits a
    versioned JSON summary to stdout. By default it also writes artifacts/benchmark-real-mode-latest.json
    under the repository root (UTF-8, no BOM — gitignored folder). Override with -ArtifactPath or
    suppress with -SkipArtifact.

    Prerequisites:
      - ArchLucid API running in real mode (AgentExecution__Mode=Real) with Azure OpenAI env vars
        configured on the host.  See docs/library/FIRST_REAL_VALUE.md for the real-AOAI compose overlay.
      - Optional: ARCHLUCID_API_KEY env var for keyed deployments (sent as X-Api-Key).
      - This script does NOT require or embed any Azure OpenAI keys — those belong to the API host.

    Environment:
      ARCHLUCID_API_BASE_URL — base URL when -BaseUrl is omitted (default http://localhost:5000).

.PARAMETER BaseUrl
    API base URL.  Falls back to ARCHLUCID_API_BASE_URL, then http://localhost:5000.

.PARAMETER TimeoutSeconds
    Maximum seconds to wait for the run to reach ReadyForCommit (default 300 = 5 min).

.PARAMETER PollIntervalSeconds
    Seconds between status polls (default 3).

.PARAMETER ArtifactPath
    Canonical JSON artifact path (default <repo>/artifacts/benchmark-real-mode-latest.json).

.PARAMETER SkipArtifact
    Do not write the canonical artifact file (stdout JSON only unless -OutputFile is used).

.PARAMETER OutputFile
    Optional additional path to write the same JSON (e.g. CI upload staging).

.EXAMPLE
    pwsh ./scripts/benchmark-real-mode-e2e.ps1
    pwsh ./scripts/benchmark-real-mode-e2e.ps1 -BaseUrl http://127.0.0.1:5000 -OutputFile results/extra-copy.json
    pwsh ./scripts/benchmark-real-mode-e2e.ps1 -TimeoutSeconds 600 -SkipArtifact
#>
param(
    [string] $BaseUrl = $env:ARCHLUCID_API_BASE_URL,

    [ValidateRange(10, 1800)]
    [int] $TimeoutSeconds = 300,

    [ValidateRange(1, 30)]
    [int] $PollIntervalSeconds = 3,

    [string] $ArtifactPath,

    [switch] $SkipArtifact,

    [string] $OutputFile
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($ArtifactPath)) {
    $ArtifactPath = Join-Path $RepoRoot "artifacts" "benchmark-real-mode-latest.json"
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Resolve-BaseUrl {
    param([string] $Candidate)
    if ([string]::IsNullOrWhiteSpace($Candidate)) { return "http://localhost:5000" }
    return $Candidate.TrimEnd("/")
}

function Build-Headers {
    param([bool] $IncludePilotRealMode)
    $h = @{}
    $key = $env:ARCHLUCID_API_KEY

    if (-not [string]::IsNullOrWhiteSpace($key)) {
        $h["X-Api-Key"] = $key
    }

    if ($IncludePilotRealMode) {
        $h["X-ArchLucid-Pilot-Try-Real-Mode"] = "1"
    }

    return $h
}

function Invoke-JsonPost {
    param(
        [string] $Uri,
        [object] $Body,
        [hashtable] $Headers
    )
    $splat = @{
        Uri                = $Uri
        Method             = "Post"
        ContentType        = "application/json"
        SkipHttpErrorCheck = $true
    }

    if ($Headers.Count -gt 0) { $splat.Headers = $Headers }
    if ($null -ne $Body) { $splat.Body = ($Body | ConvertTo-Json -Depth 20 -Compress) }
    else { $splat.Body = "{}" }

    return Invoke-WebRequest @splat
}

function Get-BenchmarkEnvironmentBlock {
    param(
        [string] $BaseUrlResolved,
        [int] $TimeoutSec,
        [int] $PollSec
    )
    $rawAoai = [Environment]::GetEnvironmentVariable("ARCHLUCID_REAL_AOAI")
    $realAoai = $null

    if (-not [string]::IsNullOrWhiteSpace($rawAoai)) {
        $realAoai = $rawAoai
    }

    [ordered]@{
        baseUrl              = $BaseUrlResolved
        timeoutSeconds       = $TimeoutSec
        pollIntervalSeconds  = $PollSec
        archLucidRealAoai    = $realAoai
        apiKeyEnvPresent     = -not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable("ARCHLUCID_API_KEY"))
    }
}

function Write-StableBenchmarkJson {
    param(
        $OrderedDocument,

        [Parameter(Mandatory)][string]$DestinationPath
    )
    $json = ConvertTo-Json -Depth 30 -Compress -InputObject $OrderedDocument

    [string] $directory = Split-Path -Parent $DestinationPath
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($DestinationPath, $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "JSON written to $DestinationPath"
}

function Test-ApiReachable {
    param([string] $Base, [hashtable] $Headers)
    try {
        $splat = @{
            Uri                = "$Base/health/live"
            Method             = "Get"
            TimeoutSec         = 5
            SkipHttpErrorCheck = $true
        }

        if ($Headers.Count -gt 0) { $splat.Headers = $Headers }

        $r = Invoke-WebRequest @splat
        return ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500)
    }
    catch {
        return $false
    }
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

$resolvedBase = Resolve-BaseUrl -Candidate $BaseUrl
$headersPlain = Build-Headers -IncludePilotRealMode $false
$headersReal  = Build-Headers -IncludePilotRealMode $true

Write-Host "ArchLucid real-mode E2E benchmark"
Write-Host "  Base URL : $resolvedBase"
Write-Host "  Timeout  : $TimeoutSeconds s"
Write-Host ""

# Graceful fallback when the API is not reachable
if (-not (Test-ApiReachable -Base $resolvedBase -Headers $headersPlain)) {
    $fallback = [ordered]@{
        schemaVersion = "1.0.0"
        kind          = "archlucid.benchmark.realModeE2e.v1"
        status        = "api_unreachable"
        environment   = Get-BenchmarkEnvironmentBlock -BaseUrlResolved $resolvedBase `
            -TimeoutSec $TimeoutSeconds -PollSec $PollIntervalSeconds
        error = [ordered]@{
            message = "ArchLucid API is not reachable at $resolvedBase. Start the real-AOAI stack first (see docs/library/FIRST_REAL_VALUE.md)."
        }
        meta = [ordered]@{
            timestampUtc = [DateTimeOffset]::UtcNow.ToString("o")
        }
    }

    $jsonOutFallback = ConvertTo-Json -Depth 30 -Compress -InputObject $fallback
    Write-Warning $fallback.error.message
    Write-Host $jsonOutFallback

    [System.Collections.ArrayList]$destinationList = @()

    if (-not $SkipArtifact) {
        [void]$destinationList.Add($ArtifactPath)
    }

    if ($OutputFile) {
        [void]$destinationList.Add($OutputFile)
    }

    foreach ($destination in (($destinationList.ToArray()) | Select-Object -Unique)) {
        Write-StableBenchmarkJson -OrderedDocument $fallback -DestinationPath $destination
    }

    exit 0
}

$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

# --- Phase 1: Create -------------------------------------------------------
Write-Host "Phase 1/3: Creating architecture request..."
$requestId = [guid]::NewGuid().ToString("N")
$createBody = @{
    requestId            = $requestId
    systemName           = "Contoso Retail"
    description          = (
        "Contoso Retail: Modernize a small retail web application running on Azure. " +
        "The system needs scalable web tiers, a SQL backend, and basic compliance with PCI DSS. " +
        "Optimize for cost in a single region with private endpoints for storage and a managed identity for the application."
    )
    environment          = "prod"
    cloudProvider        = "Azure"
    constraints          = @("single-region", "low-cost")
    requiredCapabilities = @("web", "sql", "monitoring")
    assumptions          = @("No existing infrastructure to reuse")
}

$swCreate = [System.Diagnostics.Stopwatch]::StartNew()
$createUri = "$resolvedBase/v1/architecture/request"
$resCreate = $null

try {
    $resCreate = Invoke-JsonPost -Uri $createUri -Body $createBody -Headers $headersPlain
}
catch {
    Write-Error "POST $createUri failed: $($_.Exception.Message)"
    exit 1
}

$swCreate.Stop()

if ($resCreate.StatusCode -ne 201 -and $resCreate.StatusCode -ne 200) {
    Write-Error "POST $createUri returned HTTP $($resCreate.StatusCode). Body: $($resCreate.Content)"
    exit 1
}

$createPayload = $resCreate.Content | ConvertFrom-Json
$runId = $createPayload.run.runId

if ([string]::IsNullOrWhiteSpace($runId)) {
    Write-Error "Create response did not include run.runId. Body: $($resCreate.Content)"
    exit 1
}

$createMs = [math]::Round($swCreate.Elapsed.TotalMilliseconds, 2)
Write-Host "  RunId    : $runId"
Write-Host "  Create   : $createMs ms"

# --- Phase 2: Execute + Poll -----------------------------------------------
Write-Host "Phase 2/3: Executing (real AOAI) and polling for ReadyForCommit..."
$swExecute = [System.Diagnostics.Stopwatch]::StartNew()

$executeUri = "$resolvedBase/v1/architecture/run/$([uri]::EscapeDataString($runId))/execute"

try {
    $null = Invoke-JsonPost -Uri $executeUri -Body $null -Headers $headersReal
}
catch {
    Write-Host "  (Note: explicit POST /execute did not succeed; relying on background loop.)"
}

$pollUri = "$resolvedBase/v1/architecture/run/$([uri]::EscapeDataString($runId))"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$lastStatus = $null

while ((Get-Date) -lt $deadline) {
    try {
        $splat = @{
            Uri                = $pollUri
            Method             = "Get"
            Accept             = "application/json"
            SkipHttpErrorCheck = $true
        }

        if ($headersPlain.Count -gt 0) { $splat.Headers = $headersPlain }

        $resGet = Invoke-WebRequest @splat
    }
    catch {
        Write-Error "Poll GET $pollUri failed: $($_.Exception.Message)"
        exit 1
    }

    if ($resGet.StatusCode -ne 200) {
        Write-Error "Poll GET $pollUri returned HTTP $($resGet.StatusCode). Body: $($resGet.Content)"
        exit 1
    }

    $detail = $resGet.Content | ConvertFrom-Json
    $lastStatus = [string] $detail.run.status

    if ($lastStatus -eq "ReadyForCommit") { break }

    if ($lastStatus -eq "Committed") {
        Write-Error "Run $runId is already Committed before POST /commit (unexpected)."
        exit 1
    }

    if ($lastStatus -eq "Failed") {
        Write-Error "Run $runId entered Failed status. Check agent execution logs on the API host."
        exit 1
    }

    Write-Host "  Status: $lastStatus (elapsed $([math]::Round($swExecute.Elapsed.TotalSeconds, 1)) s)"
    Start-Sleep -Seconds $PollIntervalSeconds
}

$swExecute.Stop()

if ($lastStatus -ne "ReadyForCommit") {
    $display = if ($null -eq $lastStatus) { "(unknown)" } else { $lastStatus }
    Write-Error "Timed out after $TimeoutSeconds s waiting for ReadyForCommit (lastStatus=$display, runId=$runId)."
    exit 1
}

$executeMs = [math]::Round($swExecute.Elapsed.TotalMilliseconds, 2)
Write-Host "  Execute  : $executeMs ms (includes polling)"

# --- Phase 3: Commit -------------------------------------------------------
Write-Host "Phase 3/3: Committing manifest..."
$swCommit = [System.Diagnostics.Stopwatch]::StartNew()
$commitUri = "$resolvedBase/v1/architecture/run/$([uri]::EscapeDataString($runId))/commit"

try {
    $resCommit = Invoke-JsonPost -Uri $commitUri -Body $null -Headers $headersPlain
}
catch {
    Write-Error "POST $commitUri failed: $($_.Exception.Message)"
    exit 1
}

$swCommit.Stop()

if ($resCommit.StatusCode -ne 200) {
    Write-Error "POST $commitUri returned HTTP $($resCommit.StatusCode). Body: $($resCommit.Content)"
    exit 1
}

$commitMs = [math]::Round($swCommit.Elapsed.TotalMilliseconds, 2)
$swTotal.Stop()
$totalMs = [math]::Round($swTotal.Elapsed.TotalMilliseconds, 2)

Write-Host "  Commit   : $commitMs ms"
Write-Host ""
Write-Host "Total E2E  : $totalMs ms ($([math]::Round($totalMs / 1000, 2)) s)"

# --- JSON summary (stable schema archlucid.benchmark.realModeE2e.v1) -----------------
$completed = [ordered]@{
    schemaVersion = "1.0.0"
    kind          = "archlucid.benchmark.realModeE2e.v1"
    status        = "completed"
    environment   = Get-BenchmarkEnvironmentBlock -BaseUrlResolved $resolvedBase `
        -TimeoutSec $TimeoutSeconds -PollSec $PollIntervalSeconds
    run = [ordered]@{
        runId         = [string]$runId
        executionMode = "Real"
        pilotRealHeaderApplied = $true
    }
    timings = [ordered]@{
        createMs  = $createMs
        executeMs = $executeMs
        commitMs  = $commitMs
        totalMs   = $totalMs
        totalSec  = [math]::Round($totalMs / 1000, 2)
    }
    targets = [ordered]@{
        totalWallClockUnderFiveMinutes = ($totalMs -le 300000)
    }
    meta = [ordered]@{
        timestampUtc = [DateTimeOffset]::UtcNow.ToString("o")
    }
}

$jsonOut = ConvertTo-Json -Depth 30 -Compress -InputObject $completed

[System.Collections.ArrayList]$completedDestinations = @()

if (-not $SkipArtifact) {
    [void]$completedDestinations.Add($ArtifactPath)
}

if ($OutputFile) {
    [void]$completedDestinations.Add($OutputFile)
}

foreach ($destination in (($completedDestinations.ToArray()) | Select-Object -Unique)) {
    Write-StableBenchmarkJson -OrderedDocument $completed -DestinationPath $destination
}

Write-Host ""
$jsonOut
