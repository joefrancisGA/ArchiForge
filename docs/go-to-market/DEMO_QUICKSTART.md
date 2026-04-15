# ArchLucid demo quickstart (buyer-facing)

**Audience:** Evaluators and champions who want to see the product in minutes without installing the .NET SDK, SQL Server, or Node.js locally.

**Grounding:** Same demo data as [demo-quickstart.md](../demo-quickstart.md) (Contoso Retail) and [V1_SCOPE.md](../V1_SCOPE.md). The Docker path uses **Development** environment, **simulator** agent mode (no Azure OpenAI charges), and **startup demo seed** after DbUp.

---

## Prerequisites

- **Docker Desktop** (Windows or macOS) or **Docker Engine** (Linux)
- That is all — no .NET 10 SDK, no local SQL Server, no Node.js for running the stack

---

## Start the demo (one command)

From the repository root:

| Platform | Command |
|----------|---------|
| **Windows (PowerShell)** | `.\scripts\demo-start.ps1` |
| **macOS / Linux (Bash)** | `./scripts/demo-start.sh` (ensure executable: `chmod +x scripts/demo-start.sh`) |
| **Manual** | `docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack up -d --build` |

The script waits up to **120 seconds** for `http://localhost:5000/health/ready`, then opens the operator UI (where supported).

**Ports used:** 1433 (SQL), 3000 (UI), 5000 (API), 10000–10002 (Azurite), 6379 (Redis). Free these before starting if something else is bound.

---

## Your first five minutes

1. **Wizard** — Browser should open to `/runs/new`. Pick **Greenfield web app** (or another preset) and walk the seven steps; submit a new run if you want live pipeline tracking, or explore existing data from the seeded demo.
2. **Runs** — Open **Runs** and select a run to see status, findings, and manifest linkage. The seed creates baseline and hardened Contoso runs when startup seed completes ([demo-quickstart.md](../demo-quickstart.md) §3).
3. **Explainability** — Open a finding and review the structured explainability trace (what was examined, rules, decisions).
4. **Compare** — Use **Compare runs** with two runs (seeded IDs are documented in [demo-quickstart.md](../demo-quickstart.md)) to see structured deltas.
5. **Graph** — Open the **Graph** view for a run to see provenance-style exploration.
6. **Export** — From a run or export flow, generate Markdown/DOCX/ZIP as exposed in your build (consulting templates may require optional configuration).

Adjust the path if you prefer to start from the home dashboard at `http://localhost:3000/`.

---

## What you are seeing

- **AI Architecture Intelligence** — A multi-agent pipeline (topology, cost, compliance, critic) produces structured findings and a versioned golden manifest; in simulator mode, agents run without calling cloud LLMs.
- **Governance and audit** — Policy packs, optional pre-commit gates, and durable audit patterns match [POSITIONING.md](POSITIONING.md) and [PRODUCT_DATASHEET.md](PRODUCT_DATASHEET.md).
- **Explainability** — Findings carry traces suitable for review and audit narratives.

For full capability claims, use [V1_SCOPE.md](../V1_SCOPE.md) and the [Product datasheet](PRODUCT_DATASHEET.md).

---

## Cleanup

| Platform | Command |
|----------|---------|
| **Windows** | `.\scripts\demo-stop.ps1` |
| **macOS / Linux** | `./scripts/demo-stop.sh` |

This runs `docker compose ... down -v` and removes named volumes (including Azurite data). SQL data in the compose setup is also discarded with `-v` as defined by the stack.

---

## Troubleshooting

- **Timeout on health/ready** — Run `docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile full-stack logs api` and confirm SQL and Azurite are healthy.
- **Port conflicts** — Stop other services on 1433, 3000, or 5000, or adjust host port mappings in a **local** override (do not commit port changes unless your team standardizes them).

---

## Next steps

- **Production-style pilot:** [Pilot Guide](../PILOT_GUIDE.md)
- **Business case:** [ROI_MODEL.md](ROI_MODEL.md) and [PILOT_SUCCESS_SCORECARD.md](PILOT_SUCCESS_SCORECARD.md)
- **Developer / detailed demo seed:** [demo-quickstart.md](../demo-quickstart.md)

---

## Related documents

| Doc | Use |
|-----|-----|
| [CONTAINERIZATION.md](../CONTAINERIZATION.md) | All Docker workflows including demo overlay |
| [demo-quickstart.md](../demo-quickstart.md) | Technical demo seed and HTTP verification |
