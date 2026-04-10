@echo off
setlocal
cd /d "%~dp0"
REM 54R — Dapper + SQL Server persistence tests. Set ARCHLUCID_SQL_TEST (see docs/BUILD.md).
REM See docs/TEST_EXECUTION_MODEL.md
dotnet test ArchiForge.Persistence.Tests --filter "Category=SqlServerContainer"
exit /b %ERRORLEVEL%
