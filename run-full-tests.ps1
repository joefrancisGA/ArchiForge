param(
    [switch]$SkipInstall,
    [switch]$UiOnly,
    [switch]$ApiOnly,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Continue"

$repo = Resolve-Path "."
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$artifacts = Join-Path $repo "artifacts/test-runs/$stamp"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$summary = @()
$failures = @()

function Run-Step {
    param(
        [string]$Name,
        [scriptblock]$Command,
        [string]$LogFile
    )

    Write-Host ""
    Write-Host "===== $Name =====" -ForegroundColor Cyan

    $logPath = Join-Path $artifacts $LogFile
    $start = Get-Date

    & $Command 2>&1 | Tee-Object -FilePath $logPath
    $exit = $LASTEXITCODE

    $elapsed = [Math]::Round(((Get-Date) - $start).TotalSeconds, 1)

    if ($exit -eq 0 -or $null -eq $exit) {
        $summary += "| $Name | PASS | ${elapsed}s | $LogFile |"
    }
    else {
        $summary += "| $Name | FAIL | ${elapsed}s | $LogFile |"
        $failures += "$Name failed. See $LogFile."
    }

    return $exit
}

if (-not $UiOnly) {
    Run-Step "Dotnet restore" {
        dotnet restore
    } "dotnet-restore.log"

    Run-Step "Dotnet build" {
        dotnet build --configuration $Configuration --no-restore -v minimal
    } "dotnet-build.log"

    Run-Step "Dotnet test" {
        dotnet test --configuration $Configuration --no-build `
            --logger "trx;LogFileName=dotnet-tests.trx" `
            --results-directory "$artifacts/dotnet" `
            --verbosity normal
    } "dotnet-test.log"
}

if (-not $ApiOnly) {
    Push-Location "ui"

    if (-not $SkipInstall) {
        Run-Step "NPM install" {
            npm ci
        } "npm-ci.log"
    }

    Run-Step "UI lint" {
        npm run lint
    } "ui-lint.log"

    Run-Step "UI typecheck" {
        npm run typecheck
    } "ui-typecheck.log"

    Run-Step "UI build" {
        npm run build
    } "ui-build.log"

    Run-Step "Playwright tests" {
        npx playwright test --reporter=list,json --output="../$artifacts/playwright-output"
    } "playwright.log"

    Pop-Location
}

$handoffPath = Join-Path $artifacts "cursor-handoff.md"

@"
# ArchLucid Test Handoff

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

Repo: $repo

## Summary

| Step | Result | Time | Log |
|---|---:|---:|---|
$($summary -join "`n")

## Failures

$(
if ($failures.Count -eq 0) {
"No failures detected by wrapper script."
} else {
($failures | ForEach-Object { "- $_" }) -join "`n"
}
)

## Cursor Instructions

Please inspect the logs in this folder and identify the smallest safe change set to fix the failing tests.

Prioritize:
- Build-blocking errors
- Runtime route failures
- API/client contract mismatches
- Failing Playwright screenshots
- Type errors
- Lint errors

Do not refactor unrelated code.

## Artifact Folder

$artifacts

## Recommended First Logs To Inspect

- dotnet-build.log
- dotnet-test.log
- ui-build.log
- playwright.log
- ui-typecheck.log

"@ | Set-Content -Path $handoffPath -Encoding UTF8

Write-Host ""
Write-Host "Test artifacts written to:" -ForegroundColor Green
Write-Host $artifacts

Write-Host ""
Write-Host "Cursor handoff file:" -ForegroundColor Green
Write-Host $handoffPath

if ($failures.Count -gt 0) {
    exit 1
}

exit 0