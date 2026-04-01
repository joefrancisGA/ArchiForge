# End-to-end release smoke: Release build, core tests, optional UI, API+CLI+artifacts; optional -RunPlaywright for UI E2E.
# SQL required for the API E2E block unless -SkipE2E. See docs/RELEASE_SMOKE.md
param(
    [string] $SqlConnectionString = '',
    [string] $ApiBaseUrl = 'http://localhost:5128',
    [switch] $SkipE2E,
    [switch] $SkipUi,
    [switch] $FullCore,
    [switch] $RunPlaywright
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path (Join-Path $root 'scripts') 'OperatorDiagnostics.ps1')

# Prefer npm.cmd on Windows under StrictMode (npm.ps1 can throw on $MyInvocation.Statement).
$releaseSmokeNpm = if (Get-Command npm.cmd -ErrorAction SilentlyContinue) { 'npm.cmd' } else { 'npm' }
$sln = Join-Path $root 'ArchiForge.sln'
$apiProj = Join-Path $root 'ArchiForge.Api\ArchiForge.Api.csproj'
$cliProj = Join-Path $root 'ArchiForge.Cli\ArchiForge.Cli.csproj'

$savedConn = $env:ConnectionStrings__ArchiForge
$savedApiUrl = $env:ARCHIFORGE_API_URL
$apiProc = $null
$tempRoot = $null

function Restore-Env
{
    if ($null -eq $savedConn) { Remove-Item Env:\ConnectionStrings__ArchiForge -ErrorAction SilentlyContinue }
    else { $env:ConnectionStrings__ArchiForge = $savedConn }

    if ($null -eq $savedApiUrl) { Remove-Item Env:\ARCHIFORGE_API_URL -ErrorAction SilentlyContinue }
    else { $env:ARCHIFORGE_API_URL = $savedApiUrl }
}

function Invoke-ReleaseSmokePlaywrightWhenRequested
{
    param(
        [string] $RepoRoot,
        [switch] $Requested,
        [switch] $UiSkipped
    )

    if (-not $Requested) { return }

    $uiRoot = Join-Path $RepoRoot 'archiforge-ui'
    $node = Get-Command node -ErrorAction SilentlyContinue

    if ($null -eq $node) {
        Write-OperatorFailureTriage -Stage 'Playwright E2E' -Category 'Misconfiguration' `
            -Details @('-RunPlaywright requires Node.js on PATH.') `
            -NextSteps @('Install Node 22+ or omit -RunPlaywright')
        exit 1
    }

    Write-Host ''
    Write-Host '=== Playwright E2E (opt-in: -RunPlaywright) ===' -ForegroundColor Cyan

    Push-Location $uiRoot
    try
    {
        if ($UiSkipped -or -not (Test-Path (Join-Path $uiRoot 'node_modules')))
        {
            Write-Host 'Installing UI dependencies (npm ci) for Playwright...'
            & $releaseSmokeNpm ci
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }

        $savedCi = $env:CI
        $env:CI = '1'
        try
        {
            & $releaseSmokeNpm run test:e2e
            if ($LASTEXITCODE -ne 0) {
                Write-OperatorFailureTriage -Stage 'Playwright E2E (-RunPlaywright)' -Category 'PlaywrightFailure' `
                    -Details @("npm run test:e2e exited $LASTEXITCODE (see Playwright output above).") `
                    -NextSteps @(
                    'cd archiforge-ui; npx playwright install',
                    'archiforge-ui/docs/TESTING_AND_TROUBLESHOOTING.md — section 8',
                    'Ensure port 3000 free for test webServer'
                )
                exit $LASTEXITCODE
            }
        }
        finally
        {
            if ($null -eq $savedCi) { Remove-Item Env:\CI -ErrorAction SilentlyContinue }
            else { $env:CI = $savedCi }
        }
    }
    finally
    {
        Pop-Location
    }
}

