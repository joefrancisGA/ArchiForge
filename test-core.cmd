@echo off
setlocal
cd /d "%~dp0"
REM Core tier: Suite=Core. See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.sln --filter "Suite=Core"
exit /b %ERRORLEVEL%
