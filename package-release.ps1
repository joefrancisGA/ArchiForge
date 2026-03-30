# Publish API (+ optional operator UI production build) into artifacts/release/. See docs/RELEASE_LOCAL.md
param(
    [switch] $SkipUiBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

& (Join-Path $root 'build-release.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$apiOut = Join-Path $root 'artifacts/release/api'
New-Item -ItemType Directory -Force -Path $apiOut | Out-Null
dotnet publish (Join-Path $root 'ArchiForge.Api/ArchiForge.Api.csproj') -c Release -o $apiOut --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipUiBuild)
{
    $node = Get-Command node -ErrorAction SilentlyContinue
    if ($null -ne $node)
    {
        $uiRoot = Join-Path $root 'archiforge-ui'
        Set-Location $uiRoot
        npm ci
        if ($LASTEXITCODE -ne 0) { Set-Location $root; exit $LASTEXITCODE }
        npm run build
        Set-Location $root
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    else
    {
        Write-Warning 'Node.js not on PATH; skipped archiforge-ui production build. Use -SkipUiBuild to silence this warning.'
    }
}

Write-Host "Release package: API published to $apiOut"
Write-Host "Run the API: dotnet ArchiForge.Api.dll (from that folder; requires .NET 10 runtime). See docs/RELEASE_LOCAL.md"
exit 0
