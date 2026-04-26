# V1 RC drill: HTTP checks against a running ArchLucid API (two runs, compare, replay, export, diagnostics).
# Does not build, deploy, or start the API. See docs/library/V1_RC_DRILL.md
param(
    [string] $ApiBaseUrl = 'http://localhost:5128',
    [switch] $SkipSupportBundle,
    [switch] $SkipDoctor
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path (Join-Path $root 'scripts') 'OperatorDiagnostics.ps1')

$cliProj = Join-Path $root 'ArchLucid.Cli\ArchLucid.Cli.csproj'
$base = $ApiBaseUrl.TrimEnd('/')
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss', [System.Globalization.CultureInfo]::InvariantCulture)

$script:savedApiUrl = $env:ARCHLUCID_API_URL

function Restore-DrillEnv
{
    if ($null -eq $script:savedApiUrl) { Remove-Item Env:\ARCHLUCID_API_URL -ErrorAction SilentlyContinue }
    else { $env:ARCHLUCID_API_URL = $script:savedApiUrl }
}

function Invoke-DrillRestFailure
{
    param(
        [string] $Stage,
        [System.Management.Automation.ErrorRecord] $ErrorRecord
    )

    $msg = $ErrorRecord.Exception.Message

    if ($ErrorRecord.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($ErrorRecord.ErrorDetails.Message)) {
        $msg = $ErrorRecord.ErrorDetails.Message
    }

    Write-OperatorFailureTriage -Stage $Stage -Category 'HttpFailure' `
        -Details @($msg) `
        -NextSteps @(
        'Confirm API is up and -ApiBaseUrl is correct',
        'docs/library/V1_RC_DRILL.md - prerequisites (auth, SQL, DevelopmentBypass vs JWT)',
        'dotnet run --project ArchLucid.Cli -- doctor'
    )
}

function New-V1RcDrillCommittedRun
{
    param(
        [string] $RequestIdSuffix,
        [string] $SystemName
    )

    $requestId = "v1-rc-drill-$RequestIdSuffix-$stamp"
    $description = "RC drill ($RequestIdSuffix) - design a small internal API with basic security and observability for release validation."

    $bodyObj = [ordered]@{
        requestId            = $requestId
        systemName           = $SystemName
        description          = $description
        environment          = 'dev'
        cloudProvider        = 'Azure'
        constraints          = @('Use managed identity where possible')
        requiredCapabilities = @('HTTPS')
    }

    $json = $bodyObj | ConvertTo-Json -Compress -Depth 8

    try {
        $created = Invoke-RestMethod -Uri "$base/v1/architecture/request" -Method Post -Body $json -ContentType 'application/json'
    }
    catch {
        Invoke-DrillRestFailure -Stage "Create run ($RequestIdSuffix)" -ErrorRecord $_
        exit 1
    }

    $runId = [string] $created.run.runId

    if ([string]::IsNullOrWhiteSpace($runId)) {
        Write-OperatorFailureTriage -Stage "Create run ($RequestIdSuffix)" -Category 'InvalidResponse' `
            -Details @('POST /v1/architecture/request returned no run.runId.') `
            -NextSteps @('Inspect API response body and logs')
        exit 1
    }

    try {
        $null = Invoke-RestMethod -Uri "$base/v1/architecture/run/$runId/execute" -Method Post
    }
    catch {
        Invoke-DrillRestFailure -Stage "Execute run $runId ($RequestIdSuffix)" -ErrorRecord $_
        exit 1
    }

    try {
        $null = Invoke-RestMethod -Uri "$base/v1/architecture/run/$runId/commit" -Method Post
    }
    catch {
        Invoke-DrillRestFailure -Stage "Commit run $runId ($RequestIdSuffix)" -ErrorRecord $_
        exit 1
    }

    try {
        $detail = Invoke-RestMethod -Uri "$base/v1/architecture/run/$runId" -Method Get
    }
    catch {
        Invoke-DrillRestFailure -Stage "GET run $runId ($RequestIdSuffix)" -ErrorRecord $_
        exit 1
    }

    $golden = $detail.run.goldenManifestId

    if ($null -eq $golden -or [string]::IsNullOrWhiteSpace([string] $golden)) {
        Write-OperatorFailureTriage -Stage "Committed run $runId ($RequestIdSuffix)" -Category 'MissingGoldenManifest' `
            -Details @('run.goldenManifestId is null after commit.') `
            -NextSteps @('Check API logs for decisioning / persistence errors')
        exit 1
    }

    $manifestId = [string] $golden

    return [pscustomobject]@{
        RunId      = $runId
        ManifestId = $manifestId
    }
}

