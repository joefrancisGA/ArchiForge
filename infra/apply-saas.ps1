<#
.SYNOPSIS
    Opinionated Terraform plan/apply order for ArchLucid SaaS (see docs/REFERENCE_SAAS_STACK_ORDER.md).

.DESCRIPTION
    Runs `terraform init` then `terraform plan` (default) or `terraform apply -auto-approve` for each root
    in dependency order. Pass -Apply to mutate cloud state. Backends and tfvars are still operator-supplied.

.PARAMETER Apply
    When set, runs apply instead of plan.

.PARAMETER TerraformRoots
    Optional override list of directory paths relative to repo root.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Apply,
    [string[]] $TerraformRoots = @(
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
        "infra/terraform-monitoring",
        "infra/terraform-pilot"
    )
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

foreach ($relative in $TerraformRoots) {
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

Write-Host "Done. Review plans before passing -Apply." -ForegroundColor Green
