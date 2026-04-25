#Requires -Version 5.1
<#
  Runs core-pilot.js for all three profiles and writes:
    tests/load/results/baseline-YYYY-MM-DD.json

  Merges fragments with tests/load/merge_baseline.py (python3) when available; otherwise
  keep fragment-*.json for manual merge.

  Prerequisites: k6 in PATH, API on ARCHLUCID_BASE_URL (default http://127.0.0.1:5001), Development,
  InMemory, AgentExecution:Mode=Simulator (see appsettings.Development.json). Raise rate limits
  for high VU: RateLimiting__FixedWindow__PermitLimit=200000

  Quick capture (short k6 windows):  -Compress
#>
param(
  [string] $Date = (Get-Date -Format "yyyy-MM-dd"),
  [string] $BaseUrl = "http://127.0.0.1:5001",
  [switch] $Compress
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path (Join-Path $root "ArchLucid.Api"))) {
  throw "Could not find ArchLucid.Api; use repo root. Resolved root: $root"
}

$pyExe = $null
foreach ($name in @("py", "python", "python3")) {
  $c = Get-Command $name -ErrorAction SilentlyContinue
  if ($null -ne $c) { $pyExe = $c.Source; break }
}
if ($null -eq $pyExe) { throw "Python 3 (py, python, or python3) is required for merge_baseline.py." }

$resultsDir = Join-Path $root "tests\load\results"
if (-not (Test-Path $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir | Out-Null }

$env:ARCHLUCID_BASE_URL = $BaseUrl
if ($Compress) { $env:K6_COMPRESS = "1" } else { Remove-Item Env:K6_COMPRESS -ErrorAction SilentlyContinue }

$profiles = @("core", "read", "mixed")
$fragments = [System.Collections.Generic.List[string]]::new()
foreach ($p in $profiles) {
  $env:K6_LOAD_PROFILE = $p
  $tmp = Join-Path $env:TEMP "archlucid-k6-fragment-$p-$Date.json"
  if (Test-Path $tmp) { Remove-Item -Force $tmp }
  $env:K6_BASELINE_PATH = $tmp
  try {
    Push-Location $root
    $k6 = (Get-Command k6 -ErrorAction SilentlyContinue)
    if ($null -eq $k6) {
      throw "k6 is not in PATH. Install: winget install k6 --source winget, or choco install k6."
    }
    k6 run "tests/load/core-pilot.js"
    if ($LASTEXITCODE -ne 0) { throw "k6 failed for profile $p (exit $LASTEXITCODE)" }
  }
  finally { Pop-Location }
  if (-not (Test-Path $tmp)) { throw "k6 did not write K6_BASELINE_PATH for profile $p : $tmp" }
  $fragments.Add($tmp)
}

$merged = Join-Path $resultsDir "baseline-$Date.json"
$merge = Join-Path $root "tests\load\merge_baseline.py"
& $pyExe $merge $merged @fragments
if ($LASTEXITCODE -ne 0) { throw "merge_baseline.py failed" }
Write-Host "Wrote $merged"
