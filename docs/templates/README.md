> **Scope:** Ready-to-submit `ArchitectureRequest` JSON examples for pilots — not the live API contract alone; see linked contract and ingestion docs.

# Architecture request templates

These files are **minimal valid** bodies for `POST /v1/architecture/request` (OpenAPI: `ArchitectureRequest`). Replace `requestId` if you need a stable idempotency key.

## Files

| File | Summary |
|------|---------|
| [microservices-ecommerce.json](./microservices-ecommerce.json) | Container Apps–based e‑commerce platform (API gateway, catalog, orders, payments, notifications). |
| [event-driven-iot.json](./event-driven-iot.json) | IoT telemetry ingestion, stream processing, tiered storage, and observability on Azure. |
| [regulated-healthcare-api.json](./regulated-healthcare-api.json) | HIPAA-minded patient API with identity, auditing, encryption, and private networking posture. |

## Submit with curl

Set `BASE` and `KEY` (omit `-H` Authorization if using DevelopmentBypass locally).

```bash
curl -sS -X POST "$BASE/v1/architecture/request" \
  -H "Authorization: Bearer $KEY" \
  -H "Content-Type: application/json" \
  --data-binary @"$(dirname "$0")/microservices-ecommerce.json"
```

On Windows PowerShell (repo root):

```powershell
$h = @{ Authorization = "Bearer $env:ARCHLUCID_API_KEY"; "Content-Type" = "application/json" }
Invoke-RestMethod -Method Post -Uri "$env:BASE/v1/architecture/request" -Headers $h -InFile "docs/templates/microservices-ecommerce.json"
```

## CLI (`archlucid`)

Use your project’s **`archlucid`** command to create a run from JSON if your build wires `new` / `run` to a file path (see [CLI_USAGE.md](../library/CLI_USAGE.md)); otherwise prefer **curl** against the API.

## References

- Request shape: `ArchLucid.Contracts.Requests.ArchitectureRequest`
- Prefix conventions for **documents** (`REQ:`, `POL:`, …): [CONTEXT_INGESTION.md](../library/CONTEXT_INGESTION.md)
- API behavior: [API_CONTRACTS.md](../library/API_CONTRACTS.md)
