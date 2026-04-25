#Requires -Version 7.0
<#
.SYNOPSIS
    Check SaaS Terraform layout consistency: provider pins, apply-saas.ps1 coverage, variable type alignment.

.DESCRIPTION
    - Compares required provider version constraints (azurerm) from each root's versions.tf
    - Ensures every path in infra/apply-saas.ps1 (pilot + multiRoot lists) exists on disk
    - Warns for infra/terraform* stacks that are not referenced by apply-saas.ps1
    - Warns when the same variable name is declared with different type expressions across roots
    - Does not read Azure credentials or run terraform plan/apply
.PARAMETER Strict
    When set, non-zero exit if any provider version constraint differs (default: provider drift is warning-only).
#>
[CmdletBinding()]
param(
    [switch] $Strict
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$applyPath = Join-Path (Join-Path $repoRoot "infra") "apply-saas.ps1"
if (-not (Test-Path -LiteralPath $applyPath)) {
    Write-Error "Missing infra/apply-saas.ps1"
    exit 2
}

function Get-InfraTerraformStackDirectories {
    # Same discovery as validate-saas-infra.ps1, but only infra/terraform* (not modules) for "stack" coverage
    param([string] $RepositoryRoot)
    $list = [System.Collections.Generic.List[string]]::new()
    $infra = Join-Path $RepositoryRoot "infra"
    foreach ($d in Get-ChildItem -LiteralPath $infra -Directory) {
        if ($d.Name -notlike "terraform*") { continue }
        if (-not (Get-ChildItem -LiteralPath $d.FullName -Filter "*.tf" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) { continue }
        $list.Add($d.FullName) | Out-Null
    }
    $list | Sort-Object -Unique
}

function Get-ApplySaasPathsFromScript {
    param([string] $ApplyFilePath)
    $text = Get-Content -LiteralPath $ApplyFilePath -Raw
    $out = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($text, '"(infra/[^""]+)"')) { $null = $out.Add($m.Groups[1].Value) }
    $out
}

$referenced = @(
    Get-ApplySaasPathsFromScript -ApplyFilePath $applyPath | Where-Object { $_ -match "^infra/terraform" }
) | Sort-Object
$critical = $false
$warnings = [System.Collections.Generic.List[string]]::new()

Write-Host "== apply-saas.ps1 referenced paths ($($referenced.Count))" -ForegroundColor Cyan
foreach ($r in $referenced) {
    $abs = Join-Path $repoRoot ($r -replace "/", [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $abs -PathType Container)) {
        Write-Warning "CRITICAL: path in apply-saas lists does not exist: $r"
        $warnings.Add("CRITICAL: missing $r")
        $critical = $true
    }
}

$stacks = @(Get-InfraTerraformStackDirectories -RepositoryRoot $repoRoot)
foreach ($s in $stacks) {
    $rel = ($s.Substring($repoRoot.Length).TrimStart([char[]]"/\")) -replace "\\", "/"
    if ($rel -notmatch "^infra/") { $rel = "infra/" + $rel }
    if ($rel -in $referenced) { continue }
    $msg = "SaaS stack on disk is not in apply-saas.ps1 (pilot or multiRoot list): $rel (add to apply or exclude intentionally)"
    Write-Warning $msg
    $warnings.Add("WARN: $msg")
}

# Provider version constraints (azurerm)
$providerMap = [ordered]@{}
foreach ($stack in $stacks) {
    $vfile = Join-Path $stack "versions.tf"
    if (-not (Test-Path -LiteralPath $vfile)) { continue }
    $raw = Get-Content -LiteralPath $vfile -Raw
    if ($raw -notmatch "azurerm") { continue }
    if ($raw -notmatch 'azurerm[\s\S]*?version\s*=\s*"([^"]+)"') { $ver = "unknown" } else { $ver = $matches[1] }
    $key = ($stack | Split-Path -Leaf)
    $providerMap[$key] = $ver.Trim()
}

$unique = ($providerMap.Values | Sort-Object -Unique)
if ($unique.Count -gt 1) {
    $m = "Provider (azurerm) version constraints differ across terraform stacks: " + ($unique -join " | ")
    if ($Strict) {
        Write-Host "CRITICAL: $m" -ForegroundColor Red
        $critical = $true
    } else {
        Write-Warning $m
        $warnings.Add("WARN: $m")
    }
}

# Variable name -> type line (first type= after variable block start; best-effort)
$varTypeByName = @{}
foreach ($stack in $stacks) {
    $vf = Join-Path $stack "variables.tf"
    if (-not (Test-Path -LiteralPath $vf)) { continue }
    $stackName = Split-Path -Leaf $stack
    $lines = @(Get-Content -LiteralPath $vf)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^\s*variable\s+"([^"]+)"\s*\{') { continue }
        $vname = $matches[1]
        $end = [math]::Min($i + 45, $lines.Count - 1)
        for ($j = $i + 1; $j -le $end; $j++) {
            if ($lines[$j] -match "^\s*variable\s+") { break }
            if ($lines[$j] -notmatch "^\s*type\s*=\s*(.+)") { continue }
            $t = $matches[1].Trim()
            if (-not $varTypeByName.ContainsKey($vname)) { $varTypeByName[$vname] = [System.Collections.Generic.List[object]]::new() }
            $varTypeByName[$vname].Add([pscustomobject]@{ Stack = $stackName; Type = $t })
            break
        }
    }
}
foreach ($entry in $varTypeByName.GetEnumerator()) {
    $name = $entry.Key
    $usages = $entry.Value
    if ($usages.Count -lt 2) { continue }
    $types = $usages | ForEach-Object { $_.Type } | Sort-Object -Unique
    if ($types.Count -le 1) { continue }
    $msg = "Variable '$name' has different type expressions: " + (($usages | ForEach-Object { "$($_.Stack)=$($_.Type)" }) -join "; ")
    Write-Warning $msg
    $warnings.Add("WARN: $msg")
}

if ($critical) { exit 1 }
exit 0
