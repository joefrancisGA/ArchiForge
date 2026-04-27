> **Scope:** Deeper-than-`READ_THIS_FIRST` contributor routing — who starts where. Linked from the repository **[`README.md`](../../README.md)**.

# Contributor persona table

| You are a... | Start here (contributor / internal-operator path) |
|---|---|
| **First-time contributor / internal operator** (Docker only, no .NET / Node / cloud keys) | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/engineering/FIRST_30_MINUTES.md`](../engineering/FIRST_30_MINUTES.md)** — or, if you have the .NET 10 SDK locally, run **`dotnet run --project ArchLucid.Cli -- try`** for a single-command first-value loop (pilot up → seed → sample run → committed manifest → first-value Markdown report → operator UI opens). Same demo stack; zero questions. Even faster: open the repo in the **`.devcontainer/`** (.NET 10 + Node 22, runs `archlucid try` on first boot — see [`docs/library/CLI_USAGE.md`](CLI_USAGE.md#archlucid-try)). |
| **Contributor — one-line stack from a .NET SDK checkout** (same Docker demo as `scripts/demo-start.ps1`) | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then from repo root: `dotnet run --project ArchLucid.Cli -- pilot up` — then open **http://localhost:3000/runs/new** (see [`docs/engineering/FIRST_30_MINUTES.md`](../engineering/FIRST_30_MINUTES.md)) |
| **Internal operator running a real pilot** (curl, CLI, release smoke) | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/CORE_PILOT.md`](../CORE_PILOT.md)** |
| **Developer** about to commit code | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/engineering/INSTALL_ORDER.md`](../engineering/INSTALL_ORDER.md)** |
| **SRE / Platform** owner | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/engineering/INSTALL_ORDER.md`](../engineering/INSTALL_ORDER.md)** |
| **Security / GRC** reviewer | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/trust-center.md`](../trust-center.md)** (trust posture table) · **[`docs/ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md)** |
| **Executive sponsor / buyer** | **[`docs/CONTRIBUTOR_ON_ONE_PAGE.md`](../CONTRIBUTOR_ON_ONE_PAGE.md)** — then **[`docs/CORE_PILOT.md`](../CORE_PILOT.md)** — canonical outward narrative remains **`docs/EXECUTIVE_SPONSOR_BRIEF.md`** (open in repo; not a spine table link). |

**Hub:** **[`docs/START_HERE.md`](../START_HERE.md)**.
