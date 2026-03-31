# End-to-end release smoke: Release build, core-tier tests, optional UI build, API+CLI sample run + artifact check.
# Requires SQL connection string for the E2E block (unless -SkipE2E). See docs/RELEASE_SMOKE.md
param(
    [string] $SqlConnectionString = '',
    [string] $ApiBaseUrl = 'http://localhost:5128',
    [switch] $SkipE2E,
    [switch] $SkipUi,
    [switch] $FullCore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
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

try
{
    $cs = $SqlConnectionString
    if ([string]::IsNullOrWhiteSpace($cs)) { $cs = $env:ARCHIFORGE_SMOKE_SQL }
    if ([string]::IsNullOrWhiteSpace($cs)) { $cs = $env:ConnectionStrings__ArchiForge }

    Write-Host '=== 1/6 Release build ==='
    & (Join-Path $root 'build-release.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host '=== 2/6 Fast core tests (Release) ==='
    dotnet test $sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if ($FullCore)
    {
        Write-Host '=== 2b Full Core suite (Release) — may require SQL for integration tests ==='
        dotnet test $sln -c Release --no-build --filter "Suite=Core"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if (-not $SkipUi)
    {
        $node = Get-Command node -ErrorAction SilentlyContinue
        if ($null -ne $node)
        {
            Write-Host '=== 3/6 Operator UI — Vitest ==='
            $uiRoot = Join-Path $root 'archiforge-ui'
            Push-Location $uiRoot
            npm ci
            if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }
            npm run test
            if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

            Write-Host '=== 4/6 Operator UI — production build ==='
            npm run build
            Pop-Location
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }
        else
        {
            Write-Warning 'Node.js not on PATH; skipped UI Vitest and next build.'
        }
    }
    else
    {
        Write-Host '=== 3-4/6 Skipped UI (-SkipUi) ==='
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
        Write-Host 'E2E requires a SQL connection string. Set ARCHIFORGE_SMOKE_SQL, pass -SqlConnectionString, or set ConnectionStrings__ArchiForge. Or use -SkipE2E.' -ForegroundColor Red
        exit 1
    }

    Write-Host '=== 5/6 Start API (Release), health/ready, CLI quick run ==='
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
        Write-Host 'Failed to start ArchiForge.Api process.' -ForegroundColor Red
        exit 1
    }

    $ready = $false
    for ($i = 0; $i -lt 120; $i++)
    {
        try
        {
            $r = Invoke-WebRequest -Uri ($ApiBaseUrl.TrimEnd('/') + '/health/ready') -UseBasicParsing -TimeoutSec 2
            if ($r.StatusCode -eq 200)
            {
                $ready = $true
                break
            }
        }
        catch
        {
            if ($apiProc.HasExited)
            {
                Write-Host 'API process exited before readiness. Check SQL connection string and logs above.' -ForegroundColor Red
                exit 1
            }
        }

        Start-Sleep -Seconds 1
    }

    if (-not $ready)
    {
        Write-Error 'Timed out waiting for GET /health/ready (120s).'
        exit 1
    }

    $live = Invoke-WebRequest -Uri ($ApiBaseUrl.TrimEnd('/') + '/health/live') -UseBasicParsing -TimeoutSec 5
    if ($live.StatusCode -ne 200)
    {
        Write-Host 'GET /health/live did not return 200.' -ForegroundColor Red
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
    if (-not (Test-Path $projDir))
    {
        Write-Host "Expected scaffold at $projDir" -ForegroundColor Red
        exit 1
    }

    $env:ARCHIFORGE_API_URL = $ApiBaseUrl
    Push-Location $projDir
    try
    {
        dotnet run --project $cliProj -- run --quick
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally
    {
        Pop-Location
    }

    $summaryPath = Join-Path (Join-Path $projDir 'outputs') 'run-summary.json'
    if (-not (Test-Path $summaryPath))
    {
        Write-Host "run-summary.json missing at $summaryPath" -ForegroundColor Red
        exit 1
    }

    $summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
    $runId = $summary.runId
    if ([string]::IsNullOrWhiteSpace($runId))
    {
        Write-Host 'run-summary.json has no runId.' -ForegroundColor Red
        exit 1
    }

    Write-Host "=== 6/6 Verify manifest + synthesized artifacts (run $runId) ==="
    $runJson = Invoke-RestMethod -Uri ($ApiBaseUrl.TrimEnd('/') + '/v1/architecture/run/' + $runId) -Method Get
    $manifestId = $runJson.run.goldenManifestId
    if ([string]::IsNullOrWhiteSpace($manifestId))
    {
        Write-Host 'Run has no goldenManifestId; commit did not persist manifest reference.' -ForegroundColor Red
        exit 1
    }

    $artifacts = Invoke-RestMethod -Uri ($ApiBaseUrl.TrimEnd('/') + '/api/artifacts/manifests/' + $manifestId) -Method Get
    $artifactCount = @($artifacts).Count
    if ($artifactCount -lt 1)
    {
        Write-Host "Expected at least one synthesized artifact for manifest $manifestId; API returned $artifactCount." -ForegroundColor Red
        exit 1
    }

    Write-Host "Smoke OK: $artifactCount artifact(s) listed for manifest $manifestId."
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
