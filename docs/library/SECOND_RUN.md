> **Scope:** One-page “second run” inputs after `archlucid try` — no extra operator docs required.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Second run with your own data (`SECOND_RUN`)

**Audience:** Pilot operators who already ran **`archlucid try`** (or the operator UI demo) and want a **real** `POST /v1/architecture/request` from their own vocabulary in under five minutes.

**Goal:** One small file (TOML **or** JSON) → CLI creates the run → executes → polls → commits → prints the **first-value report URL**.

## 60-second path

1. Copy the template below into `SECOND_RUN.toml` next to your API (same machine that can reach `http://localhost:5000` or your pilot URL).
2. Replace **`name`** and **`description`** with your system (description must be at least **10** characters — API validation).
3. Optionally fill **`components`**, **`data_stores`**, **`public_endpoints`**, **`compliance_posture`** — each becomes structured hints on the architecture request.
4. Run:

```powershell
archlucid second-run SECOND_RUN.toml
```

5. On success you get:
   - `RunId` in the log line
   - **`First-value report URL`** — `GET /v1/pilots/runs/{runId}/first-value-report` (Markdown)
   - **`Operator UI`** link to `/runs/{runId}`
   - `first-value-{runId}.md` saved in the current directory (unless `--no-open` is used)

**Flags (same ergonomics as `try`):** `--api-base-url`, `--ui-base-url`, `--no-open`, `--commit-deadline <seconds>`.

## TOML template

```toml
# Required
name = "Contoso.Payments.Api"
description = "REST API for card capture and settlement; must stay PCI-scoped, single Azure region, private egress only."

# Optional lists (empty arrays are fine)
components = ["api", "worker", "admin-portal"]
data_stores = ["Azure SQL", "Redis cache"]
public_endpoints = ["https://payments.contoso.com"]
compliance_posture = ["PCI-DSS", "SOC2"]

# Optional scalar overrides
environment = "prod"
# cloud_provider is currently fixed to Azure in the product contract; omit or set "Azure"

# Optional free-text lists merged into the API request
constraints = ["single-region", "no public data plane"]
assumptions = ["Greenfield service — no legacy consumers"]

# Optional stable id (max 64 chars). Omit to let the CLI generate a new GUID (N format).
# request_id = "my-stable-second-run-001"

# Extra bullet requirements (in addition to auto lines for data_stores / public_endpoints)
inline_requirements = ["99.95% monthly availability target for the API tier"]
```

## JSON shape

Same keys in **snake_case** or **camelCase** (the CLI uses case-insensitive JSON). Example:

```json
{
  "name": "Contoso.Payments.Api",
  "description": "REST API for card capture and settlement; must stay PCI-scoped, single Azure region, private egress only.",
  "components": ["api", "worker"],
  "data_stores": ["Azure SQL"],
  "public_endpoints": ["https://payments.contoso.com"],
  "compliance_posture": ["PCI-DSS"]
}
```

## Limits

| Limit | Value |
|-------|------|
| File size (UTF-8) | **24 KiB** hard cap in the CLI parser |
| `request_id` | Max **64** characters (matches `ArchitectureRequest`) |
| `description` | Min **10** characters (matches API) |

## When something fails

The CLI prints **HTTP status**, **API detail**, and (when the host returns it) **`X-Correlation-ID`** so you can jump straight into log search. It also prints **audit event type strings** that appear in `dbo.AuditEvents` for the architecture run lifecycle — grep those alongside the correlation id.

## Auth

Same as other CLI commands: set **`ARCHLUCID_API_KEY`** when your host requires an API key (trial / automation). Development bypass hosts typically need no key.

## See also

- [docs/CORE_PILOT.md](../CORE_PILOT.md) — where this path sits after the first demo
- [docs/OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md) — copy-paste API equivalents
- [docs/API_CONTRACTS.md](API_CONTRACTS.md) — full `POST /v1/architecture/request` body (`ArchitectureRequest`)
