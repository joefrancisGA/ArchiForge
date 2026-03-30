# Slow tier: xUnit Category=Slow. See docs/TEST_EXECUTION_MODEL.md
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
dotnet test ArchiForge.sln --filter "Category=Slow"
exit $LASTEXITCODE
