#Requires -Version 7.0
<#
.SYNOPSIS
    Processes a saved ArchLucid CloudEvents JSON file and creates Jira issue(s) via REST API v3 — companion to jira-webhook-bridge.mjs.

.DESCRIPTION
    Same environment variables as the Node bridge. For `com.archlucid.authority.run.completed`, GETs run detail from ArchLucid
    and creates one Jira issue per finding (capped by MAX_FINDINGS_PER_RUN). For `com.archlucid.alert.fired`, creates one issue.

    Set SKIP_HMAC=1 when replaying a file without computing X-ArchLucid-Webhook-Signature. Do not use SKIP_HMAC in production.

.PARAMETER ProcessPath
    Path to a UTF-8 CloudEvents JSON file (body as ArchLucid would POST).

.EXAMPLE
    $env:SKIP_HMAC='1'
    $env:JIRA_BASE_URL='https://your.atlassian.net'
    $env:JIRA_EMAIL='you@example.com'
    $env:JIRA_API_TOKEN='<api token>'
    $env:JIRA_PROJECT_KEY='ARCH'
    $env:ARCHLUCID_API_KEY='<key>'   # required for run.completed
    $env:ARCHLUCID_BASE_URL='https://your-api'
    ./jira-webhook-bridge.ps1 -ProcessPath .\sample-alert-fired.json
#>
param(
    [Parameter(Mandatory)][string] $ProcessPath
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

function Get-EnvOptional([string]$Name, [string]$Default = "") {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
    return $value.Trim()
}

function ConvertTo-JiraPriority([string]$Severity) {
    switch -Regex ($Severity.ToLowerInvariant()) {
        '^(critical|0)$' { return "Highest" }

        '^(high|1)$' { return "High" }

        '^(medium|2)$' { return "Medium" }

        '^(low|info|3|4)$' { return "Low" }

        default { return "Medium" }

    }

}

function Build-JiraAdf([string[]]$Lines) {
    $blocks = foreach ($line in $Lines) {

        @{
            type     = "paragraph"
            content = @(@{ type = "text"; text = $line })
        }

    }

    return @{
        type    = "doc"
        version = 1

        content = @($blocks)

    }

}

function Invoke-ArchLucidGetJson {

    param([string] $Url, [string] $ApiKey)

    $hdr = @{
        "X-Api-Key" = $ApiKey
        Accept      = "application/json"
    }

    return Invoke-RestMethod -Uri $Url -Headers $hdr -Method Get

}

function Invoke-JiraCreate {

    param([hashtable] $Body, [string] $JiraBase, [string] $Email, [string] $Token)

    $pair = "{0}:{1}" -f $Email, $Token
    $basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
    $uri = ($JiraBase.TrimEnd('/')) + "/rest/api/3/issue"
    $hdr = @{
        Authorization  = "Basic $basic"
        Accept         = "application/json"
        "Content-Type" = "application/json"
    }

    return Invoke-RestMethod -Uri $uri -Headers $hdr -Method Post -Body ($Body | ConvertTo-Json -Depth 30 -Compress)

}

$raw = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $ProcessPath), [System.Text.UTF8Encoding]::new($false))

$skipHmac = [Environment]::GetEnvironmentVariable("SKIP_HMAC") -eq "1"
$secret = Get-EnvOptional "ARCHLUCID_WEBHOOK_HMAC_SECRET"

if ((-not $skipHmac) -and (-not [string]::IsNullOrWhiteSpace($secret))) {
    throw "File mode: set SKIP_HMAC=1 or clear ARCHLUCID_WEBHOOK_HMAC_SECRET (HMAC signs raw HTTP POST bodies; replay files omit the signature header)."
}

$ce = $raw | ConvertFrom-Json
$typeStr = [string] $ce.type

$jiraBase = Get-EnvOptional "JIRA_BASE_URL"
$email = Get-EnvOptional "JIRA_EMAIL"
$jtok = Get-EnvOptional "JIRA_API_TOKEN"

$proj = Get-EnvOptional "JIRA_PROJECT_KEY"
$issueTask = Get-EnvOptional "JIRA_ISSUE_TYPE" "Task"
$issueAlert = Get-EnvOptional "JIRA_ISSUE_ALERT_TYPE" (Get-EnvOptional "ALERT_ISSUE_TYPE" "Bug")
$maxFindings = [int] (Get-EnvOptional "MAX_FINDINGS_PER_RUN" "25")

