# API fuzz testing (Schemathesis)

This document describes **Schemathesis**-based property testing of the ArchLucid HTTP API against the **OpenAPI** document. It complements contract snapshots ([OPENAPI_CONTRACT_DRIFT.md](OPENAPI_CONTRACT_DRIFT.md)), PR CI, and the weekly **ZAP** baseline ([security/ZAP_BASELINE_RULES.md](security/ZAP_BASELINE_RULES.md)).

**Workflow:** [`.github/workflows/schemathesis-scheduled.yml`](../.github/workflows/schemathesis-scheduled.yml)  
**Upstream:** [Schemathesis](https://github.com/schemathesis/schemathesis) (official Docker image `schemathesis/schemathesis:stable`)

---

## Purpose

Schemathesis performs **property-based API fuzzing** driven by the **OpenAPI specification**. It generates large numbers of requests—both schema-valid and deliberately invalid—to surface:

- Crashes and unhandled exceptions
- Unexpected **5xx** responses on inputs the spec allows (or that edge the spec)
- **Response bodies** that do not match the documented schema
- **Content-Type** mismatches vs. documented responses
- **Latency** regressions relative to configured thresholds

Unlike a fixed integration test suite, Schemathesis **explores** the combination space implied by the spec (plus configured phases), so it often finds issues that hand-written tests miss.

---

## When it runs

| Trigger | Schedule / graph |
|---------|------------------|
| **Pull request** | **Schemathesis light** — [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) job **`api-schemathesis-light`** after **`dotnet-full-regression`**: **examples-only** phase, tight timeouts (~1–2 minutes including image build) |
| **Cron** | Weekly **Monday 06:00 UTC** (`0 6 * * 1`) — full phases in [`.github/workflows/schemathesis-scheduled.yml`](../.github/workflows/schemathesis-scheduled.yml) |
| **Manual** | **Actions → Security: Schemathesis API fuzz (scheduled) → Run workflow** |

The **scheduled** job is **not** fully mirrored on every PR (to keep PR latency bounded). The **light** PR job uses **`--phases=examples`** only (Schemathesis **v4+** valid phases are **`examples`**, **`coverage`**, **`fuzzing`**, **`stateful`** — the old **`explicit`** phase name is invalid and will fail the job). The weekly run adds **fuzzing** and **stateful** exploration. Typical wall-clock time for the scheduled workflow is on the order of **10–20 minutes** (API image build, container start, fuzz run, artifact upload), depending on runner load and API complexity.

---

## What it checks

The scheduled workflow runs Schemathesis **v4** with:

- **`--checks=all`** — all built-in checks (status codes where applicable, content types, schema conformance, response-time bounds, etc., per Schemathesis version).
- **`--phases=examples,fuzzing,stateful`** — schema examples, generated fuzz cases, and **stateful** scenarios that follow **OpenAPI links** between operations (workflows such as “create → read” when the document defines links).

Timeouts are set in **seconds** (CLI v4): `--request-timeout 10`, `--max-response-time 30`.

The API is started in **`ASPNETCORE_ENVIRONMENT=Development`** with **`ASPNETCORE_URLS=http://+:8080`**, matching the **ZAP** scheduled job pattern so anonymous access to docs and health aligns with local Development behavior. The schema URL is **`/openapi/v1.json`** (Microsoft `MapOpenApi` document).

---

## How to run locally

You need **Docker**. Two practical patterns:

### A — CI-parity (recommended to reproduce the workflow)

From the **repository root** (same image and flags as CI):

```bash
docker build -f ArchLucid.Api/Dockerfile -t archlucid-api:fuzz .
export FUZZNET=archlucid-fuzz-local
docker network create "$FUZZNET"
docker run -d --name archlucid-fuzz-api --network "$FUZZNET" -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  archlucid-api:fuzz

# Wait until the API responds (same probe as CI)
until curl -fsS http://127.0.0.1:8080/health/live; do sleep 2; done

mkdir -p .schemathesis-out
chmod -R a+rwx .schemathesis-out

docker run --rm --network "$FUZZNET" \
  -v "$(pwd)/.schemathesis-out:/out" \
  schemathesis/schemathesis:stable \
  run http://archlucid-fuzz-api:8080/openapi/v1.json \
  --url http://archlucid-fuzz-api:8080 \
  --checks=all \
  --phases=examples,fuzzing,stateful \
  --report=junit \
  --report-dir=/out \
  --report-junit-path=/out/schemathesis-results.xml \
  --request-timeout 10 \
  --max-response-time 30

docker rm -f archlucid-fuzz-api
docker network rm "$FUZZNET"
```

JUnit output: **`.schemathesis-out/schemathesis-results.xml`**.

**Note:** The published API image expects a real environment (e.g. SQL) for full readiness; **`/health/live`** only confirms the process is up. If your image fails to start without dependencies, use pattern **B** or point the API container at Compose-backed SQL (see [BUILD.md](BUILD.md) / [docker-compose.yml](../docker-compose.yml)).

### B — Against `docker compose` full-stack API

If the API is already running via **`docker compose --profile full-stack`** (service **`api`**, container name **`archlucid-api`**, host port **5000** maps to container **8080**):

1. Ensure the API is healthy (`/health/live` on **http://localhost:5000** from the host).
2. Run Schemathesis on the **same Docker network** as the API so you can use **`http://api:8080`** inside the fuzz container:

```bash
# Resolve one Docker network attached to the API container (first listed)
NETWORK=$(docker inspect archlucid-api -f '{{range $k, $_ := .NetworkSettings.Networks}}{{$k}}{{"\n"}}{{end}}' | head -n1)
mkdir -p .schemathesis-out && chmod -R a+rwx .schemathesis-out

docker run --rm --network "$NETWORK" \
  -v "$(pwd)/.schemathesis-out:/out" \
  schemathesis/schemathesis:stable \
  run http://api:8080/openapi/v1.json \
  --url http://api:8080 \
  --checks=all \
  --phases=examples,fuzzing,stateful \
  --report=junit \
  --report-dir=/out \
  --report-junit-path=/out/schemathesis-results.xml \
  --request-timeout 10 \
  --max-response-time 30
```

On **Docker Desktop** (Mac/Windows), you can instead target the host-mapped port from a container using **`host.docker.internal`** (e.g. schema `http://host.docker.internal:5000/openapi/v1.json` and `--url http://host.docker.internal:5000`) if you prefer not to attach to the Compose network.

---

## Interpreting results

### JUnit XML artifact

Each run uploads **`schemathesis-results.xml`** as a workflow artifact (name like `schemathesis-junit-<run_id>`). Download it from the **Actions** run page. Many IDEs and CI systems can display JUnit; you can also open the XML and search for `<failure` / `<error` elements.

### Common failure categories

| Symptom | Likely cause | Triage hint |
|--------|----------------|-------------|
| **Response does not match schema** | **Schema drift** (implementation changed without updating OpenAPI) or **real bug** (wrong DTO/status). | Compare failing operation with **`Contracts/openapi-v1.contract.snapshot.json`** and `OpenApiContractSnapshotTests`; regenerate snapshot only when the API change is intentional ([OPENAPI_CONTRACT_DRIFT.md](OPENAPI_CONTRACT_DRIFT.md)). |
| **Unexpected 5xx** | Unhandled edge case, null reference, downstream failure (e.g. DB). | Reproduce with the **curl** or case ID Schemathesis prints; check API logs and correlation IDs. |
| **Status code not allowed** | Spec documents 200 only but API returns 401/404 for fuzzed paths or auth. | Decide whether the **spec** should list realistic statuses or the **handler** should narrow behavior. |
| **Content-Type check failed** | Wrong `Content-Type` header vs. OpenAPI `content` map. | Fix producer metadata or document `application/json` vs. problem details consistently. |
| **Response time exceeded** | Slow path under fuzz load or cold start. | Confirm `max-response-time` is realistic; optimize hot path or exclude heavy operations via Schemathesis filters if justified. |
| **Stateful / link failures** | Workflow assumptions (e.g. missing link, wrong example IDs). | Inspect OpenAPI **links**; may be spec or ordering bug. |

### Schema drift vs. real bug

- **Schema drift:** Fuzzing is correct; the **document** or **snapshot** is stale. Update OpenAPI generation / attributes, then refresh **`openapi-v1.contract.snapshot.json`** with the documented env var and commit.
- **Real bug:** Implementation violates its **own** spec (or returns 500 on valid input). Fix the API and add a **targeted regression test** in **`ArchLucid.Api.Tests`** so the issue does not return.

---

## Alternatives considered

| Tool | Why not chosen as the primary scheduled fuzzer here |
|------|-----------------------------------------------------|
| **RESTler** | Strong for Microsoft stacks, but a heavier **compile → fuzz** pipeline and more moving parts than a single **Docker run** for contributors and CI. |
| **Dredd** | Good for contract verification against examples; **less flexible** on **generated** invalid/valid traffic and **stateful** exploration compared to Schemathesis + Hypothesis. |

Schemathesis remains **one layer** in depth-in-defense: unit/integration tests and **ZAP** cover other angles; see [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) and [TEST_STRUCTURE.md](TEST_STRUCTURE.md).
