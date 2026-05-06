# Fixes blank lines before "if" / "foreach" in ArchLucid.Decisioning per project style rules.
# Exempt: first statement immediately inside a method/ctor-like opening "{" (prev non-blank line ends with ")" and is not for/while/if/...).

function Test-MethodLikeOpenBrace {
    param(
        [System.Collections.Generic.List[string]]$List,
        [int]$OpenBraceIndex
    )

    $k = $OpenBraceIndex - 1
    while ($k -ge 0 -and $List[$k].Trim().Length -eq 0) {
        $k--
    }

    if ($k -lt 0) {
        return $false
    }

    $trim = $List[$k].Trim()
    if ($trim -match '^\s*#') { return $false }
    if ($trim -match '^\s*(if|while|for|foreach|switch|catch|using|fixed|lock)\s*\(') { return $false }
    if ($trim -match '^\s*else\s+if\s*\(') { return $false }

    return [bool]($trim -match '\)\s*$')
}

function Test-IsControlLine {
    param([string]$Line)

    $trim = $Line.TrimStart()
    if ($trim.Length -eq 0) { return $false }
    if ($trim.StartsWith('#')) { return $false }
    if ($trim.StartsWith('//')) { return $false }

    return [bool]($trim -match '^(if|foreach)\s*\(')
}

function Fix-File {
    param([string]$Path)

    $raw = Get-Content -LiteralPath $Path -Raw
    if ($null -eq $raw) {
        return $false
    }

    $parts = [regex]::Split($raw, '\r\n|\n|\r')
    $list = [System.Collections.Generic.List[string]]::new()
    $list.AddRange([string[]]$parts)
    $targets = [System.Collections.Generic.List[int]]::new()

    for ($i = 0; $i -lt $list.Count; $i++) {
        if (Test-IsControlLine $list[$i]) {
            [void]$targets.Add($i)
        }
    }

    [int[]]$sorted = $targets | Sort-Object -Descending
    $changed = $false

    foreach ($i in $sorted) {
        if ($i -ge $list.Count -or -not (Test-IsControlLine $list[$i])) {
            continue
        }

        $blankRun = 0
        $b = $i - 1
        while ($b -ge 0 -and $list[$b].Trim().Length -eq 0) {
            $blankRun++
            $b--
        }

        $j = $b
        $firstStatementInBlock = $false
        if ($j -ge 0 -and $list[$j].Trim() -eq '{') {
            $onlyBlanks = $true
            for ($t = $j + 1; $t -lt $i; $t++) {
                if ($list[$t].Trim().Length -ne 0) {
                    $onlyBlanks = $false
                    break
                }
            }

            $firstStatementInBlock = $onlyBlanks
        }

        $exempt = $false
        if ($firstStatementInBlock) {
            $exempt = Test-MethodLikeOpenBrace $list $j
        }

        $desiredBlank = 1
        if ($exempt) {
            $desiredBlank = 0
        }

        if ($blankRun -lt $desiredBlank) {
            $add = $desiredBlank - $blankRun
            for ($n = 0; $n -lt $add; $n++) {
                $list.Insert($i, '')
                $changed = $true
            }
        }
        elseif ($blankRun -gt $desiredBlank) {
            $removeCount = $blankRun - $desiredBlank
            $startIdx = $i - $blankRun
            $list.RemoveRange($startIdx, $removeCount)
            $changed = $true
        }
    }

    if (-not $changed) {
        return $false
    }

    $newText = [string]::Join([Environment]::NewLine, $list) + [Environment]::NewLine
    Set-Content -LiteralPath $Path -Value $newText -NoNewline -Encoding utf8

    return $true
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$root = Join-Path $repoRoot 'ArchLucid.Decisioning'
$root = [System.IO.Path]::GetFullPath($root)
if (-not (Test-Path -LiteralPath $root)) {
    throw "Decisioning path not found: $root (PSScriptRoot=$PSScriptRoot)"
}

$edited = 0
Get-ChildItem -LiteralPath $root -Recurse -Filter '*.cs' | ForEach-Object {
    if (Fix-File $_.FullName) {
        $edited++
    }
}

Write-Host "Updated $edited file(s)."
