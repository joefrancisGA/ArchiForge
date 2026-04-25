#Requires -Version 7.0
<#
.SYNOPSIS
  Read an Azure Resource Manager (ARM) JSON template and emit a V1 `POST /v1/architecture/request` body
  with `infrastructureDeclarations[0].format` = `json` (ResourceDeclarationDocument).

.PARAMETER TemplatePath
  Path to a template file (e.g. exported from `az deployment group export` or a repository template).

#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)] [string] $TemplatePath,
  [string] $SystemName = "ARM import",
  [string] $Environment = "prod",
  [string] $Description = "Architecture request generated from an ARM template (see infrastructureDeclarations[0] for the resource list)."
)
$ErrorActionPreference = "Stop"
$raw = Get-Content -Path $TemplatePath -Raw -Encoding utf8
$arm = $raw | ConvertFrom-Json
$resources = [System.Collections.Generic.List[hashtable]]::new()
$items = $arm.resources
if ($null -ne $items) {
  foreach ($r in $items) {
    $t = [string] $r.type
    $n = [string] $r.name
    if ([string]::IsNullOrWhiteSpace($t) -or [string]::IsNullOrWhiteSpace($n)) { continue }
    $props = @{ "armType" = $t }
    if ($r.apiVersion) { $props["apiVersion"] = [string] $r.apiVersion }
    if ($r.location) { $props["location"] = [string] $r.location }
    $resources.Add(@{
        type     = $t
        name     = $n
        properties = $props
      })
  }
}
$declarationJson = (@{ resources = $resources } | ConvertTo-Json -Depth 25 -Compress)
$summary = "Imported " + $resources.Count + " ARM template resources (see infrastructureDeclarations[0])."
$body = [ordered]@{
  requestId             = "arm-import-" + [Guid]::NewGuid().ToString("N")
  description            = if ($Description.Length -ge 10) { $Description } else { "ARM import. $Description" }
  systemName            = $SystemName
  environment            = $Environment
  cloudProvider          = "Azure"
  constraints            = @("Ingested from ARM template; validate against live subscription state separately.")
  inlineRequirements     = @($summary)
  infrastructureDeclarations = @(
    @{
      name     = "arm-template"
      format   = "json"
      content  = $declarationJson
    }
  )
} | ConvertTo-Json -Depth 25
Write-Output "POST to `"$($env:ARCHLUCID_API_URL.TrimEnd('/'))/v1/architecture/request`" with X-Api-Key when set."
Write-Output $body
if ($env:ARCHLUCID_API_URL -and $env:ARCHLUCID_API_KEY) {
  $u = $env:ARCHLUCID_API_URL.TrimEnd('/') + "/v1/architecture/request"
  Invoke-RestMethod -Method Post -Uri $u -Headers @{ "X-Api-Key" = $env:ARCHLUCID_API_KEY; "Content-Type" = "application/json" } -Body $body
} else {
  Write-Warning "Set ARCHLUCID_API_URL and ARCHLUCID_API_KEY to post automatically."
}
