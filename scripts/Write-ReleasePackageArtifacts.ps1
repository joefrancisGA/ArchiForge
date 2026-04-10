# Emits operator-facing release metadata, manifest, checksums, and handoff summary under artifacts/release/.
# Called from package-release.ps1 / package-release.cmd after dotnet publish (and optional UI build).
param(
    [string] $RepoRoot,
    [string] $ApiPublishDirectory,
    [switch] $UiProductionBuildIncluded,
    [switch] $SkipChecksums
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    throw 'RepoRoot is required.'
}

if ([string]::IsNullOrWhiteSpace($ApiPublishDirectory)) {
    throw 'ApiPublishDirectory is required.'
}

$repo = (Resolve-Path -LiteralPath $RepoRoot).Path
$apiDir = (Resolve-Path -LiteralPath $ApiPublishDirectory).Path
$releaseRoot = [System.IO.Path]::GetDirectoryName($apiDir)

if (-not (Test-Path -LiteralPath $apiDir)) {
    throw "API publish directory does not exist: $ApiPublishDirectory"
}

$apiDll = Join-Path $apiDir 'ArchLucid.Api.dll'
$informationalVersion = 'unknown'
$assemblyVersion = 'unknown'
$fileVersion = 'unknown'

if (Test-Path -LiteralPath $apiDll) {
    try {
        $an = [System.Reflection.AssemblyName]::GetAssemblyName($apiDll)
        $assemblyVersion = $an.Version.ToString()
    }
    catch {
        # Leave default
    }

    try {
        $fvi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($apiDll)

        if (-not [string]::IsNullOrWhiteSpace($fvi.FileVersion)) {
            $fileVersion = $fvi.FileVersion.Trim()
        }

        if (-not [string]::IsNullOrWhiteSpace($fvi.ProductVersion)) {
            $informationalVersion = $fvi.ProductVersion.Trim()
        }
    }
    catch {
        # Leave defaults
    }
}

$commitSha = 'unknown'

try {
    Push-Location $repo
    $rev = git rev-parse HEAD 2>$null

    if (-not [string]::IsNullOrWhiteSpace($rev)) {
        $commitSha = $rev.Trim()
    }
}
catch {
    $commitSha = 'unknown'
}
finally {
    Pop-Location
}

$buildUtc = (Get-Date).ToUniversalTime().ToString('o')
$dotnetSdk = 'unknown'

try {
    $dv = dotnet --version 2>$null

    if (-not [string]::IsNullOrWhiteSpace($dv)) {
        $dotnetSdk = $dv.Trim()
    }
}
catch {
    # unknown
}

$packagerHost = $env:COMPUTERNAME

if ([string]::IsNullOrWhiteSpace($packagerHost)) {
    $packagerHost = $env:HOSTNAME
}

if ([string]::IsNullOrWhiteSpace($packagerHost)) {
    $packagerHost = 'unknown'
}

$metadata = [ordered]@{
    schemaVersion          = '1.1'
    packageKind            = 'ArchLucid.ReleaseCandidate'
    application            = 'ArchLucid.Api'
    informationalVersion   = $informationalVersion
    assemblyVersion        = $assemblyVersion
    fileVersion            = $fileVersion
    commitSha              = $commitSha
    buildTimestampUtc      = $buildUtc
    dotnetSdkVersion       = $dotnetSdk
    packagerHost           = $packagerHost
    apiPublishPathRelative = 'artifacts/release/api'
    uiProductionBuildIncluded = [bool] $UiProductionBuildIncluded
}

$metadataPath = Join-Path $releaseRoot 'metadata.json'
$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding utf8

# --- Enumerate published API files (deterministic order) ---
$apiFiles = @(Get-ChildItem -LiteralPath $apiDir -Recurse -File | Sort-Object { $_.FullName.ToLowerInvariant() })
$totalBytes = 0L
$fileEntries = [System.Collections.Generic.List[object]]::new()

