@echo off
setlocal
cd /d "%~dp0"
REM Full regression: entire solution. See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.sln
exit /b %ERRORLEVEL%
