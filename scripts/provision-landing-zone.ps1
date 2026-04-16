<#!
.SYNOPSIS
  Runs Terraform init/validate (default), optional plan, or apply across ArchLucid infra roots in dependency order.

.DESCRIPTION
  Each root under infra/terraform-* keeps its own state file. This script runs CLI steps in order only.

.PARAMETER DryRun
  Print intended commands without executing Terraform.

.PARAMETER Plan
  After validate, run terraform plan per root (requires Azure auth for provider refresh).

.PARAMETER Apply
  After validate, run terraform apply -auto-approve per root (destructive — use only in controlled pipelines).

.PARAMETER VarFile
  Optional -var-file= path for plan/apply.

.EXAMPLE
  ./scripts/provision-landing-zone.ps1
  ./scripts/provision-landing-zone.ps1 -DryRun
  ./scripts/provision-landing-zone.ps1 -Plan
#>
param(
    [switch] $DryRun,
    [switch] $Plan,
    [switch] $Apply,
    [string] $VarFile = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$orderedRoots = @(
    "infra/terraform-storage",
    "infra/terraform-private",
    "infra/terraform-container-apps",
    "infra/terraform-sql-failover",
    "infra/terraform-entra",
    "infra/terraform-openai",
    "infra/terraform-keyvault",
    "infra/terraform-monitoring",
    "infra/terraform-edge",
    "infra/terraform",
    "infra/terraform-servicebus",
    "infra/terraform-orchestrator"
)

function Invoke-TerraformInRoot {
    param(
        [string] $Root
    )
    Write-Host "==> $Root" -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot $Root)
    try {
        if ($DryRun) {
            Write-Host "  [dry-run] terraform init -backend=false && validate && fmt -check" -ForegroundColor DarkGray
            return
        }
        terraform init -backend=false | Write-Host
        terraform validate | Write-Host
        terraform fmt -check -recursive | Write-Host
        if ($Plan -or $Apply) {
            $extra = @()
            if ($VarFile -ne "") {
                $extra += "-var-file=$VarFile"
            }
            if ($Apply) {
                terraform @("apply", "-auto-approve") + $extra | Write-Host
            }
            else {
                terraform @("plan", "-input=false") + $extra | Write-Host
            }
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host "ArchLucid landing zone — $($orderedRoots.Count) Terraform roots" -ForegroundColor Green
foreach ($r in $orderedRoots) {
    $path = Join-Path $repoRoot $r
    if (-not (Test-Path $path)) {
        Write-Warning "Skip missing directory: $r"
        continue
    }
    Invoke-TerraformInRoot -Root $r
}
Write-Host "Done." -ForegroundColor Green
