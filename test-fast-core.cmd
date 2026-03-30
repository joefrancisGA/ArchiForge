@echo off
setlocal
cd /d "%~dp0"
REM Fast core: Suite=Core, exclude Slow and Integration. See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
exit /b %ERRORLEVEL%
