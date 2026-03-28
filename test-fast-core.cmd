@echo off
setlocal
cd /d "%~dp0"
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
exit /b %ERRORLEVEL%