try
{
    $cs = $SqlConnectionString
    if ([string]::IsNullOrWhiteSpace($cs)) { $cs = $env:ARCHIFORGE_SMOKE_SQL }
    if ([string]::IsNullOrWhiteSpace($cs)) { $cs = $env:ConnectionStrings__ArchiForge }

    Write-OperatorPhaseHeader -Title 'Release build' -Step 1 -Total 6
    & (Join-Path $root 'build-release.ps1')
    if ($LASTEXITCODE -ne 0) {
        Write-OperatorFailureTriage -Stage '1/6 Release build' -Category 'BuildOrRestoreFailure' `
            -Details @('build-release.ps1 exited non-zero.') `
            -NextSteps @('Run: .\build-release.ps1', 'Then: dotnet build ArchiForge.sln -c Release')
        exit $LASTEXITCODE
    }

    Write-OperatorPhaseHeader -Title 'Fast core tests (Release)' -Step 2 -Total 6
    dotnet test $sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"
    if ($LASTEXITCODE -ne 0) {
        Write-OperatorFailureTriage -Stage '2/6 Fast core tests' -Category 'TestFailure' `
            -Details @('First failing test is listed above in the test log.') `
            -NextSteps @(
            'dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"',
            'See docs/TEST_STRUCTURE.md for Suite/Core vs Integration'
        )
        exit $LASTEXITCODE
    }

    if ($FullCore)
    {
        Write-Host ''
        Write-Host '=== [2b/6] Full Core suite (optional; may require SQL) ===' -ForegroundColor Cyan
        dotnet test $sln -c Release --no-build --filter "Suite=Core"
        if ($LASTEXITCODE -ne 0) {
            Write-OperatorFailureTriage -Stage '2b/6 Full Core suite' -Category 'TestFailure' `
                -Details @('-FullCore includes integration-style tests; failures often need SQL or local services.') `
                -NextSteps @(
                'Re-run without -FullCore to isolate E2E smoke, or fix SQL per docs/BUILD.md',
                'dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core"'
            )
            exit $LASTEXITCODE
        }
    }

    if (-not $SkipUi)
    {
        $node = Get-Command node -ErrorAction SilentlyContinue
        if ($null -ne $node)
        {
            Write-OperatorPhaseHeader -Title 'Operator UI — Vitest' -Step 3 -Total 6
            $uiRoot = Join-Path $root 'archiforge-ui'
            Push-Location $uiRoot
            & $releaseSmokeNpm ci
            if ($LASTEXITCODE -ne 0) {
                Pop-Location
                Write-OperatorFailureTriage -Stage '3/6 UI Vitest' -Category 'NpmCiFailure' `
                    -Details @('npm ci failed in archiforge-ui.') `
                    -NextSteps @('cd archiforge-ui; npm ci', 'Or: .\release-smoke.ps1 -SkipUi')
                exit $LASTEXITCODE
            }
            & $releaseSmokeNpm run test
            if ($LASTEXITCODE -ne 0) {
                Pop-Location
                Write-OperatorFailureTriage -Stage '3/6 UI Vitest' -Category 'VitestFailure' `
                    -Details @('Vitest failed — file names above.') `
                    -NextSteps @('cd archiforge-ui; npm run test', 'Or: .\release-smoke.ps1 -SkipUi')
                exit $LASTEXITCODE
            }

            Write-OperatorPhaseHeader -Title 'Operator UI — production build' -Step 4 -Total 6
            & $releaseSmokeNpm run build
            Pop-Location
            if ($LASTEXITCODE -ne 0) {
                Write-OperatorFailureTriage -Stage '4/6 UI production build' -Category 'NextBuildFailure' `
                    -Details @('next build / npm run build failed.') `
                    -NextSteps @('cd archiforge-ui; npm run build', 'Or: .\release-smoke.ps1 -SkipUi')
                exit $LASTEXITCODE
            }
        }
        else
        {
            Write-Warning 'Node.js not on PATH; skipped UI Vitest and next build.'
        }
    }
    else
    {
        Write-Host ''
        Write-Host '=== [3-4/6] Skipped UI (-SkipUi) ===' -ForegroundColor DarkGray
    }

    if ($SkipE2E)
    {
        Write-Host '=== 5-6/6 Skipped E2E API+CLI (-SkipE2E) ==='
        if ($FullCore)
        {
            Write-Host 'Release smoke finished (build + fast core + full Core suite).'
        }
        else
        {
            Write-Host 'Release smoke finished (build + fast core tests).'
        }

        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($cs))
    {
        Write-OperatorFailureTriage -Stage '5/6 E2E API block (not started)' -Category 'Misconfiguration' `
            -Details @('No SQL connection string resolved for the temporary API process.') `
            -NextSteps @(
            'Set env: $env:ARCHIFORGE_SMOKE_SQL = ''Server=...;Database=...;...''',
            'Or pass: -SqlConnectionString ''...''',
            'Or set ConnectionStrings__ArchiForge in the shell',
            'CI / agents without SQL: .\release-smoke.ps1 -SkipE2E'
        )
        exit 1
    }

    Write-OperatorPhaseHeader -Title 'Start API (Release), wait for /health/ready, CLI quick run' -Step 5 -Total 6
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("archiforge-smoke-" + (Get-Date -Format 'yyyyMMddHHmmss'))
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

    $env:ConnectionStrings__ArchiForge = $cs
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    $apiProc = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run',
        '--project', $apiProj,
        '-c', 'Release',
        '--no-build',
        '--launch-profile', 'http'
    ) -WorkingDirectory $root -PassThru -WindowStyle Hidden

    if ($null -eq $apiProc)
    {
        Write-OperatorFailureTriage -Stage '5/6 Start API' -Category 'ProcessStartFailure' `
            -Details @('Start-Process did not return a handle for dotnet run ArchiForge.Api.') `
            -NextSteps @('Verify dotnet on PATH', 'Run manually: dotnet run --project ArchiForge.Api -c Release --launch-profile http')
        exit 1
    }

    $ready = $false
    $readyUrl = $ApiBaseUrl.TrimEnd('/') + '/health/ready'

    for ($i = 0; $i -lt 120; $i++) {
        if ($apiProc.HasExited) {
            Write-OperatorFailureTriage -Stage '5/6 API readiness' -Category 'ApiProcessExitedEarly' `
                -Details @(
                "The API process exited before /health/ready returned 200 (exit code hint: $($apiProc.ExitCode)).",
                'This script starts the API hidden — stdout/stderr are not shown here.'
            ) `
                -NextSteps @(
                'Verify SQL: migrations, firewall, TrustServerCertificate, correct database name',
                "Confirm nothing else is bound to $($ApiBaseUrl) (or pass -ApiBaseUrl)",
                'Reproduce in a visible window: $env:ConnectionStrings__ArchiForge = ''...''; dotnet run --project ArchiForge.Api -c Release --launch-profile http',
                'CLI: dotnet run --project ArchiForge.Cli -- doctor   (with API up)',
                'See docs/RELEASE_SMOKE.md — Troubleshooting'
            )
            exit 1
        }

        $probe = Get-ArchiForgeHttpProbe -Uri $readyUrl -TimeoutSec 2

        if ($probe.Ok -and $probe.StatusCode -eq 200) {
            $ready = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if (-not $ready) {
        Write-OperatorFailureTriage -Stage '5/6 API readiness' -Category 'ReadinessTimeout' `
            -Details @(
            'GET /health/ready did not return HTTP 200 within 120s (first blocking gate for E2E).',
            "Target: $readyUrl"
        ) `
            -NextSteps @(
            'Inspect failing health checks below (first unhealthy entry is the usual root cause).',
            'dotnet run --project ArchiForge.Cli -- doctor',
            'docs/TROUBLESHOOTING.md — SQL, port 5128, ArchiForge:StorageProvider',
            'Pilot misconfig: wrong connection string or SQL unreachable from this machine'
        )
        Write-ArchiForgeReadinessTimeoutDiagnostics -ApiBaseUrl $ApiBaseUrl
        exit 1
    }

    $liveUrl = $ApiBaseUrl.TrimEnd('/') + '/health/live'
    $liveProbe = Get-ArchiForgeHttpProbe -Uri $liveUrl -TimeoutSec 8

    if (-not $liveProbe.Ok -or $liveProbe.StatusCode -ne 200) {
        Write-OperatorFailureTriage -Stage '5/6 Liveness after readiness' -Category 'LivenessFailure' `
            -Details @("GET $liveUrl returned HTTP $($liveProbe.StatusCode) (expected 200).") `
            -NextSteps @('If readiness passed but live failed, capture API logs and open an issue — unusual ordering.')
        exit 1
    }

    Push-Location $tempRoot
    try
    {
        dotnet run --project $cliProj -- new ArchiForgeSmokeRc
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally
    {
        Pop-Location
    }

    $projDir = Join-Path $tempRoot 'ArchiForgeSmokeRc'
    if (-not (Test-Path $projDir)) {
        Write-OperatorFailureTriage -Stage '5/6 CLI new' -Category 'ScaffoldLayoutMissing' `
            -Details @("Expected project folder at $projDir after archiforge new.") `
            -NextSteps @('Re-run new in an empty folder', 'Check CLI new command output above')
        exit 1
    }

    $env:ARCHIFORGE_API_URL = $ApiBaseUrl
    Push-Location $projDir
    try
    {
        dotnet run --project $cliProj -- run --quick
        if ($LASTEXITCODE -ne 0) {
            Write-OperatorFailureTriage -Stage '5/6 CLI run --quick' -Category 'CliRunFailure' `
                -Details @('run --quick failed — stderr above often includes HTTP status and Next: hints.') `
                -NextSteps @(
                'Confirm API still up and ARCHIFORGE_API_URL matches smoke API',
                'API must see Development environment for seed (script sets ASPNETCORE_ENVIRONMENT=Development for child API)',
                'dotnet run --project ArchiForge.Cli -- doctor'
            )
            exit $LASTEXITCODE
        }
    }
    finally
    {
        Pop-Location
    }

    $summaryPath = Join-Path (Join-Path $projDir 'outputs') 'run-summary.json'
    if (-not (Test-Path $summaryPath)) {
        Write-OperatorFailureTriage -Stage '6/6 Artifact verification' -Category 'MissingRunSummary' `
            -Details @("Expected outputs\run-summary.json at $summaryPath") `
            -NextSteps @('Re-run run --quick with API logging visible', 'Check CLI Next: hints from step 5')
        exit 1
    }

    $summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
    $runId = $summary.runId
    if ([string]::IsNullOrWhiteSpace($runId)) {
        Write-OperatorFailureTriage -Stage '6/6 Artifact verification' -Category 'InvalidRunSummary' `
            -Details @('run-summary.json exists but runId is empty.') `
            -NextSteps @('Inspect run-summary.json', 'Re-run CLI run --quick')
        exit 1
    }

    Write-OperatorPhaseHeader -Title "Verify manifest + synthesized artifacts (run $runId)" -Step 6 -Total 6
    $runJson = Invoke-RestMethod -Uri ($ApiBaseUrl.TrimEnd('/') + '/v1/architecture/run/' + $runId) -Method Get
    $manifestId = $runJson.run.goldenManifestId
    if ([string]::IsNullOrWhiteSpace($manifestId)) {
        Write-OperatorFailureTriage -Stage '6/6 Artifact verification' -Category 'MissingGoldenManifest' `
            -Details @("Run $runId has no goldenManifestId in GET /v1/architecture/run/{runId}.") `
            -NextSteps @(
            'Check API logs for commit/persistence errors for this runId',
            'Verify SQL persistence and Development seed path'
        )
        exit 1
    }

    try {
        $artifacts = Invoke-RestMethod -Uri ($ApiBaseUrl.TrimEnd('/') + '/api/artifacts/manifests/' + $manifestId) -Method Get
    }
    catch {
        Write-OperatorFailureTriage -Stage '6/6 Artifact verification' -Category 'ArtifactsApiFailure' `
            -Details @("GET /api/artifacts/manifests/$manifestId failed: $($_.Exception.Message)") `
            -NextSteps @('curl or browser the same URL with API up', 'Check run and manifest IDs in API logs')
        exit 1
    }

    $artifactCount = @($artifacts).Count
    if ($artifactCount -lt 1) {
        Write-OperatorFailureTriage -Stage '6/6 Artifact verification' -Category 'NoSynthesizedArtifacts' `
            -Details @("Manifest $manifestId returned $artifactCount artifact(s); expected >= 1.") `
            -NextSteps @(
            'Synthesis or persistence regression — search API logs for this manifestId',
            'docs/RELEASE_SMOKE.md — Zero artifacts'
        )
        exit 1
    }

    Write-Host "Smoke OK: $artifactCount artifact(s) listed for manifest $manifestId." -ForegroundColor Green
    Invoke-ReleaseSmokePlaywrightWhenRequested -RepoRoot $root -Requested:$RunPlaywright -UiSkipped:$SkipUi
    Write-Host 'Release smoke finished successfully.'
    exit 0
}
finally
{
    if ($null -ne $apiProc -and -not $apiProc.HasExited)
    {
        Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue
    }

    Restore-Env

    if ($null -ne $tempRoot -and (Test-Path $tempRoot))
    {
        Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
