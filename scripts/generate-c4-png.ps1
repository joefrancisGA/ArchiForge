# Regenerate C4 PNGs under docs/diagrams/c4/ (requires Node.js + npx).
# Usage: from repo root:  .\scripts\generate-c4-png.ps1

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here "..")
$c4 = Join-Path $root "docs\diagrams\c4"

Push-Location $c4
try {
    npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-context.mmd -o c4-context.png -b white
    npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-container.mmd -o c4-container.png -b white
    npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-component-api.mmd -o c4-component-api.png -b white
    Write-Host "OK: PNGs written to $c4"
}
finally {
    Pop-Location
}
