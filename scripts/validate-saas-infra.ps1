#Requires -Version 7.0
<#
.SYNOPSIS
    Run terraform init (no remote backend) and terraform validate for every Terraform root under infra/.

.DESCRIPTION
    Discovers:
      - every directory under infra/ whose name matches terraform* and contains at least one *.tf file;
      - every immediate child of infra/modules/ that contains at least one *.tf file.
    No Azure credentials are required. Does not run plan or apply.

.PARAMETER Root
    Optional. Validate a single root only (name or path). Examples: terraform-pilot, infra/terraform-pilot,
    or a path under infra/modules/.
#>
[CmdletBinding()]
param(
    [string] $Root = $null
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location -LiteralPath $repoRoot

function Get-InfraTerraformRootDirectories {
    param([string] $RepositoryRoot)
    $list = [System.Collections.Generic.List[string]]::new()
    $infra = Join-Path $RepositoryRoot "infra"
    if (-not (Test-Path -LiteralPath $infra)) { return @() }
    foreach ($d in Get-ChildItem -LiteralPath $infra -Directory) {
        if ($d.Name -notlike "terraform*") { continue }
        if (-not (Get-ChildItem -LiteralPath $d.FullName -Filter "*.tf" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) { continue }
        $list.Add($d.FullName) | Out-Null
    }
    $modulesRoot = Join-Path $infra "modules"
    if (Test-Path -LiteralPath $modulesRoot) {
        foreach ($m in Get-ChildItem -LiteralPath $modulesRoot -Directory -ErrorAction SilentlyContinue) {
            if (-not (Get-ChildItem -LiteralPath $m.FullName -Filter "*.tf" -File -ErrorAction SilentlyContinue | Select-Object -First 1)) { continue }
            $list.Add($m.FullName) | Out-Null
        }
    }
    $list | Sort-Object -Unique
}

function Resolve-SingleRoot {
    param([string] $Raw, [string] $RepositoryRoot)
    $t = $Raw.Trim()
    if ([string]::IsNullOrWhiteSpace($t)) { return $null }
    if (Test-Path -LiteralPath $t -PathType Container) { return (Resolve-Path -LiteralPath $t).Path }
    $candidates = @(
        (Join-Path $RepositoryRoot $t)
        (Join-Path (Join-Path $RepositoryRoot "infra") $t)
        (Join-Path (Join-Path $RepositoryRoot "infra") (Join-Path "modules" $t))
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c -PathType Container) { return (Resolve-Path -LiteralPath $c).Path }
    }
    throw "Root not found: $Raw. Use a name like terraform-pilot, infra/terraform-pilot, or a module name."
}

function Get-RootDisplayPath {
    param([string] $RootPath, [string] $RepositoryRoot)
    $full = (Resolve-Path -LiteralPath $RootPath).Path
    $base = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    if ($full.Length -ge $base.Length -and $full.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) {
        $s = $full.Substring($base.Length).TrimStart([char[]]"/\")
        return $s -replace "\\", "/"
    }
    return $full
}

$terraformCmd = Get-Command terraform -ErrorAction SilentlyContinue
if ($null -eq $terraformCmd) {
    Write-Error "terraform CLI not found on PATH."
    exit 2
}

$roots = if (-not [string]::IsNullOrWhiteSpace($Root)) {
    @(Resolve-SingleRoot -Raw $Root -RepositoryRoot $repoRoot)
} else {
    @(Get-InfraTerraformRootDirectories -RepositoryRoot $repoRoot)
}

if ($roots.Count -eq 0) {
    Write-Error "No Terraform roots found under infra/."
    exit 2
}

$rows = [System.Collections.Generic.List[object]]::new()
$anyFail = $false
foreach ($dir in $roots) {
    $display = Get-RootDisplayPath -RootPath $dir -RepositoryRoot $repoRoot
    $initResult = "fail"
    $valResult = "skipped"
    Push-Location -LiteralPath $dir
    try {
        if ($VerbosePreference -eq "Continue") {
            & terraform init -backend=false -input=false
        } else {
            $null = & terraform init -backend=false -input=false 2>&1
        }
        if ($LASTEXITCODE -ne 0) { $anyFail = $true }
        else { $initResult = "ok" }
        if ($initResult -eq "ok") {
            if ($VerbosePreference -eq "Continue") { & terraform validate } else { $null = & terraform validate 2>&1 }
            if ($LASTEXITCODE -ne 0) {
                $valResult = "fail"
                $anyFail = $true
            } else {
                $valResult = "ok"
            }
        }
    } finally {
        Pop-Location
    }
    $rows.Add([pscustomobject]@{ Root = $display; Init = $initResult; Validate = $valResult })
}

$rows | Sort-Object Root | Format-Table -AutoSize
if ($anyFail) {
    Write-Error "One or more Terraform roots failed init/validate. See table above."
    exit 1
}
exit 0
