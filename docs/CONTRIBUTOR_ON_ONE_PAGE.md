> **Scope:** Contributor fast path — install order, one verification command, and where to read next; not buyer narrative, deep architecture, or operator atlas detail.

# Contributor on one page

## Install in this order

1. **Git** and this repo cloned.
2. **Docker Desktop** (or engine + Compose v2) — required for the default demo stack.
3. **.NET 10 SDK** matching [`global.json`](../global.json) — follow **[docs/engineering/INSTALL_ORDER.md](engineering/INSTALL_ORDER.md)** for pinned versions and optional Node **22** (UI work).
4. **SQL** only if you bypass Docker for the API — same install doc.
5. Optional: **.devcontainer** — pre-wires SDK + Node; see install doc § devcontainer.

## Verify it works in 60 seconds

From repo root (Docker running):

```bash
dotnet run --project ArchLucid.Cli -- try
```

Pilot stack up → seed → sample run → committed manifest → first-value report; browser opens on success.

## I want to…

| I want to… | Go here |
| --- | --- |
| **Skim the forced doc tree** | [docs/READ_THIS_FIRST.md](READ_THIS_FIRST.md) |
| **Docker-only first run (no .NET first)** | [docs/engineering/FIRST_30_MINUTES.md](engineering/FIRST_30_MINUTES.md) |
| **Buyer / operator hub after that** | [docs/START_HERE.md](START_HERE.md) |
| **Core pilot / curl / smoke path** | [docs/CORE_PILOT.md](CORE_PILOT.md) |
| **Build, test, migrations** | [docs/engineering/BUILD.md](engineering/BUILD.md) |
| **Architecture index** | [docs/ARCHITECTURE_INDEX.md](ARCHITECTURE_INDEX.md) |
| **One-page C4-style map** | [docs/ARCHITECTURE_ON_ONE_PAGE.md](ARCHITECTURE_ON_ONE_PAGE.md) |
| **Trust / GRC posture** | [docs/trust-center.md](trust-center.md) |

## If something is broken

- **Local failures and ports:** [docs/TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Share evidence:** `dotnet run --project ArchLucid.Cli -- support-bundle --zip` (review before sending).
- **Environment sanity:** `dotnet run --project ArchLucid.Cli -- doctor`
