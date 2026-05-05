# Points this repo at scripts/git-hooks (pre-push: OpenAPI snapshot gate).
# Run from repo root: .\scripts\git-hooks\Install-GitHooks.ps1

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
Set-Location $root

git config core.hooksPath scripts/git-hooks
Write-Host 'git core.hooksPath set to scripts/git-hooks (pre-push runs OpenAPI contract check when API-related paths change).'
Write-Host 'Skip once (PowerShell): $env:ARCHLUCID_SKIP_OPENAPI_PRE_PUSH = "1"; git push'
Write-Host 'Always run on push (PowerShell): $env:ARCHLUCID_OPENAPI_PRE_PUSH = "all"; git push'
