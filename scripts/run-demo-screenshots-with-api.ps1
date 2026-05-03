# Starts ArchLucid.Api in the background, waits for /health/ready, builds archlucid-ui,
# runs `npm run screenshots:demo` (Playwright live-api-demo-screenshots), then tears down the API job.
#
# Prerequisites (same as a normal local API run - this script cannot fix SQL for you):
#   - .NET SDK (repo global.json)
#   - Node.js + `npm ci` in archlucid-ui (first time)
#   - SQL + ConnectionStrings / user secrets (see docs/library/OPERATOR_QUICKSTART.md)
#
# Usage (from repo root or anywhere):
#   .\scripts\run-demo-screenshots-with-api.ps1
# Optional:
#   .\scripts\run-demo-screenshots-with-api.ps1 -ApiReadyTimeoutSec 300 -ScreenshotsVerbose

[CmdletBinding()]
param(
    [int] $ApiPort = 5128,
    [int] $ApiReadyTimeoutSec = 600,
    [int] $HealthPollIntervalSec = 3,
    [switch] $SkipNextBuildOnly,
    [switch] $ScreenshotsVerbose
)

$ErrorActionPreference = "Stop"

function Get-ElapsedPrefix {
    if ($script:T0Utc) {
        $sec = [math]::Round(((Get-Date).ToUniversalTime() - $script:T0Utc).TotalSeconds, 1)
        return "[$sec s]"
    }
    return ""
}

function Write-BannerPhase {
    param(
        [Parameter(Mandatory = $true)][string] $Title
    )

    $ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")

    Write-Host ""
    Write-Host "===================================================================" -ForegroundColor DarkCyan
    Write-Host "$(Get-ElapsedPrefix)  $Title" -ForegroundColor Cyan
    Write-Host "                                   (local clock $ts)" -ForegroundColor DarkGray
    Write-Host "===================================================================" -ForegroundColor DarkCyan
    Write-Host ""
}

function Write-Step {
    param(
        [Parameter(Mandatory = $true)][string] $Message
    )

    Write-Host "$(Get-ElapsedPrefix)  -> $Message" -ForegroundColor White
}

function Write-Ok {
    param([string] $Message = "Done.")

    Write-Host "$(Get-ElapsedPrefix)  OK: $Message" -ForegroundColor Green
}

function Write-WarnStep {
    param([Parameter(Mandatory = $true)][string] $Message)

    Write-Host "$(Get-ElapsedPrefix)  WARN: $Message" -ForegroundColor Yellow
}

function Receive-BackgroundJobTail {
    param(
        [Parameter(Mandatory = $true)] $Job
    )

    $lines = Receive-Job $Job -ErrorAction SilentlyContinue
    foreach ($ln in $lines) {
        if ($null -ne $ln -and "$ln".Trim().Length -gt 0) {
            Write-Host "    [API] $ln" -ForegroundColor DarkGray
        }
    }
}

function Wait-HealthReadyVerbose {
    param(
        [Parameter(Mandatory = $true)][string] $ReadyUrl,
        [Parameter(Mandatory = $true)] $Job,
        [int] $TimeoutSec,
        [int] $PollSec
    )

    Write-Step "Polling until GET $ReadyUrl returns HTTP 200 (timeout $($TimeoutSec)s, poll every $($PollSec)s)."
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $attempt = 0

    while ((Get-Date) -lt $deadline) {
        Receive-BackgroundJobTail -Job $Job
        try {
            $attempt++
            $response = Invoke-WebRequest -Uri $ReadyUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Ok "Health ready after attempt $attempt (HTTP $($response.StatusCode))"
                Receive-BackgroundJobTail -Job $Job
                return
            }

            Write-WarnStep "Unexpected status $($response.StatusCode) from readiness probe - retrying."
        }
        catch {
            $elapsedWait = [math]::Round(((Get-Date).ToUniversalTime() - $script:T0Utc).TotalSeconds, 0)
            Write-Host "$(Get-ElapsedPrefix)      ...still waiting ($($elapsedWait)s wall); last error: $($_.Exception.Message)" -ForegroundColor DarkYellow
            Start-Sleep -Seconds $PollSec
        }
    }

    Receive-BackgroundJobTail -Job $Job
    throw "Timed out waiting for API readiness ($ReadyUrl)."
}

$script:T0Utc = (Get-Date).ToUniversalTime()

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Csproj = Join-Path $RepoRoot "ArchLucid.Api\ArchLucid.Api.csproj"
$UiDir = Join-Path $RepoRoot "archlucid-ui"

Write-BannerPhase "Demo screenshots pipeline - prerequisites + API job + npm / Playwright"
Write-Step "Repo root resolved to: $RepoRoot"
Write-Step "API project path:       $Csproj"
Write-Step "UI root:               $UiDir"

if (-not (Test-Path $Csproj)) {
    throw "ArchLucid.Api project not found. Expected: $Csproj"
}

Write-Ok "ArchLucid.Api.csproj exists."

if (-not (Test-Path (Join-Path $UiDir "package.json"))) {
    throw "archlucid-ui missing package.json at $UiDir"
}

Write-Ok "archlucid-ui/package.json exists."

Write-BannerPhase " toolchain sanity (helps interpret later failures)"

try {
    $dn = dotnet --version 2>$null
    Write-Ok "dotnet SDK reports version $dn"
}
catch {
    Write-WarnStep "dotnet --version failed - verify .NET SDK is on PATH."
}

