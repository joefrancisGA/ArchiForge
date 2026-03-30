# Pilot / RC gate: Release build, fast-core tests in Release, optional UI Vitest. See docs/RELEASE_LOCAL.md
param(
    [switch] $SkipUi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host '=== Release build (ArchiForge.sln, -c Release) ==='
& (Join-Path $root 'build-release.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== Fast core tests (Release, no rebuild) ==='
dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipUi)
{
    $node = Get-Command node -ErrorAction SilentlyContinue
    if ($null -ne $node)
    {
        Write-Host '=== Operator UI unit tests (Vitest) ==='
        $uiRoot = Join-Path $root 'archiforge-ui'
        Set-Location $uiRoot
        npm ci
        if ($LASTEXITCODE -ne 0) { Set-Location $root; exit $LASTEXITCODE }
        npm run test
        Set-Location $root
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    else
    {
        Write-Warning 'Node.js not on PATH; skipped UI unit tests. Use -SkipUi for a quiet skip, or install Node 22+.'
    }
}

Write-Host '=== Readiness check finished successfully ==='
exit 0
