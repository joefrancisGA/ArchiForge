# Mutation testing (Stryker.NET) — scaffolding

## Why

**Unit tests** prove code runs; **mutation tests** ask whether assertions would fail if the implementation changed slightly. Stryker.NET mutates compiled code and re-runs tests to highlight weak or missing assertions.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the repo `global.json`.
- **Local tool (repo root):** `.config/dotnet-tools.json` pins **`dotnet-stryker`**. Run `dotnet tool restore` once per clone, then `dotnet dotnet-stryker`.

## Configuration

The repo includes **`stryker-config.json`** at the solution root for **Persistence**, plus:

- **`stryker-config.application.json`** — `ArchiForge.Application` + `ArchiForge.Application.Tests`
- **`stryker-config.agentruntime.json`** — `ArchiForge.AgentRuntime` + `ArchiForge.AgentRuntime.Tests`

Scheduled CI runs all three targets (matrix) and uploads separate artifacts (`stryker-report-Persistence`, `…-Application`, `…-AgentRuntime`).

## Commands

From the repository root:

```bash
dotnet tool restore
dotnet dotnet-stryker
dotnet dotnet-stryker -f stryker-config.application.json
dotnet dotnet-stryker -f stryker-config.agentruntime.json
```

## Scheduled CI

GitHub Actions workflow **`.github/workflows/stryker-scheduled.yml`** runs weekly (and on **workflow_dispatch**), restores tools, executes Stryker, and uploads **`StrykerOutput`** as an artifact for review.

HTML reports are emitted under `StrykerOutput` (see Stryker CLI output for the exact path).

## CI

Mutation testing is **not** part of the default GitHub Actions workflow: it is slower than the Tier 1 “fast core” suite. Run it locally or in a scheduled pipeline when changing critical persistence or security code.

## Security / cost

- Runs are **CPU-heavy**; avoid parallel mutation on tiny dev VMs.
- No secrets are required; Stryker does not call Azure.

## Reliability

Flaky tests will show as “survived” or inconsistent mutants. Fix test isolation before trusting mutation scores.
