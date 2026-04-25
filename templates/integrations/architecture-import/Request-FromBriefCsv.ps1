#Requires -Version 7.0
<#
.SYNOPSIS
  Build a minimal `POST /v1/architecture/request` JSON from `brief-template.csv` (no Terraform/ARM required).
#>
[CmdletBinding()]
param(
  [string] $CsvPath = (Join-Path $PSScriptRoot "brief-template.csv")
)
$ErrorActionPreference = "Stop"
$rows = Import-Csv -Path $CsvPath
if ($rows.Count -lt 1) { throw "CSV must have at least one data row." }
$r = $rows[0]
$desc = [string] $r.description
if ($desc.Length -lt 10) { throw "description column must be at least 10 characters." }
$constraints = @()
if ($r.constraint1) { $constraints += [string] $r.constraint1 }
if ($r.constraint2) { $constraints += [string] $r.constraint2 }
$body = [ordered]@{
  requestId         = "csv-brief-" + [Guid]::NewGuid().ToString("N")
  description       = $desc
  systemName        = [string] $r.systemName
  environment       = [string] $r.environment
  cloudProvider     = [string] $r.cloudProvider
  constraints       = $constraints
} | ConvertTo-Json -Depth 6
Write-Output $body
if ($env:ARCHLUCID_API_URL -and $env:ARCHLUCID_API_KEY) {
  $u = $env:ARCHLUCID_API_URL.TrimEnd('/') + "/v1/architecture/request"
  Invoke-RestMethod -Method Post -Uri $u -Headers @{ "X-Api-Key" = $env:ARCHLUCID_API_KEY; "Content-Type" = "application/json" } -Body $body
}