foreach ($f in $apiFiles) {
    $len = $f.Length
    $totalBytes += $len
    $relFromApi = $f.FullName.Substring($apiDir.Length).TrimStart([char]'\', [char]'/')
    $relPath = 'api/' + ($relFromApi -replace '\\', '/')
    $fileEntries.Add([ordered]@{
            path      = $relPath
            sizeBytes = $len
        })
}

$manifest = [ordered]@{
    schemaVersion  = '1.0'
    packageKind    = 'ArchLucid.ReleaseHandoff'
    generatedAtUtc = $buildUtc
    summary        = [ordered]@{
        apiFileCount = $apiFiles.Count
        apiTotalBytes = $totalBytes
        uiProductionBuildIncluded = [bool] $UiProductionBuildIncluded
        checksumsSha256Generated = -not [bool] $SkipChecksums
    }
    layout         = [ordered]@{
        releaseRootRelative = 'artifacts/release'
        apiPublishRelative  = 'artifacts/release/api'
        operatorUiSourceRelative = 'archlucid-ui'
    }
    operatorUi     = [ordered]@{
        productionBuildIncluded = [bool] $UiProductionBuildIncluded
        note                    = 'Operator UI is built from archlucid-ui/ in the repo. Production output is under archlucid-ui/.next (not copied into artifacts/release).'
    }
    build          = [ordered]@{
        informationalVersion = $informationalVersion
        commitSha            = $commitSha
        assemblyVersion      = $assemblyVersion
        fileVersion          = $fileVersion
        dotnetSdkVersion     = $dotnetSdk
    }
    apiPublishFiles = $fileEntries
    companionFiles  = @(
        'metadata.json',
        'release-manifest.json',
        'checksums-sha256.txt',
        'PACKAGE-HANDOFF.txt'
    )
}

$manifestPath = Join-Path $releaseRoot 'release-manifest.json'
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding utf8

# --- SHA-256 listing (same path order as apiPublishFiles in release-manifest.json; lowercase hex) ---
$checksumPath = Join-Path $releaseRoot 'checksums-sha256.txt'

if (-not $SkipChecksums) {
    $checksumLines = [System.Collections.Generic.List[string]]::new()

    foreach ($f in $apiFiles) {
        $relFromApi = $f.FullName.Substring($apiDir.Length).TrimStart([char]'\', [char]'/')
        $relPath = 'api/' + ($relFromApi -replace '\\', '/')
        $hash = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $checksumLines.Add("$hash  $relPath")
    }

    $checksumLines -join [Environment]::NewLine | Set-Content -LiteralPath $checksumPath -Encoding utf8
}
else {
    Set-Content -LiteralPath $checksumPath -Encoding utf8 -Value '# Checksums skipped (-SkipChecksums). Re-run package-release.ps1 without -SkipChecksums to generate.'
}

# --- Plain-text handoff blurb ---
$handoffPath = Join-Path $releaseRoot 'PACKAGE-HANDOFF.txt'
$uiLine = if ($UiProductionBuildIncluded) {
    'Operator UI: production build was run in this session (see archlucid-ui/.next in the repo).'
}
else {
    'Operator UI: production build was not run this session (install Node or use package-release.ps1 without -SkipUiBuild).'
}

$checksumLine = if ($SkipChecksums) {
    'checksums-sha256.txt Placeholder only — package was built with -SkipChecksums; re-run without it for SHA-256 lines.'
}
else {
    'checksums-sha256.txt SHA-256 per file under api/ (order matches release-manifest.json; verify after copy).'
}

$handoff = @"
ArchLucid — release package handoff (RC)
==========================================

Build identity
  Informational version: $informationalVersion
  Commit:                $commitSha
  Packaged at (UTC):     $buildUtc

What is in artifacts/release/
  api/                 Published ArchLucid.Api (framework-dependent; needs .NET 10 runtime).
  metadata.json        Machine-readable build/version fields for support tickets.
  release-manifest.json File list + sizes + layout (for inventory / audits).
  $checksumLine
  PACKAGE-HANDOFF.txt  This file.

Run the API (example)
  cd artifacts/release/api
  set ASPNETCORE_ENVIRONMENT=Production
  set ConnectionStrings__ArchLucid=<your SQL connection string>
  dotnet ArchLucid.Api.dll

$uiLine

Docs: docs/RELEASE_LOCAL.md, docs/PILOT_GUIDE.md, README.md (health: /health/live, /health/ready).
"@

Set-Content -LiteralPath $handoffPath -Value $handoff.TrimEnd() -Encoding utf8

Write-Host ''
Write-Host 'Release handoff artifacts:' -ForegroundColor Green

foreach ($p in @($metadataPath, $manifestPath, $checksumPath, $handoffPath)) {
    Write-Host "  $p"
}
