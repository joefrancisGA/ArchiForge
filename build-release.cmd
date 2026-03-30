@echo off
setlocal
cd /d "%~dp0"
REM Release configuration build (whole solution). See docs/RELEASE_LOCAL.md
dotnet restore ArchiForge.sln
if errorlevel 1 exit /b %ERRORLEVEL%
dotnet build ArchiForge.sln -c Release --nologo
exit /b %ERRORLEVEL%
