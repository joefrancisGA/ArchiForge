> **Scope:** First-time **ArchLucid contributor / internal engineer** running the full stack on a laptop. Goal: from `git clone` to "I committed a manifest and saw a finding" in ~30 minutes, using only Docker (no .NET SDK, no Node, no cloud keys).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Audience banner — read first.** ArchLucid is a **SaaS** product. **Customers, evaluators, and sponsors never run Docker, SQL, .NET, Node, or any local CLI.** They sign up at **`archlucid.com`** and use the in-product operator UI. This document is the **contributor / internal-engineer** first-run path — it is **not** the customer first-run path. If you arrived here as a buyer or evaluator, start at **[`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md)** then **[`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md)**, and request a guided trial. See **[`START_HERE.md`](../START_HERE.md)** "Audience split" and **[`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](../QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md)** §0.1.

# First 30 minutes — ArchLucid (contributor / internal engineer)

This is the **single canonical first-run path for ArchLucid contributors and internal engineers**. If a different document tells you to install .NET or Node *before* you have something working, prefer this one — those tools are useful later, not first.

You will:

1. Start the full stack in Docker (Contoso demo seed, simulator agents — no AI keys needed).
2. Open the operator UI and create a run from the sample preset.
3. Watch the run execute, commit a versioned manifest, and open at least one finding.
4. Tear it down cleanly.

If you get stuck, jump to the **[Troubleshooting](#troubleshooting)** section at the end.

> **Skip ahead with one command (.NET 10 SDK required):** if you have the .NET 10 SDK installed locally — or you opened the repo in the bundled **`.devcontainer/`** — run **`dotnet run --project ArchLucid.Cli -- try`** from the repo root. It does **steps 2 → 8 below** in a single command (Docker stack up, demo seed, sample architecture request, poll until commit, save the sponsor first-value Markdown, print the operator-UI run URL). Use **`--no-open`** when running headless (e.g., in the devcontainer). See [`docs/CLI_USAGE.md#archlucid-try`](../library/CLI_USAGE.md#archlucid-try). Manual walkthrough below stays the source of truth — useful when something fails partway through.

> **Validating the public trial funnel against staging?** Use **`dotnet run --project ArchLucid.Cli -- trial smoke --org "Smoke-$(date +%s)" --email trial-smoke@example.invalid --baseline-hours 16`** (PowerShell: replace `$(date +%s)` with `(Get-Date -UFormat %s)`). It is a pure-HTTP loop — no Docker, no SQL on your laptop — that calls **`POST /v1/register` → `GET /v1/tenant/trial-status` → `GET /v1/pilots/runs/{trialWelcomeRunId}/pilot-run-deltas`** and prints **PASS / FAIL** per step with an audit-event hint on failure. See [`docs/CLI_USAGE.md#archlucid-trial-smoke`](../library/CLI_USAGE.md#archlucid-trial-smoke) and [`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`](../runbooks/TRIAL_FUNNEL_END_TO_END.md).

> **Operator funnel metrics (optional):** after you are signed into the operator shell, the home page shows **process-lifetime** counts from **`GET /v1/diagnostics/operator-task-success-rates`** (see [`docs/OBSERVABILITY.md`](../library/OBSERVABILITY.md) for `archlucid_operator_task_success_total`). These reset when the API host restarts — useful for demos, not a substitute for long-window analytics.

---

## Prerequisites (one check)

- **Docker Desktop** (Windows / macOS) or **Docker Engine** (Linux), running and reachable from your shell. That is the only requirement for this walkthrough.

```bash
docker info
```

If that prints engine info (no error), you are ready. If not, start Docker Desktop / the Docker daemon and try again.

> **Ports used:** `1433` (SQL), `3000` (UI), `5000` (API), `6379` (Redis), `10000-10002` (Azurite). If any of those are already bound on your machine, stop the conflicting process before continuing.

**Hosted SaaS operators:** after you move past Docker-only evaluation, set `ASPNETCORE_ENVIRONMENT=SaaS` (or include the file explicitly) so `ArchLucid.Api/appsettings.SaaS.json` layers on top of the base JSON — opinionated defaults for **RLS fail-closed**, **prompt redaction**, and **no development auth bypass**. API keys stay **disabled** in the committed JSON until you set `Authentication:ApiKey:Enabled=true` **and** supply `AdminKey` / `ReadOnlyKey` via Key Vault or environment (startup validation rejects enabled keys without secrets). Terraform order: `infra/apply-saas.ps1` (plan by default) aligns with [`REFERENCE_SAAS_STACK_ORDER.md`](../library/REFERENCE_SAAS_STACK_ORDER.md).

---

## The 10 commands

> Run these from **a fresh terminal**. Lines beginning with `#` are comments — do not paste them.

### 1. Get the code

```bash
git clone https://github.com/joefrancisGA/ArchLucid.git
cd ArchLucid
```

> *What to expect:* the repository clones into `./ArchLucid/`. You are now at the repo root for the rest of this walkthrough.

![Cloned repo screenshot — placeholder](placeholder-01-cloned.png)

### 2. Start the demo stack

**Windows (PowerShell):**

```powershell
.\scripts\demo-start.ps1
```

**macOS / Linux (bash):**

```bash
./scripts/demo-start.sh
```

**Optional (same stack, from a .NET SDK checkout):** if you already cloned the repo and have the **.NET 10 SDK** installed, you can run **`dotnet run --project ArchLucid.Cli -- pilot up`** from the repo root instead of the scripts above. It runs the same **`docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack up -d --build`** command and polls **`http://127.0.0.1:5000/health/ready`** for up to **120 seconds**.

> *What to expect:* Docker pulls/builds five containers (SQL Server, Azurite, Redis, API, UI) and waits up to **120 seconds** for `GET http://localhost:5000/health/ready` to return **200**. On success the script prints `API is ready.` and tries to open the operator UI at `http://localhost:3000/runs/new` in your default browser. **Simulator agents** are enabled — no Azure OpenAI key required.

![Demo stack starting — placeholder](placeholder-02-demo-up.png)

### 3. Sanity-check the API

```bash
curl -s http://localhost:5000/health/ready
```

> *What to expect:* JSON like `{"status":"Healthy", ...}` with per-check entries for SQL, schema, compliance rule pack, and temp dir. If you see `Unhealthy`, see [Troubleshooting](#troubleshooting).

### 4. Confirm the build identity

```bash
curl -s http://localhost:5000/version
```

> *What to expect:* JSON with `informationalVersion`, `commitSha`, `runtimeFramework`, and `environment`. This is what you cite to support if you file an issue.

### 5. Open the operator UI

If your browser did not auto-open:

```text
http://localhost:3000/runs/new
```

> *What to expect:* the **New run** wizard, with three product layers visible in the sidebar (Core Pilot is the default). The sample-run preset is already loaded — you do not have to invent a brief.

![New-run wizard — placeholder](placeholder-03-new-run.png)

### 6. Submit the sample run

In the **New run** wizard, **leave every field at its default** and click **Submit**. (The default brief is the deterministic `PilotService` sample — it always succeeds against simulator agents.)

> *What to expect:* the UI navigates to the run detail page. The run starts in `Created` and quickly moves through `WaitingForResults`. You will see topology, cost, and compliance agent rows appear.

### 7. Watch the run execute (CLI mirror)

In a second terminal:

```bash
curl -s http://localhost:5000/v1/architecture/run/$(\
  curl -s "http://localhost:5000/v1/architecture/runs?pageSize=1" | \
  python -c "import sys,json;print(json.load(sys.stdin)['items'][0]['runId'])")
```

(One line is fine if you copy it as-is. Pure-shell PowerShell variant: open the run in the UI and copy the **Run ID** from the URL, then `curl -s http://localhost:5000/v1/architecture/run/<RUN_ID>`.)

> *What to expect:* the JSON response shows `status`, the agent task list, and any submitted results. The status will progress to `ReadyForCommit` once all simulator agents have reported.

### 8. Commit the manifest

In the UI, on the run detail page, click **Commit**. (Or via curl, replacing `<RUN_ID>`):

```bash
curl -s -X POST http://localhost:5000/v1/architecture/run/<RUN_ID>/commit
```

> *What to expect:* a JSON response with a `manifestVersion` (e.g. `1`). The UI now shows a **Manifest** tab populated with the merged decisions.

![Manifest committed — placeholder](placeholder-04-manifest-committed.png)

### 9. Open a finding

In the UI sidebar, open **Findings** (under the run). Click any row.

> *What to expect:* a **Finding detail** panel with category, severity, evidence, and the agent that produced it. **You have now exercised the full Core Pilot loop**: request → execute → commit → reviewable artifact.

![Finding detail — placeholder](placeholder-05-finding.png)

### 10. Tear down

When you are done, stop and remove the containers:

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack down -v
```

> *What to expect:* containers stop, volumes are removed (note the `-v`), and your machine is back to its pre-demo state. **Drop `-v`** if you want to keep the SQL data for next time.

---

## What you just proved

| Capability | Where you saw it |
|---|---|
| API health and build provenance | Steps 3 + 4 |
| Run lifecycle (create → execute → commit) | Steps 6 → 8 |
| Versioned, reviewable manifest | Step 8 |
| Typed findings with evidence | Step 9 |
| Operator UI (Core Pilot layer) | Steps 5 → 9 |

This is the **Core Pilot** path. **Advanced Analysis** (compare runs, replay, graph) and **Enterprise Controls** (governance approvals, policy packs, alerts) are progressive disclosures from the same UI — see **[docs/OPERATOR_DECISION_GUIDE.md](../library/OPERATOR_DECISION_GUIDE.md)** for when to reach for them.

---

## Where to go next (by persona)

| You are a... | Read next |
|---|---|
| **Operator** running a real pilot | [docs/OPERATOR_QUICKSTART.md](../library/OPERATOR_QUICKSTART.md) (commands), [docs/CORE_PILOT.md](../CORE_PILOT.md) (walkthrough), [docs/PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md) (how to measure success) |
| **Developer** about to commit code | [docs/onboarding/day-one-developer.md](../onboarding/day-one-developer.md) |
| **SRE / platform** owner | [docs/onboarding/day-one-sre.md](../onboarding/day-one-sre.md) |
| **Security / GRC** reviewer | [docs/onboarding/day-one-security.md](../onboarding/day-one-security.md) |
| **Executive sponsor / buyer** | [docs/EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) |

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `docker info` errors out | Start Docker Desktop or the Docker daemon. On Windows ensure it has finished starting (the whale icon is solid white). |
| `demo-start.*` times out at 120 s | Run `docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack logs api` and look for the failing dependency. Most common cause: a port (1433 / 3000 / 5000) is already bound. |
| Browser does not open the UI | Open `http://localhost:3000/runs/new` manually. |
| `health/ready` returns `Unhealthy` for `sql` | Wait 30 seconds and retry — SQL Server takes a moment to apply DbUp migrations on first boot. |
| `commit` returns 422 | The run does not yet have one result per required agent. Wait for the simulator agents to finish (look at the run's **Tasks** tab) and retry. |
| You want to start over | Run the **Step 10** teardown command, then re-run **Step 2**. |

Deeper guides: **[docs/TROUBLESHOOTING.md](../TROUBLESHOOTING.md)** and **[docs/engineering/CONTAINERIZATION.md](CONTAINERIZATION.md)**.

---

## Why this document exists

A first-time operator should not have to choose between four onboarding paths and three commands before they have a working stack. **This document is the single front door.** Every other onboarding doc is intentionally **persona-specific** (developer / SRE / security / sponsor) — they assume you have already completed this 30-minute path and want to go deeper for your role.

Screenshot placeholders (`placeholder-*.png`) are intentional — they will be replaced by the next release-smoke run; tracking is in [`docs/PRODUCT_LEARNING.md`](../library/PRODUCT_LEARNING.md). Do not block on them; the commands themselves are authoritative.
