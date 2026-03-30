@echo off
setlocal
cd /d "%~dp0"
REM Integration tier: Category=Integration. See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.sln --filter "Category=Integration"
exit /b %ERRORLEVEL%
