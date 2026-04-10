#Requires -Version 5.1
<#
.SYNOPSIS
  Starts SQL Server via docker compose (if needed), sets ARCHLUCID_SQL_TEST, runs full solution tests.

.DESCRIPTION
  Matches CI password for the sqlserver service in docker-compose.yml (ArchLucid_Dev_Pass123!).
  Use from repo root: .\scripts\run-full-regression-docker-sql.ps1
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

Write-Host "Starting SQL Server (docker compose sqlserver)..."
docker compose up -d sqlserver

$cs =
    "Server=127.0.0.1,1433;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;Initial Catalog=ArchLucidPersistenceTests"

$env:ARCHLUCID_SQL_TEST = $cs
Write-Host "ARCHLUCID_SQL_TEST set (catalog ArchLucidPersistenceTests)."

Write-Host "Waiting for SQL healthcheck (~35s)..."
Start-Sleep -Seconds 35

Write-Host "Running dotnet test (full solution, Release)..."
dotnet test ArchLucid.sln -c Release --collect:"XPlat Code Coverage"
