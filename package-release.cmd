@echo off
setlocal
cd /d "%~dp0"
REM Publish API to artifacts\release\api; optional UI production build when Node is on PATH. See docs/RELEASE_LOCAL.md
call "%~dp0build-release.cmd"
if errorlevel 1 exit /b %ERRORLEVEL%
if not exist "%~dp0artifacts\release\api" mkdir "%~dp0artifacts\release\api"
dotnet publish "%~dp0ArchiForge.Api\ArchiForge.Api.csproj" -c Release -o "%~dp0artifacts\release\api" --no-build
if errorlevel 1 exit /b %ERRORLEVEL%
where node >nul 2>&1
if errorlevel 1 goto :AfterUi
call :BuildUi
if errorlevel 1 exit /b %ERRORLEVEL%
:AfterUi
echo Release package: API published to %~dp0artifacts\release\api
echo See docs\RELEASE_LOCAL.md for run instructions.
exit /b 0

:BuildUi
pushd "%~dp0archiforge-ui"
call npm ci
if errorlevel 1 goto :UiFail
call npm run build
if errorlevel 1 goto :UiFail
popd
exit /b 0

:UiFail
popd
exit /b %ERRORLEVEL%