try
{
    $script:total = 9
    $script:step = 0

    function Write-DrillPhase([string] $title)
    {
        $script:step++
        Write-OperatorPhaseHeader -Title $title -Step $script:step -Total $script:total
    }

    Write-DrillPhase 'Health + version (live, ready, /version)'

    $liveProbe = Get-ArchLucidHttpProbe -Uri "$base/health/live" -TimeoutSec 15

    if (-not $liveProbe.Ok -or $liveProbe.StatusCode -ne 200) {
        Write-OperatorFailureTriage -Stage 'GET /health/live' -Category 'LivenessFailure' `
            -Details @("HTTP $($liveProbe.StatusCode); $($liveProbe.Error)") `
            -NextSteps @('Start ArchLucid.Api', 'Verify -ApiBaseUrl')
        exit 1
    }

    $readyProbe = Get-ArchLucidHttpProbe -Uri "$base/health/ready" -TimeoutSec 30

    if (-not $readyProbe.Ok -or $readyProbe.StatusCode -ne 200) {
        Write-OperatorFailureTriage -Stage 'GET /health/ready' -Category 'ReadinessFailure' `
            -Details @("HTTP $($readyProbe.StatusCode); $($readyProbe.Error)") `
            -NextSteps @('Inspect readiness JSON', 'docs/TROUBLESHOOTING.md - SQL, storage, compliance pack')
        Write-ArchLucidReadinessTimeoutDiagnostics -ApiBaseUrl $ApiBaseUrl
        exit 1
    }

    try {
        $ver = Invoke-RestMethod -Uri "$base/version" -Method Get -TimeoutSec 15
    }
    catch {
        Invoke-DrillRestFailure -Stage 'GET /version' -ErrorRecord $_
        exit 1
    }

    if ($null -eq $ver.informationalVersion) {
        Write-Host 'Warning: /version JSON missing informationalVersion (unexpected).' -ForegroundColor Yellow
    }
    else {
        Write-Host "Version: $($ver.informationalVersion)  commit: $($ver.commitSha)" -ForegroundColor DarkGray
    }

    Write-DrillPhase 'Run A - request, execute, commit'
    $runA = New-V1RcDrillCommittedRun -RequestIdSuffix 'a' -SystemName 'RcDrillServiceA'

    Write-DrillPhase 'Run B - request, execute, commit'
    $runB = New-V1RcDrillCommittedRun -RequestIdSuffix 'b' -SystemName 'RcDrillServiceB'

    Write-DrillPhase "List artifacts for Run A manifest ($($runA.ManifestId))"

    try {
        $artifacts = Invoke-RestMethod -Uri "$base/v1/artifacts/manifests/$($runA.ManifestId)" -Method Get
    }
    catch {
        Invoke-DrillRestFailure -Stage 'GET /v1/artifacts/manifests/{manifestId}' -ErrorRecord $_
        exit 1
    }

    $artifactCount = @($artifacts).Count

    if ($artifactCount -lt 1) {
        Write-OperatorFailureTriage -Stage 'Artifact list (Run A)' -Category 'NoSynthesizedArtifacts' `
            -Details @("Expected >= 1 artifact descriptor; got $artifactCount.") `
            -NextSteps @('Check synthesis logs for manifest', 'docs/RELEASE_SMOKE.md - Zero artifacts')
        exit 1
    }

    Write-Host "Artifact descriptors (Run A): $artifactCount" -ForegroundColor DarkGray

    Write-DrillPhase 'Compare runs end-to-end (A vs B)'

    $pairUrl = "$base/v1/architecture/run/compare/end-to-end?leftRunId=$([uri]::EscapeDataString($runA.RunId))&rightRunId=$([uri]::EscapeDataString($runB.RunId))"

    try {
        $null = Invoke-RestMethod -Uri $pairUrl -Method Get
    }
    catch {
        Invoke-DrillRestFailure -Stage 'GET run/compare/end-to-end' -ErrorRecord $_
        exit 1
    }

    Write-DrillPhase 'Authority replay (ReconstructOnly) for Run A'

    $replayBody = (@{ runId = $runA.RunId; mode = 'ReconstructOnly' } | ConvertTo-Json -Compress)

    try {
        $replay = Invoke-RestMethod -Uri "$base/v1/authority/replay" -Method Post -Body $replayBody -ContentType 'application/json'
    }
    catch {
        Invoke-DrillRestFailure -Stage 'POST /v1/authority/replay' -ErrorRecord $_
        exit 1
    }

    if ($null -eq $replay.validation) {
        Write-Host 'Replay returned 200 but validation object missing (check API version).' -ForegroundColor Yellow
    }

    Write-DrillPhase 'Run export ZIP (Run A)'

    $zipPath = Join-Path ([System.IO.Path]::GetTempPath()) ("v1-rc-drill-export-$stamp.zip")

    try {
        Invoke-WebRequest -Uri "$base/v1/artifacts/runs/$($runA.RunId)/export" -OutFile $zipPath -UseBasicParsing -TimeoutSec 120
    }
    catch {
        Invoke-DrillRestFailure -Stage 'GET /v1/artifacts/runs/{runId}/export' -ErrorRecord $_
        exit 1
    }

    if (-not (Test-Path $zipPath)) {
        Write-OperatorFailureTriage -Stage 'Run export ZIP' -Category 'MissingFile' `
            -Details @("Expected file at $zipPath") `
            -NextSteps @('Retry curl; verify ExecuteAuthority / ReadAuthority for export route')
        exit 1
    }

    $len = (Get-Item $zipPath).Length

    if ($len -lt 64) {
        Write-OperatorFailureTriage -Stage 'Run export ZIP' -Category 'EmptyOrTinyZip' `
            -Details @("ZIP size $len bytes - unexpected.") `
            -NextSteps @('Open ZIP; check API logs')
        exit 1
    }

    Write-Host "Export saved: $zipPath ($len bytes)" -ForegroundColor DarkGray

    $env:ARCHLUCID_API_URL = $ApiBaseUrl

    if (-not $SkipDoctor) {
        Write-DrillPhase 'CLI doctor'

        Push-Location $root
        try {
            dotnet run --project $cliProj -- doctor
            if ($LASTEXITCODE -ne 0) {
                Write-OperatorFailureTriage -Stage 'CLI doctor' -Category 'CliExitNonZero' `
                    -Details @("dotnet doctor exited $LASTEXITCODE") `
                    -NextSteps @('Run doctor in a visible console', 'Confirm ARCHLUCID_API_URL')
                exit $LASTEXITCODE
            }
        }
        finally {
            Pop-Location
        }
    }
    else {
        $script:step++
        Write-Host ''
        Write-Host "=== [$($script:step)/$($script:total)] Skipped CLI doctor (-SkipDoctor) ===" -ForegroundColor DarkGray
    }

    if (-not $SkipSupportBundle) {
        Write-DrillPhase 'CLI support-bundle (--zip)'

        $bundleParent = Join-Path ([System.IO.Path]::GetTempPath()) "v1-rc-drill-bundle-$stamp"
        New-Item -ItemType Directory -Path $bundleParent -Force | Out-Null

        Push-Location $root
        try {
            dotnet run --project $cliProj -- support-bundle --zip --output $bundleParent
            if ($LASTEXITCODE -ne 0) {
                Write-OperatorFailureTriage -Stage 'CLI support-bundle' -Category 'CliExitNonZero' `
                    -Details @("support-bundle exited $LASTEXITCODE") `
                    -NextSteps @('Review stderr', 'docs/TROUBLESHOOTING.md')
                exit $LASTEXITCODE
            }
        }
        finally {
            Pop-Location
        }

        Write-Host "Support bundle parent: $bundleParent" -ForegroundColor DarkGray
    }
    else {
        $script:step++
        Write-Host ''
        Write-Host "=== [$($script:step)/$($script:total)] Skipped support-bundle (-SkipSupportBundle) ===" -ForegroundColor DarkGray
    }

    Write-Host ''
    Write-Host 'V1 RC drill completed successfully.' -ForegroundColor Green
    Write-Host "  Run A: $($runA.RunId)  manifest: $($runA.ManifestId)" -ForegroundColor DarkGray
    Write-Host "  Run B: $($runB.RunId)  manifest: $($runB.ManifestId)" -ForegroundColor DarkGray
    exit 0
}
finally
{
    Restore-DrillEnv
}
