param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [string]$OutputDirectory = "artifacts/staging-readiness",

    [string]$AuthMode = "unspecified",

    [switch]$RunDoctor,

    [switch]$RunRcDrill,

    [string]$ApiKey = ""
)

$ErrorActionPreference = "Stop"

function Add-Result {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )

    $safeDetail = ($Detail -replace "\r?\n", "<br>")
    $Lines.Add("| $Name | $Status | $safeDetail |")
}

function Invoke-JsonProbe {
    param(
        [string]$Url,
        [hashtable]$Headers
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -Headers $Headers -Method Get -UseBasicParsing -TimeoutSec 30
        return @{
            Status = "PASS"
            Detail = "HTTP $($response.StatusCode); $($response.Content.Substring(0, [Math]::Min(400, $response.Content.Length)))"
        }
    }
    catch {
        return @{
            Status = "FAIL"
            Detail = $_.Exception.Message
        }
    }
}

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$outputRoot = Join-Path (Get-Location) $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$outputPath = Join-Path $outputRoot "staging-readiness-$timestamp.md"

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
    $headers["X-Api-Key"] = $ApiKey
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# ArchLucid staging readiness evidence")
$lines.Add("")
$lines.Add("| Field | Value |")
$lines.Add("| --- | --- |")
$lines.Add("| Generated UTC | $timestamp |")
$lines.Add("| Base URL | $normalizedBaseUrl |")
$lines.Add("| Auth mode declared | $AuthMode |")
$lines.Add("| API key supplied | $(-not [string]::IsNullOrWhiteSpace($ApiKey)) |")
$lines.Add("")
$lines.Add("## Probe Results")
$lines.Add("")
$lines.Add("| Check | Status | Detail |")
$lines.Add("| --- | --- | --- |")

$live = Invoke-JsonProbe -Url "$normalizedBaseUrl/health/live" -Headers $headers
Add-Result -Lines $lines -Name "/health/live" -Status $live.Status -Detail $live.Detail

$ready = Invoke-JsonProbe -Url "$normalizedBaseUrl/health/ready" -Headers $headers
Add-Result -Lines $lines -Name "/health/ready" -Status $ready.Status -Detail $ready.Detail

$version = Invoke-JsonProbe -Url "$normalizedBaseUrl/version" -Headers $headers
Add-Result -Lines $lines -Name "/version" -Status $version.Status -Detail $version.Detail

if ($RunDoctor) {
    try {
        $env:ARCHLUCID_API_URL = $normalizedBaseUrl
        $doctorOutput = dotnet run --project ArchLucid.Cli -- doctor 2>&1 | Out-String
        Add-Result -Lines $lines -Name "archlucid doctor" -Status "PASS" -Detail $doctorOutput.Trim()
    }
    catch {
        Add-Result -Lines $lines -Name "archlucid doctor" -Status "FAIL" -Detail $_.Exception.Message
    }
}
else {
    Add-Result -Lines $lines -Name "archlucid doctor" -Status "SKIPPED" -Detail "Pass -RunDoctor to execute."
}

if ($RunRcDrill) {
    try {
        $rcOutput = .\v1-rc-drill.ps1 -BaseUrl $normalizedBaseUrl 2>&1 | Out-String
        Add-Result -Lines $lines -Name "v1-rc-drill" -Status "PASS" -Detail $rcOutput.Trim()
    }
    catch {
        Add-Result -Lines $lines -Name "v1-rc-drill" -Status "FAIL" -Detail $_.Exception.Message
    }
}
else {
    Add-Result -Lines $lines -Name "v1-rc-drill" -Status "SKIPPED" -Detail "Pass -RunRcDrill to execute against the target API."
}

$lines.Add("")
$lines.Add("## Notes")
$lines.Add("")
$lines.Add("- This evidence file records probe outcomes for one target environment; it does not certify unrelated customer infrastructure.")
$lines.Add("- Do not paste secrets into this file. API key presence is recorded as a boolean only.")
$lines.Add("- Attach support bundles separately after reviewing redaction.")
$lines.Add("")

$lines | Set-Content -Path $outputPath -Encoding UTF8
Write-Host "Wrote $outputPath"
