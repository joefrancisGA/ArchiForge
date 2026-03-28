# Runs the Core automated test suite (xUnit trait Suite=Core).
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
dotnet test ArchiForge.sln --filter "Suite=Core"
exit $LASTEXITCODE
