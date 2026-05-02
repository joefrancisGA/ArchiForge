# Installs the shared pre-commit hook from scripts/hooks/ into .git/hooks/.
# Run once after cloning: pwsh scripts/install-git-hooks.ps1
#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$HooksSrc   = Join-Path $ScriptDir 'hooks'
$GitDir     = & git -C $ScriptDir rev-parse --git-dir
$GitHooksDir = Join-Path $ScriptDir $GitDir 'hooks'

function Install-Hook {
    param([string]$Name)
    $src  = Join-Path $HooksSrc $Name
    $dest = Join-Path $GitHooksDir $Name

    if (-not (Test-Path $src)) {
        Write-Host "  skip: $Name (no source file)"
        return
    }

    Copy-Item -Force $src $dest
    Write-Host "  installed: $Name"
}

Write-Host "Installing git hooks from $HooksSrc -> $GitHooksDir"
Install-Hook 'pre-commit'
Write-Host 'Done. Run ''git commit --no-verify'' to bypass in emergencies.'
