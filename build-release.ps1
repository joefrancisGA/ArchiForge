# Release configuration build (whole solution, -c Release). See docs/RELEASE_LOCAL.md
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
dotnet restore ArchiForge.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build ArchiForge.sln -c Release --nologo
exit $LASTEXITCODE
