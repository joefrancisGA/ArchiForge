<#
    .SYNOPSIS
        Single entry point for every ArchLucid local test tier.

    .DESCRIPTION
        Replaces the eight legacy ``test-<tier>.ps1`` scripts with one
        parameterised driver. Pick a tier with ``-Tier <name>``; the
        underlying ``dotnet test`` (or ``npm`` for UI tiers) command is
        documented in the per-tier help in ``docs/TEST_EXECUTION_MODEL.md``.

        The legacy ``test-<tier>.ps1`` scripts continue to work but now
        delegate here, so existing runbooks, CI workflows, and operator
        documentation references stay valid during the migration.

    .PARAMETER Tier
        One of:
          Core                 – xUnit Suite=Core (full Core tier)
          FastCore             – same filter as CI corset: Core minus Slow, Integration, GoldenCorpusRecord
          OpenApiContract      – OpenAPI ``/openapi/v1.json`` snapshot (``scripts/ci/check_openapi_contract_snapshot.ps1``; CI gate)
          Full                 – entire ``ArchLucid.sln`` test run
          Integration           – xUnit Category=Integration
          Slow                  – xUnit Category=Slow
          SqlServerIntegration  – xUnit Category=SqlServerContainer (requires
                                  ARCHLUCID_SQL_TEST connection string)
          UiSmoke               – Playwright smoke for ``archlucid-ui``
          UiUnit                – Vitest unit suite for ``archlucid-ui``

    .PARAMETER ListTiers
        Print the supported tier names and exit 0 without running anything.

    .PARAMETER HeartbeatSeconds
        While ``dotnet test`` is running, print a short status line at this interval (elapsed time + project).
        Use ``0`` to disable. UI tiers (e.g. UiUnit) use the same interval while npm runs.

    .EXAMPLE
        .\test.ps1 -Tier FastCore
        .\test.ps1 -Tier UiSmoke
        .\test.ps1 -ListTiers
        .\test.ps1 -Tier Full -HeartbeatSeconds 0
