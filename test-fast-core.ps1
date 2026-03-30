# Fast core: Suite=Core, excluding Category=Slow and Category=Integration.
# See docs/TEST_EXECUTION_MODEL.md
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
exit $LASTEXITCODE
