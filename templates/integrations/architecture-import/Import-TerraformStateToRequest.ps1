#Requires -Version 7.0
<#
.SYNOPSIS
  Run `terraform show -json` and build a V1 `POST /v1/architecture/request` JSON body using the public API `json`
  infrastructure declaration format (ResourceDeclarationDocument), not the internal `terraform-show-json` path.

  The server parser `ArchLucid.ContextIngestion/.../TerraformShowJsonInfrastructureDeclarationParser` maps full
  `terraform show -json` state; this script transforms into the *API-accepted* `format: "json"` payload.

.PARAMETER SystemName
  `systemName` in the request.

.PARAMETER Description
  `description` (>= 10 characters).

#>
[CmdletBinding()]
param(
  [string] $SystemName = "Terraform import",
  [string] $Environment = "prod",
  [string] $Description = "Architecture request generated from Terraform state (see inlineRequirements[0] for the resource summary).",
  [string] $WorkingDirectory = (Get-Location).Path
)
$ErrorActionPreference = "Stop"
Push-Location $WorkingDirectory
try {
  $raw = & terraform show -json 2>&1
  if ($LASTEXITCODE -ne 0) { throw "terraform show -json failed: $raw" }
} finally {
  Pop-Location
}
$state = $raw | ConvertFrom-Json
$resources = [System.Collections.Generic.List[hashtable]]::new()
if ($state.values -and $state.values.root_module -and $state.values.root_module.resources) {
  foreach ($r in $state.values.root_module.resources) {
    $t = [string] $r.type
    $n = [string] $r.name
    if ([string]::IsNullOrWhiteSpace($t) -or [string]::IsNullOrWhiteSpace($n)) { continue }
    $props = @{}
    $props["terraformType"] = $t
    if ($r.provider_name) { $props["providerName"] = [string] $r.provider_name }
    if ($r.mode) { $props["mode"] = [string] $r.mode }
    $resources.Add(@{
        type     = $t
        name     = $n
        properties = $props
      })
  }
}
# ResourceDeclarationDocument: content must be a *string* (JSON) in the API contract.
$declarationJson = (@{ resources = $resources } | ConvertTo-Json -Depth 25 -Compress)
$summary = "Imported " + $resources.Count + " Terraform root-module resources (see infrastructureDeclarations[0] json)."
$body = [ordered]@{
  requestId             = "tf-import-" + [Guid]::NewGuid().ToString("N")
  description            = if ($Description.Length -ge 10) { $Description } else { "Terraform import. $Description" }
  systemName            = $SystemName
  environment            = $Environment
  cloudProvider          = "Azure"
  constraints            = @("Ingested from terraform show -json; review ResourceDeclaration item coverage in the product.")
  inlineRequirements     = @($summary)
  infrastructureDeclarations = @(
    @{
      name     = "terraform-state-json"
      format   = "json"
      content  = $declarationJson
    }
  )
} | ConvertTo-Json -Depth 25
Write-Output "POST this JSON to `"$($env:ARCHLUCID_API_URL.TrimEnd('/'))/v1/architecture/request`" with header X-Api-Key."
Write-Output $body
if ($env:ARCHLUCID_API_URL -and $env:ARCHLUCID_API_KEY) {
  $u = $env:ARCHLUCID_API_URL.TrimEnd('/') + "/v1/architecture/request"
  Invoke-RestMethod -Method Post -Uri $u -Headers @{ "X-Api-Key" = $env:ARCHLUCID_API_KEY; "Content-Type" = "application/json" } -Body $body
} else {
  Write-Warning "Set ARCHLUCID_API_URL and ARCHLUCID_API_KEY to post automatically."
}
