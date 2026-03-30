@echo off
setlocal
cd /d "%~dp0"
REM RC-style gate: Release build, fast-core in Release, UI Vitest if Node is available. See docs/RELEASE_LOCAL.md
call "%~dp0build-release.cmd"
if errorlevel 1 exit /b %ERRORLEVEL%
echo === Fast core tests (Release, no rebuild) ===
dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"
if errorlevel 1 exit /b %ERRORLEVEL%
where node >nul 2>&1
if errorlevel 1 goto :SkipUi
echo === Operator UI unit tests (Vitest) ===
call "%~dp0test-ui-unit.cmd"
if errorlevel 1 exit /b %ERRORLEVEL%
goto :Done
:SkipUi
echo Node.js not on PATH; skipped UI unit tests. Install Node 22+ for a full gate.
:Done
echo === Readiness check finished successfully ===
exit /b 0