if ([string]::IsNullOrWhiteSpace($jiraBase) -or [string]::IsNullOrWhiteSpace($email) -or [string]::IsNullOrWhiteSpace($jtok) -or [string]::IsNullOrWhiteSpace($proj)) {
    throw "Set JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, JIRA_PROJECT_KEY."

}

$created = [System.Collections.ArrayList] @()

if ($typeStr -eq "com.archlucid.alert.fired") {
    $d = $ce.data
    $sum = "[ArchLucid Alert] $($d.title)"

    $lines = @(
        "Alert ID: $($d.alertId)"

        "Severity: $($d.severity)"

        "Category: $($d.category)"

        "Rule ID: $($d.ruleId)"

        "Deduplication: $($d.deduplicationKey)"

        "Run ID: $($d.runId)"
    )

    $body = @{
        fields = @{
            project = @{ key = $proj }
            issuetype = @{ name = $issueAlert }
            summary = $sum.Substring(0, [Math]::Min(250, $sum.Length))
            priority = @{ name = (ConvertTo-JiraPriority ([string]$d.severity)) }
            description = (Build-JiraAdf $lines)
        }

    }

    [void]$created.Add((Invoke-JiraCreate -Body $body -JiraBase $jiraBase -Email $email -Token $jtok))

}


elseif ($typeStr -eq "com.archlucid.authority.run.completed") {

    $archBase = Get-EnvOptional "ARCHLUCID_BASE_URL"

    $apiKey = Get-EnvOptional "ARCHLUCID_API_KEY"

    if ([string]::IsNullOrWhiteSpace($archBase) -or [string]::IsNullOrWhiteSpace($apiKey)) {
        throw "run.completed requires ARCHLUCID_BASE_URL and ARCHLUCID_API_KEY."

    }


    $rid = [string] $ce.data.runId

    if ([string]::IsNullOrWhiteSpace($rid)) { throw "data.runId missing." }

    $url = ($archBase.TrimEnd('/')) + "/v1/authority/runs/$([uri]::EscapeDataString($rid))"
    $detail = Invoke-ArchLucidGetJson -Url $url -ApiKey $apiKey


    $findings = @()

    if ($null -ne $detail.findingsSnapshot -and $null -ne $detail.findingsSnapshot.findings) {

        $findings = @($detail.findingsSnapshot.findings)

    }



    $n = 0

    foreach ($f in $findings) {

        if ($n -ge $maxFindings) { break }

        $n++
        $title = "[ArchLucid] $(if ([string]::IsNullOrWhiteSpace($f.title)) { $f.findingId } else { $f.title })"

        $descLines = @(
            "Run ID: $rid"

            "Finding ID: $($f.findingId)"

            "Severity: $($f.severity)"

            "Category: $($f.category)"

            ""

            [string]$f.rationale
        )

        $body = @{
            fields = @{
                project = @{ key = $proj }
                issuetype = @{ name = $issueTask }
                summary = $title.Substring(0, [Math]::Min(250, $title.Length))
                priority = @{ name = (ConvertTo-JiraPriority ([string]$f.severity)) }

                description = (Build-JiraAdf $descLines)

            }


        }


        [void]$created.Add((Invoke-JiraCreate -Body $body -JiraBase $jiraBase -Email $email -Token $jtok))


    }



    if ($findings.Count -eq 0) {

        $fallbackSummary = "[ArchLucid] Run $rid (no findings in snapshot)"

        $body = @{


            fields = @{


                project = @{ key = $proj }


                issuetype = @{ name = $issueTask }


                summary = $fallbackSummary.Substring(0, [Math]::Min(250, $fallbackSummary.Length))


                description = (Build-JiraAdf @("authority.run.completed webhook — findings empty for $rid."))


            }


        }


        [void]$created.Add((Invoke-JiraCreate -Body $body -JiraBase $jiraBase -Email $email -Token $jtok))


    }



}


else {




    Write-Warning "Ignored CloudEvents type: $typeStr"


}




$created | ConvertTo-Json -Depth 10
