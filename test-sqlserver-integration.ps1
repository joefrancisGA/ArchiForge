# 54R — Dapper + SQL Server (Category=SqlServerContainer on ArchLucid.Persistence.Tests).
# Set ARCHLUCID_SQL_TEST to a full connection string (see docs/BUILD.md).
# See docs/TEST_EXECUTION_MODEL.md
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
dotnet test ArchLucid.Persistence.Tests --filter "Category=SqlServerContainer"
exit $LASTEXITCODE