try {
    Push-Location $UiDir

    try {
        $nv = npm --version 2>$null
        Write-Ok "npm reports version $nv"
    }
    catch {
        Write-WarnStep "npm --version failed - verify Node is on PATH and run npm ci in archlucid-ui."
    }
}
finally {
    Pop-Location
}

$ApiBase = "http://127.0.0.1:$ApiPort"
$ReadyUrl = "$ApiBase/health/ready"

Write-BannerPhase "Starting API background job ($ApiBase)"

$env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
Write-Step "Set session env ASPNETCORE_URLS=$($env:ASPNETCORE_URLS) (job runspace mirrors this)"

Write-Step "Spawning: dotnet run --project <ArchLucid.Api> --launch-profile http"
$apiAspNetUrls = $env:ASPNETCORE_URLS
$apiJob = Start-Job -ScriptBlock {
    param ($CsprojPath, $AspNetUrls)
    $projDir = Split-Path -Parent $CsprojPath

    Write-Output "(job) working directory will be $projDir"

    Write-Output "(job) ASPNETCORE_URLS=$AspNetUrls"
    $env:ASPNETCORE_URLS = $AspNetUrls

    Set-Location -LiteralPath $projDir

    dotnet run --project $CsprojPath --launch-profile http --no-launch-browser
} -ArgumentList $Csproj, $apiAspNetUrls

Write-Ok "Background job Id=$($apiJob.Id) Name=$($apiJob.Name); state=$($apiJob.State)"
Write-Host "    Tip: you can peek live job output anytime with:" -ForegroundColor DarkGray

Write-Host "         Receive-Job -Id $($apiJob.Id) -Keep" -ForegroundColor DarkGray
Write-Host ""

try {
    Wait-HealthReadyVerbose -ReadyUrl $ReadyUrl -Job $apiJob -TimeoutSec $ApiReadyTimeoutSec -PollSec $HealthPollIntervalSec

    Push-Location $UiDir

    try {
        Write-BannerPhase "Configuring Live E2E / screenshot env for archlucid-ui"

        $env:LIVE_API_URL = $ApiBase
        Write-Step "Set LIVE_API_URL=$ApiBase"

        # Playwright demo preflight assumes DevelopmentBypass auth against localhost (dotnet run Development).
        # Stray JWT/API-key variables from other shells send wrong Authorization/X-Api-Key headers and cause 401/403.
        Remove-Item Env:LIVE_JWT_TOKEN -ErrorAction SilentlyContinue
        Remove-Item Env:LIVE_API_KEY -ErrorAction SilentlyContinue
        Remove-Item Env:LIVE_API_KEY_READONLY -ErrorAction SilentlyContinue
        Write-Step "Cleared LIVE_JWT_TOKEN / LIVE_API_KEY / LIVE_API_KEY_READONLY for this npm subprocess (if present)."

        if ($SkipNextBuildOnly) {
            Write-Step "Parameter -SkipNextBuildOnly: omitting npm run build (standalone .next tree must exist)."
            $env:LIVE_E2E_SKIP_NEXT_BUILD = "1"
            Write-Step "PLAYWRIGHT will not rebuild: LIVE_E2E_SKIP_NEXT_BUILD=1"
        }
        else {
            Write-BannerPhase "npm run build"

            npm run build
            if ($LASTEXITCODE -ne 0) {
                throw "npm run build failed with exit code $LASTEXITCODE"
            }

            Write-Ok "Next.js production build finished."

            $env:LIVE_E2E_SKIP_NEXT_BUILD = "1"
            Write-Step "Skipping duplicate build inside Playwright: LIVE_E2E_SKIP_NEXT_BUILD=1"
        }

        Write-BannerPhase "npm run screenshots:demo (Playwright chromium - live-api-demo-screenshots.spec.ts)"

        if ($ScreenshotsVerbose) {
            Write-Step "Verbose mode: PLAYWRIGHT_DEBUG=1 PLAYWRIGHT_DEBUG_BROWSER=console (very chatty)"

            $env:PLAYWRIGHT_DEBUG = "1"
            $env:PLAYWRIGHT_DEBUG_BROWSER = "console"

            npm run screenshots:demo
        }
        else {
            npm run screenshots:demo
        }

        if ($LASTEXITCODE -ne 0) {
            throw "npm run screenshots:demo failed with exit code $LASTEXITCODE"
        }

        Write-Ok "Screenshots pipeline completed (see archlucid-ui artifacts/screenshots/)."
    }
    finally {
        Pop-Location
    }
}
finally {
    Write-BannerPhase "Tearing down API background job Id=$($apiJob.Id)"

    Stop-Job $apiJob -ErrorAction SilentlyContinue

    Receive-BackgroundJobTail -Job $apiJob

    Remove-Job $apiJob -Force -ErrorAction SilentlyContinue
    Write-Ok "Removed API job."

    Write-BannerPhase "All steps finished ($(Get-ElapsedPrefix) elapsed since script start)"

    Write-Host "If screenshots failed, inspect:" -ForegroundColor DarkCyan

    Write-Host "  - API SQL / secrets (dotnet run failures print in [API] lines above)" -ForegroundColor DarkGray

    Write-Host "  - Playwright traces under archlucid-ui/test-results/ (when enabled)" -ForegroundColor DarkGray

    Write-Host "  - Report output under artifacts/screenshots/<timestamp>/" -ForegroundColor DarkGray
    Write-Host ""
}
