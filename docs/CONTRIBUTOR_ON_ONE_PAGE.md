> **Deprecated as primary entry (2026-04-27):** **[`START_HERE.md`](START_HERE.md)** §3 is the contributor path. This page remains a one-pager shortcut.

> **Scope:** Contributor fast path — install order, one verification command, and where to read next; not buyer narrative, deep architecture, or operator atlas detail.

# Contributor on one page

**ArchLucid** is an AI-assisted architecture workflow (structured request → run → committed manifest, with exports and governance hooks). **Onboarding entry:** [START_HERE.md](START_HERE.md).

## Common tasks

| I want to… | Go here |
| --- | --- |
| **Docker-first boot (no .NET first)** | [docs/engineering/FIRST_30_MINUTES.md](engineering/FIRST_30_MINUTES.md) |
| **Build, test, migrations** | [docs/engineering/BUILD.md](engineering/BUILD.md) |
| **Architecture poster (C4)** | [docs/ARCHITECTURE_ON_ONE_PAGE.md](ARCHITECTURE_ON_ONE_PAGE.md) |
| **HTTP API contracts** | [docs/library/API_CONTRACTS.md](library/API_CONTRACTS.md) |
| **Deployment (internal operators)** | [docs/engineering/DEPLOYMENT.md](engineering/DEPLOYMENT.md) |

## Copy-paste (repo root)

```bash
dotnet build ArchLucid.sln
dotnet test ArchLucid.sln
docker compose up -d
```

## Verify in one shot (Docker running)

```bash
dotnet run --project ArchLucid.Cli -- try
```

## Install + troubleshooting

[docs/engineering/INSTALL_ORDER.md](engineering/INSTALL_ORDER.md) — pinned SDK/SQL/Node. [docs/TROUBLESHOOTING.md](TROUBLESHOOTING.md) — ports and local failures. `dotnet run --project ArchLucid.Cli -- doctor` · `dotnet run --project ArchLucid.Cli -- support-bundle --zip` (review before sharing).