#>
[CmdletBinding(DefaultParameterSetName = 'Run')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Run', Position = 0)]
    [ValidateSet(
        'Core',
        'FastCore',
        'OpenApiContract',
        'Full',
        'Integration',
        'Slow',
        'SqlServerIntegration',
        'UiSmoke',
        'UiUnit'
    )]
    [string] $Tier,

    [Parameter(Mandatory = $true, ParameterSetName = 'List')]
    [switch] $ListTiers,

    [Parameter(ParameterSetName = 'Run')]
    [ValidateRange(0, 86400)]
    [int] $HeartbeatSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# Tier name → human-readable description (printed by -ListTiers and on dispatch).
# Adding a tier = add an entry here AND a case in Invoke-Tier below.
$tierDescriptions = [ordered]@{
    'Core'                 = 'xUnit Suite=Core (full Core tier)'
    'FastCore'             = 'Suite=Core minus Slow, Integration, GoldenCorpusRecord (matches CI corset)'
    'OpenApiContract'      = 'OpenAPI v1 contract snapshot test (scripts/ci/check_openapi_contract_snapshot.ps1)'
    'Full'                 = 'entire ArchLucid.sln test run'
    'Integration'          = 'xUnit Category=Integration'
    'Slow'                 = 'xUnit Category=Slow'
    'SqlServerIntegration' = 'xUnit Category=SqlServerContainer (requires ARCHLUCID_SQL_TEST)'
    'UiSmoke'              = 'Playwright smoke for archlucid-ui'
    'UiUnit'               = 'Vitest unit suite for archlucid-ui'
}

if ($ListTiers) {
    Write-Host 'ArchLucid test tiers:'
    foreach ($entry in $tierDescriptions.GetEnumerator()) {
        Write-Host ('  {0,-22} {1}' -f $entry.Key, $entry.Value)
    }
    exit 0
}

function Start-TestHeartbeat {
    param(
        [System.Threading.CancellationToken] $Token,
        [TimeSpan] $Interval,
        [string] $Label
    )

    if ($Interval -le [TimeSpan]::Zero) {
        return $null
    }

    # Task.Run is ambiguous with a raw scriptblock; bind explicitly to Run(Action).
    $startedUtc = [datetime]::UtcNow
    $tok = $Token
    $everyMs = [int]$Interval.TotalMilliseconds
    $text = $Label
    $action = [System.Action]{
        while (-not $tok.IsCancellationRequested) {
            $sliceMs = 200
            $remainMs = $everyMs
            while ($remainMs -gt 0 -and -not $tok.IsCancellationRequested) {
                $thisSlice = [Math]::Min($sliceMs, $remainMs)
                Start-Sleep -Milliseconds $thisSlice
                $remainMs -= $thisSlice
            }

            if ($tok.IsCancellationRequested) {
                break
            }

            $elapsed = [int]([DateTime]::UtcNow - $startedUtc).TotalSeconds
            Write-Host ''
            Write-Host ("[{0:yyyy-MM-dd HH:mm:ss}Z] Heartbeat: still running - {1} (elapsed {2}s)" -f [DateTime]::UtcNow, $text, $elapsed) -ForegroundColor DarkYellow
            Write-Host ''
        }
    }
    return [System.Threading.Tasks.Task]::Run($action)
}

function Invoke-DotnetTest {
    param(
        [string] $Project,
        [string] $Filter,
        [int] $HeartbeatSec
    )

    $dotnetArgs = @(
        'test', $Project,
        '--verbosity', 'normal',
        '--logger', 'console;verbosity=detailed'
    )

    if ($Filter) {
        $dotnetArgs += @('--filter', $Filter)
    }

    $cancel = New-Object System.Threading.CancellationTokenSource
    $hbTask = $null
    if ($HeartbeatSec -gt 0) {
        $interval = [TimeSpan]::FromSeconds($HeartbeatSec)
        $hbTask = Start-TestHeartbeat -Token $cancel.Token -Interval $interval -Label "dotnet test $Project"
    }

    Write-Host ("Running: dotnet {0}" -f ($dotnetArgs -join ' '))
    Write-Host ''

    $exit = -1
    try {
        & dotnet @dotnetArgs
        $exit = $LASTEXITCODE
    }
    finally {
        if ($null -ne $cancel) {
            $cancel.Cancel()
        }

        if ($null -ne $hbTask) {
            try {
                $null = $hbTask.Wait(8000)
            }
            catch {
                # Best-effort wait for heartbeat task to exit.
            }
        }
    }

    return $exit
}

function Invoke-UiCommand {
    param(
        [string[]] $Steps,
        [int] $HeartbeatSec
    )

    Set-Location (Join-Path $root 'archlucid-ui')

    # Use .cmd shims on Windows so StrictMode does not load npm.ps1 (breaks on $MyInvocation.Statement).
    $npm = if (Get-Command npm.cmd -ErrorAction SilentlyContinue) { 'npm.cmd' } else { 'npm' }
    $npx = if (Get-Command npx.cmd -ErrorAction SilentlyContinue) { 'npx.cmd' } else { 'npx' }

    foreach ($step in $Steps) {
        $cancel = New-Object System.Threading.CancellationTokenSource
        $hbTask = $null
        if ($HeartbeatSec -gt 0) {
            $interval = [TimeSpan]::FromSeconds($HeartbeatSec)
            $hbTask = Start-TestHeartbeat -Token $cancel.Token -Interval $interval -Label "archlucid-ui: $step"
        }

        try {
            switch ($step) {
                'install'           { & $npm ci }
                'playwright-deps'   { & $npx playwright install --with-deps chromium }
                'test:e2e'          { & $npm run test:e2e }
                'test'              { & $npm run test }
                default             { throw "Unknown UI step: $step" }
            }

            if ($LASTEXITCODE -ne 0) { return $LASTEXITCODE }
        }
        finally {
            $cancel.Cancel()
            if ($null -ne $hbTask) {
                try {
                    $null = $hbTask.Wait(8000)
                }
                catch {
                    # Best-effort wait for heartbeat task to exit.
                }
            }
        }
    }

    return 0
}

function Invoke-Tier {
    param(
        [string] $Selected,
        [int] $TierHeartbeatSeconds
    )

    Write-Host "ArchLucid test driver - running tier: $Selected"
    Write-Host "  $($tierDescriptions[$Selected])"
    if ($TierHeartbeatSeconds -gt 0) {
        Write-Host ("  Heartbeat every {0}s (use -HeartbeatSeconds 0 to disable)" -f $TierHeartbeatSeconds)
    }

    Write-Host ''

    switch ($Selected) {
        'Core' {
            return (Invoke-DotnetTest -Project 'ArchLucid.sln' -Filter 'Suite=Core' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'FastCore' {
            return (Invoke-DotnetTest -Project 'ArchLucid.sln' -Filter 'Suite=Core&Category!=Slow&Category!=Integration&Category!=GoldenCorpusRecord' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'OpenApiContract' {
            $script = Join-Path $root 'scripts/ci/check_openapi_contract_snapshot.ps1'
            & $script
            return $LASTEXITCODE
        }
        'Full' {
            return (Invoke-DotnetTest -Project 'ArchLucid.sln' -Filter '' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'Integration' {
            return (Invoke-DotnetTest -Project 'ArchLucid.sln' -Filter 'Category=Integration' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'Slow' {
            return (Invoke-DotnetTest -Project 'ArchLucid.sln' -Filter 'Category=Slow' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'SqlServerIntegration' {
            return (Invoke-DotnetTest -Project 'ArchLucid.Persistence.Tests' -Filter 'Category=SqlServerContainer' -HeartbeatSec $TierHeartbeatSeconds)
        }
        'UiSmoke' {
            return (Invoke-UiCommand `
                -Steps @('install', 'playwright-deps', 'test:e2e') `
                -HeartbeatSec $TierHeartbeatSeconds)
        }
        'UiUnit' {
            return (Invoke-UiCommand -Steps @('install', 'test') -HeartbeatSec $TierHeartbeatSeconds)
        }
        default {
            throw ("Unhandled tier '" + $Selected + "' - add a case in Invoke-Tier.")
        }
    }
}

$exit = Invoke-Tier -Selected $Tier -TierHeartbeatSeconds $HeartbeatSeconds
exit $exit
