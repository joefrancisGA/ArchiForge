<#
.SYNOPSIS
    Opinionated Terraform plan/apply for the ArchLucid SaaS profile (see docs/REFERENCE_SAAS_STACK_ORDER.md).

.DESCRIPTION
    **Default:** runs only `infra/terraform-pilot` — the canonical pilot profile (cost knobs + nested stack metadata; no Azure resources in that root).

    **Opt-in (-MultiRoot):** runs `terraform init` then `terraform plan` (default) or `terraform apply -auto-approve` for each infrastructure root in dependency order. Backends and tfvars are still operator-supplied.

    The pilot profile root is **not** included in the -MultiRoot list — it does not provision Azure resources.

.PARAMETER Apply
    When set, runs apply instead of plan.

.PARAMETER MultiRoot
    When set, plans/applies each nested infrastructure root in order (separate state per directory — advanced path).

.PARAMETER TerraformRoots
    Optional override list of directory paths relative to repo root (supersedes -MultiRoot default lists).
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Apply,
    [switch] $MultiRoot,
    [string[]] $TerraformRoots = $null
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

[string[]] $pilotProfileOnly = @(
    "infra/terraform-pilot"
)

[string[]] $multiRootSequence = @(
    "infra/terraform-private",
    "infra/terraform-keyvault",
    "infra/terraform-sql-failover",
    "infra/terraform-storage",
    "infra/terraform-servicebus",
    "infra/terraform-logicapps",
    "infra/terraform-openai",
    "infra/terraform-entra",
    "infra/terraform-container-apps",
    "infra/terraform-edge",
    "infra/terraform",
    "infra/terraform-monitoring",
    "infra/terraform-orchestrator"
)

[string[]] $roots = if ($null -ne $TerraformRoots -and $TerraformRoots.Length -gt 0) {
    $TerraformRoots
}
elseif ($MultiRoot) {
    $multiRootSequence
}
else {
    $pilotProfileOnly
}

foreach ($relative in $roots) {
    $dir = Join-Path $repoRoot $relative
    if (-not (Test-Path $dir)) {
        Write-Warning "Skipping missing directory: $relative"
        continue
    }

    Write-Host "==> $relative : terraform init" -ForegroundColor Cyan
    Push-Location $dir
    try {
        terraform init -input=false | Write-Host

        if ($Apply) {
            Write-Host "==> $relative : terraform apply" -ForegroundColor Yellow
            terraform apply -input=false -auto-approve | Write-Host
        }
        else {
            Write-Host "==> $relative : terraform plan" -ForegroundColor Yellow
            terraform plan -input=false | Write-Host
        }
    }
    finally {
        Pop-Location
    }
}

if ($MultiRoot) {
    Write-Host "Done (multi-root opt-in path). Review plans before passing -Apply." -ForegroundColor Green
}
else {
    Write-Host "Done (pilot profile only). For full stack order per root, use: ./infra/apply-saas.ps1 -MultiRoot" -ForegroundColor Green
}
