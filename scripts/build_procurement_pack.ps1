#Requires -Version 7.0
<#
.SYNOPSIS
  Assembles dist/procurement-pack.zip via scripts/build_procurement_pack.py (single source of truth).

.NOTES
  Pass-through args, e.g.:
    ./scripts/build_procurement_pack.ps1 --dry-run
    ./scripts/build_procurement_pack.ps1 --out C:\temp\pack.zip
#>
$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$py = Join-Path $repoRoot "scripts/build_procurement_pack.py"
$env:PYTHONUTF8 = "1"
python $py @args
