$p = Join-Path (Split-Path -Parent $PSScriptRoot) 'ArchLucid.Decisioning'
$p = [IO.Path]::GetFullPath($p)
Write-Host "Path=$p exists=$(Test-Path -LiteralPath $p)"
$one = Join-Path $p 'Advisory\Scheduling\ArchitectureDigestBuilder.cs'
$raw = Get-Content -LiteralPath $one -Raw
Write-Host "HasLF=$($raw.Contains([char]10)) HasCR=$($raw.Contains([char]13))"
Write-Host "RawLen=$($raw.Length)"
$parts = [regex]::Split($raw, '\r\n|\n|\r')
Write-Host "SplitCount=$($parts.Length) firstLen=$($parts[0].Length)"
$list = [System.Collections.Generic.List[string]]::new()
$list.AddRange([string[]]$parts)
Write-Host "Lines=$($list.Count)"
$hits = 0
for ($i = 0; $i -lt $list.Count; $i++) {
    $trim = $list[$i].TrimStart()
    if ($trim.Length -eq 0) { continue }
    if ($trim -match '^(if|foreach)\s*\(') {
        Write-Host "hit $i : $($list[$i])"
        $hits++
    }
}
Write-Host "hits=$hits"
