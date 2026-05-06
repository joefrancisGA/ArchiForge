$root = Join-Path $PSScriptRoot ".." "ArchLucid.Decisioning"
$root = [System.IO.Path]::GetFullPath($root)
$files = Get-ChildItem -Path $root -Recurse -Filter "*.cs" | ForEach-Object { $_.FullName }
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $raw = Get-Content -Path $file -Raw -ErrorAction SilentlyContinue
    if ($null -eq $raw) { continue }

    $arr = $raw -split "`r?`n"

    for ($i = 0; $i -lt $arr.Length; $i++) {
        $line = [string]$arr[$i]
        $trim = $line.TrimStart()
        if ($trim.Length -eq 0) { continue }
        if ($trim.StartsWith("//")) { continue }

        $isIf = $trim -match '^if\s*\('
        $isForeach = $trim -match '^foreach\s*\('
        if (-not $isIf -and -not $isForeach) { continue }

        $blankRun = 0
        $b = $i - 1
        while ($b -ge 0 -and $arr[$b].Trim().Length -eq 0) { $blankRun++; $b-- }

        $j = $b
        $prevNonEmpty = if ($j -ge 0) { $arr[$j].Trim() } else { "" }

        $firstInMethod = $false
        if ($prevNonEmpty -eq "{") {
            $ctxStart = [Math]::Max(0, $j - 15)
            $ctx = ($arr[$ctxStart..$j] | ForEach-Object { $_.TrimEnd() }) -join "`n"
            if ($ctx -match '\)\s*=>') { $firstInMethod = $true }

            $k = $j - 1
            while ($k -ge 0 -and $arr[$k].Trim().Length -eq 0) { $k-- }

            if (-not $firstInMethod -and $k -ge 0) {
                $beforeBrace = $arr[$k].Trim()
                if ($beforeBrace -match '^\)\s*$') { $firstInMethod = $true }
            }
        }

        $snippet = $trim.Substring(0, [Math]::Min(70, $trim.Length))

        if (-not $firstInMethod) {
            if ($blankRun -ne 1) {
                [void]$violations.Add("$file`:$($i+1): want 1 blank, have $blankRun prev=`"$prevNonEmpty`" :: $snippet")
            }
        }
        elseif ($blankRun -ne 0) {
            [void]$violations.Add("$file`:$($i+1): first-in-method want 0 blanks, have $blankRun :: $snippet")
        }
    }
}

$violations | Sort-Object
Write-Host "--- count: $($violations.Count) ---"
