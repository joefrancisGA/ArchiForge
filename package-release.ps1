# Publish API (+ optional operator UI production build) into artifacts/release/. See docs/RELEASE_LOCAL.md
param(
    [switch] $SkipUiBuild,
    [switch] $SkipChecksums
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

& (Join-Path $root 'build-release.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$apiOut = Join-Path $root 'artifacts/release/api'
New-Item -ItemType Directory -Force -Path $apiOut | Out-Null
dotnet publish (Join-Path $root 'ArchLucid.Api/ArchLucid.Api.csproj') -c Release -o $apiOut --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$uiProductionBuildIncluded = $false

if (-not $SkipUiBuild) {
    $node = Get-Command node -ErrorAction SilentlyContinue

    if ($null -ne $node) {
        $uiRoot = Join-Path $root 'archlucid-ui'
        Set-Location $uiRoot
        npm ci
        if ($LASTEXITCODE -ne 0) { Set-Location $root; exit $LASTEXITCODE }
        npm run build
        Set-Location $root
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $uiProductionBuildIncluded = $true
    }
    else {
        Write-Warning 'Node.js not on PATH; skipped archlucid-ui production build. Use -SkipUiBuild to silence this warning.'
    }
}

$writer = Join-Path (Join-Path $root 'scripts') 'Write-ReleasePackageArtifacts.ps1'
try {
    & $writer `
        -RepoRoot $root `
        -ApiPublishDirectory $apiOut `
        -UiProductionBuildIncluded:$uiProductionBuildIncluded `
        -SkipChecksums:$SkipChecksums
}
catch {
    Write-Error $_
    exit 1
}

Write-Host ''
Write-Host "Release package: API published to $apiOut"
Write-Host 'Run the API: dotnet ArchLucid.Api.dll (from that folder; requires .NET 10 runtime). See artifacts/release/PACKAGE-HANDOFF.txt and docs/RELEASE_LOCAL.md'
exit 0
