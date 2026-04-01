# Shared triage helpers for run-readiness-check.ps1 and release-smoke.ps1 (56R Prompt 4).
# Dot-source from repo root: . (Join-Path $root 'scripts/OperatorDiagnostics.ps1')

function Write-OperatorPhaseHeader {
    param(
        [Parameter(Mandatory = $true)][string] $Title,
        [Parameter(Mandatory = $true)][int] $Step,
        [Parameter(Mandatory = $true)][int] $Total
    )

    Write-Host ''
    Write-Host "=== [$Step/$Total] $Title ===" -ForegroundColor Cyan
}

function Write-OperatorFailureTriage {
    param(
        [Parameter(Mandatory = $true)][string] $Stage,
        [Parameter(Mandatory = $true)][string] $Category,
        [string[]] $Details = @(),
        [string[]] $NextSteps = @()
    )

    Write-Host ''
    Write-Host '--- FAILURE (triage) ---' -ForegroundColor Red
    Write-Host "Stage:    $Stage" -ForegroundColor Yellow
    Write-Host "Category: $Category" -ForegroundColor Yellow

    foreach ($line in $Details) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            Write-Host $line
        }
    }

    if ($NextSteps.Count -gt 0) {
        Write-Host 'Next:' -ForegroundColor Green

        foreach ($n in $NextSteps) {
            Write-Host "  - $n"
        }
    }

    Write-Host '-------------------------' -ForegroundColor Red
}

function Get-ArchiForgeHttpProbe {
    param(
        [Parameter(Mandatory = $true)][string] $Uri,
        [int] $TimeoutSec = 8
    )

    try {
        $r = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec $TimeoutSec -ErrorAction Stop
        return @{
            Ok         = $true
            StatusCode = [int] $r.StatusCode
            Content    = [string] $r.Content
            Error      = $null
        }
    }
    catch {
        $status = $null
        $body = $null
        $resp = $_.Exception.Response

        if ($null -ne $resp) {
            try {
                $status = [int] $resp.StatusCode
                $stream = $resp.GetResponseStream()

                if ($null -ne $stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $body = $reader.ReadToEnd()
                }
            }
            catch {
                $body = $null
            }
        }

        return @{
            Ok         = $false
            StatusCode = $status
            Content    = $body
            Error      = $_.Exception.Message
        }
    }
}

function Get-ArchiForgeReadinessFailureSummaryLines {
    param(
        [AllowNull()][string] $JsonContent
    )

    $lines = [System.Collections.Generic.List[string]]::new()

    if ([string]::IsNullOrWhiteSpace($JsonContent)) {
        $lines.Add('  (empty or missing readiness response body)')
        return $lines.ToArray()
    }

    try {
        $o = $JsonContent | ConvertFrom-Json

        if ($null -eq $o.entries) {
            $lines.Add('  (no entries[] in JSON — not a detailed health payload)')
            return $lines.ToArray()
        }

        $bad = @($o.entries | Where-Object { $_.status -ne 'Healthy' } | Sort-Object name)
        if ($bad.Count -eq 0) {
            $lines.Add("  overall status in JSON: $($o.status) (no per-entry failures listed)")
            return $lines.ToArray()
        }

        $first = $bad[0]
        $lines.Add("  First failing check (alphabetical among unhealthy): $($first.name)")
        $lines.Add("    status=$($first.status)  description=$($first.description)  error=$($first.error)")

        if ($bad.Count -gt 1) {
            $lines.Add("  Other unhealthy checks ($($bad.Count - 1) more, sorted by name):")

            for ($i = 1; $i -lt $bad.Count; $i++) {
                $e = $bad[$i]
                $lines.Add("    - $($e.name): $($e.status) — $($e.description)")
            }
        }
    }
    catch {
        $lines.Add("  (could not parse readiness JSON: $($_.Exception.Message))")
        $snippet = $JsonContent

        if ($snippet.Length -gt 400) {
            $snippet = $snippet.Substring(0, 400) + '...'
        }

        $lines.Add("  raw snippet: $snippet")
    }

    return $lines.ToArray()
}

function Write-ArchiForgeReadinessTimeoutDiagnostics {
    param(
        [Parameter(Mandatory = $true)][string] $ApiBaseUrl,
        [int] $TimeoutSec = 10
    )

    $base = $ApiBaseUrl.TrimEnd('/')
    Write-Host ''
    Write-Host '--- Readiness probe snapshot (after timeout) ---' -ForegroundColor DarkYellow

    $ready = Get-ArchiForgeHttpProbe -Uri ($base + '/health/ready') -TimeoutSec $TimeoutSec
    Write-Host "GET /health/ready -> HTTP $($ready.StatusCode) ok=$($ready.Ok)"

    if (-not [string]::IsNullOrWhiteSpace($ready.Error)) {
        Write-Host "  transport: $($ready.Error)"
    }

    $detailLines = Get-ArchiForgeReadinessFailureSummaryLines -JsonContent $ready.Content

    foreach ($dl in $detailLines) {
        Write-Host $dl
    }

    $agg = Get-ArchiForgeHttpProbe -Uri ($base + '/health') -TimeoutSec $TimeoutSec
    Write-Host "GET /health -> HTTP $($agg.StatusCode) ok=$($agg.Ok) (aggregate; same order as API health pipeline)"
}
