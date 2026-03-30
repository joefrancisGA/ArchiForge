@echo off
setlocal
cd /d "%~dp0"
REM Slow tier: Category=Slow. See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.sln --filter "Category=Slow"
exit /b %ERRORLEVEL%
