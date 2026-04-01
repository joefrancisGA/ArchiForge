# Pilot / RC gate: Release build, fast-core tests in Release, optional UI Vitest. See docs/RELEASE_LOCAL.md
param(
    [switch] $SkipUi
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

. (Join-Path (Join-Path $root 'scripts') 'OperatorDiagnostics.ps1')

$nodeAvailable = $null -ne (Get-Command node -ErrorAction SilentlyContinue)
$totalPhases = 2

if ((-not $SkipUi) -and $nodeAvailable) {
    $totalPhases = 3
}

Write-OperatorPhaseHeader -Title 'Release build (ArchiForge.sln, -c Release)' -Step 1 -Total $totalPhases
& (Join-Path $root 'build-release.ps1')

if ($LASTEXITCODE -ne 0) {
    Write-OperatorFailureTriage -Stage '1 Release build' -Category 'BuildOrRestoreFailure' `
        -Details @('dotnet build or restore exited non-zero (see compiler output above).') `
        -NextSteps @(
        'Fix compile errors, then re-run: .\build-release.ps1',
        'Full log: dotnet build ArchiForge.sln -c Release --nologo'
    )
    exit $LASTEXITCODE
}

Write-OperatorPhaseHeader -Title 'Fast core tests (Release, no rebuild)' -Step 2 -Total $totalPhases
dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"

if ($LASTEXITCODE -ne 0) {
    Write-OperatorFailureTriage -Stage '2 Fast core tests' -Category 'TestFailure' `
        -Details @(
        'The first failing test name appears above in xUnit output (scroll up).',
        'Exit code is non-zero from dotnet test.'
    ) `
        -NextSteps @(
        'Re-run the same filter locally: dotnet test ArchiForge.sln -c Release --no-build --filter "Suite=Core&Category!=Slow&Category!=Integration"',
        'Narrow further: dotnet test <TestProject>.csproj -c Release --filter "FullyQualifiedName~PartialName"',
        'If failures mention SQL: some Core tests may need a server; compare with CI matrix in docs/TEST_STRUCTURE.md'
    )
    exit $LASTEXITCODE
}

if (-not $SkipUi) {
    $node = Get-Command node -ErrorAction SilentlyContinue

    if ($null -ne $node) {
        Write-OperatorPhaseHeader -Title 'Operator UI unit tests (Vitest)' -Step 3 -Total $totalPhases
        $uiRoot = Join-Path $root 'archiforge-ui'
        Set-Location $uiRoot
        npm ci

        if ($LASTEXITCODE -ne 0) {
            Set-Location $root
            Write-OperatorFailureTriage -Stage '3 UI unit tests' -Category 'NpmCiFailure' `
                -Details @('npm ci failed in archiforge-ui (lockfile / registry / network).') `
                -NextSteps @(
                'cd archiforge-ui; npm ci',
                'Confirm Node 22+ and a clean node_modules if needed',
                'To skip UI gate: .\run-readiness-check.ps1 -SkipUi'
            )
            exit $LASTEXITCODE
        }

        npm run test
        Set-Location $root

        if ($LASTEXITCODE -ne 0) {
            Write-OperatorFailureTriage -Stage '3 UI unit tests' -Category 'VitestFailure' `
                -Details @('Vitest reported failures (see file names above).') `
                -NextSteps @(
                'cd archiforge-ui; npm run test',
                'Run a single file: npx vitest run path/to/file.test.ts',
                'To skip UI gate: .\run-readiness-check.ps1 -SkipUi'
            )
            exit $LASTEXITCODE
        }
    }
    else {
        Write-Warning 'Node.js not on PATH; skipped UI unit tests. Use -SkipUi for a quiet skip, or install Node 22+.'
    }
}

Write-Host ''
Write-Host '=== Readiness check finished successfully ===' -ForegroundColor Green
exit 0
