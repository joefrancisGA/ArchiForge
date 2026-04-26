@echo off
REM V1 RC drill (HTTP against a running API). See docs/library/V1_RC_DRILL.md
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0v1-rc-drill.ps1" %*
exit /b %ERRORLEVEL%
