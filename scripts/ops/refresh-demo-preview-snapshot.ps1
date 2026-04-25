<#
.SYNOPSIS
  Re-captures archlucid-ui/public/demo-preview-snapshot.json from GET /v1/demo/preview (docker-compose demo profile).

.DESCRIPTION
  Intended for maintainers after demo seed changes. Calls the anonymous preview endpoint once (honors API rate limits).
  Optionally writes demo-preview-snapshot.etag next to the JSON for ETag-aware marketing builds.

.PARAMETER BaseUrl
  ArchLucid API root (no trailing slash). Default http://localhost:5000 (docker-compose full-stack mapping).

.EXAMPLE
  ./scripts/ops/refresh-demo-preview-snapshot.ps1
  ./scripts/ops/refresh-demo-preview-snapshot.ps1 -BaseUrl "https://staging.example.com"
#>
[CmdletBinding()]
param(
  [string] $BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outDir = Join-Path $root "archlucid-ui\public"
$jsonPath = Join-Path $outDir "demo-preview-snapshot.json"
$etagPath = Join-Path $outDir "demo-preview-snapshot.etag"

$baseTrimmed = $BaseUrl.TrimEnd("/")
$uri = "$baseTrimmed/v1/demo/preview"

Write-Host "GET $uri"

$response = Invoke-WebRequest -Uri $uri -Headers @{ Accept = "application/json" } -Method Get -UseBasicParsing

if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
  throw "Unexpected status $($response.StatusCode) from demo preview."
}

[System.IO.File]::WriteAllText($jsonPath, $response.Content, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $jsonPath"

$etag = $response.Headers["ETag"]

if ($etag) {
  [System.IO.File]::WriteAllText($etagPath, $etag.Trim(), [System.Text.UTF8Encoding]::new($false))
  Write-Host "Wrote $etagPath"
}
