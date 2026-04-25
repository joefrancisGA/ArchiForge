# Import Existing Architecture into ArchLucid

**Pattern:** turn **infrastructure that already exists** (Terraform state, Azure Resource Manager (ARM) template, or a **CSV business brief**) into a **V1** `POST /v1/architecture/request` call. No new C# in your fork is required: the product already ingests `infrastructureDeclarations` on the run request; the **JSON** form maps to `ResourceDeclarationDocument` in the context-ingestion layer (see [CONTEXT_INGESTION.md](../../../docs/library/CONTEXT_INGESTION.md) § `json` / `simple-terraform` / `terraform-show-json`).

**Server-side reference (read-only in this recipe):**  
[`ArchLucid.ContextIngestion/Infrastructure/TerraformShowJsonInfrastructureDeclarationParser.cs`](../../../ArchLucid.ContextIngestion/Infrastructure/TerraformShowJsonInfrastructureDeclarationParser.cs) parses the **full** `terraform show -json` document when a declaration is marked `terraform-show-json` on the **host**. The **public** `POST` validator today accepts `json` and `simple-terraform` only ([`ArchLucid.Api/Validators/InfrastructureDeclarationRequestValidator.cs`](../../../ArchLucid.Api/Validators/InfrastructureDeclarationRequestValidator.cs)), so this recipe **converts** Terraform/ARM into the **`json`** public contract.

---

## Terraform (PowerShell)

**Script:** [`Import-TerraformStateToRequest.ps1`](./Import-TerraformStateToRequest.ps1) — runs `terraform show -json` in the current directory, projects **root** `values.root_module.resources` into `ResourceDeclarationItem` rows, serializes a **ResourceDeclarationDocument** JSON string, and builds the full `ArchitectureRequest` JSON. If **`ARCHLUCID_API_URL`** and **`ARCHLUCID_API_KEY`** are set, it **POSTs** the body.

**Prereqs:** Terraform on `PATH`, PowerShell 7+, a directory where `terraform init` and `terraform show` succeed.

**Example (PowerShell, same env vars as everywhere else in these templates):**

```powershell
cd C:\IaC\myStack
$env:ARCHLUCID_API_URL = "https://api.contoso.com"
$env:ARCHLUCID_API_KEY = "paste-key-from-vault"   # never print or log
./Import-TerraformStateToRequest.ps1 -SystemName "OrdersApi" -Description "Import current Terraform state for the Orders service boundary before refactor."
```

The script only walks **one** `root_module` in this template; nested `child_modules` in state are a straightforward extension: mirror the **recursive** visit in the parser class above (optional exercise for your fork).

---

## ARM template (PowerShell)

**Script:** [`Import-ArmTemplateToRequest.ps1`](./Import-ArmTemplateToRequest.ps1) — `Get-Content` a JSON file with a top-level `resources` array, maps `type` + `name` per resource into the same **ResourceDeclarationDocument** shape, then POSTs when the env vars are set.

**Example:**

```powershell
$env:ARCHLUCID_API_URL = "https://api.contoso.com"
$env:ARCHLUCID_API_KEY = "paste-key-from-vault"
./Import-ArmTemplateToRequest.ps1 -TemplatePath "C:\arm\export.json" -SystemName "SharedHub"
```

---

## CSV (no Terraform / no ARM)

**File:** [`brief-template.csv`](./brief-template.csv) — one line per system with **systemName**, **environment**, **description** (≥ 10 characters), **cloudProvider** (use **`Azure`** for the shipped V1 `CloudProvider` model), and optional **constraint** columns.

**Script:** [`Request-FromBriefCsv.ps1`](./Request-FromBriefCsv.ps1) — imports the first row, emits JSON, and POSTs when the env vars are set.

```powershell
$env:ARCHLUCID_API_URL = "https://api.contoso.com"
$env:ARCHLUCID_API_KEY = "paste-key-from-vault"
./Request-FromBriefCsv.ps1
```

No infrastructure declarations are attached; this is a **narrative-only** request suitable when governance starts from documents.

---

## Configuration (shared)

| Name | Required for POST | Description |
|------|-------------------|-------------|
| `ARCHLUCID_API_URL` | Yes (scripts that auto-post) | HTTPS base without trailing `/` |
| `ARCHLUCID_API_KEY` | Yes (scripts that auto-post) | `X-Api-Key` |
| (Terraform / ARM) | N/A on server | The body includes `infrastructureDeclarations[0].content` as a **string** containing the inner JSON (PowerShell enforces that) |

**Execute policy:** `POST /v1/architecture/request` requires **Execute**-class policy for the caller. Use a service account key created for **automation** with least scope.

---

## Verify it works

1. **Health (no key):**  
   `curl -sS -o /dev/null -w "%{http_code}\n" "$ARCHLUCID_API_URL/health"`
2. **V1 list run summaries (proves `X-Api-Key` and Read path):**  
   `curl -sS -H "X-Api-Key: $ARCHLUCID_API_KEY" "$ARCHLUCID_API_URL/v1/architecture/runs?limit=1" | head -c 200; echo`
3. **After a create:**  
   `curl -sS -H "X-Api-Key: $ARCHLUCID_API_KEY" "$ARCHLUCID_API_URL/v1/architecture/run/<runIdFromResponse>" | head -c 500; echo`

A **201** (or idempotent **200** with the right headers) on create and a **200** on `GET /v1/architecture/run/…` confirm the pipeline.

**Notes**

- The inner JSON in `infrastructureDeclarations[].content` must be **valid** `ResourceDeclarationDocument` JSON: top-level `resources` array, each with **type**, **name**, and optional `properties` map.  
- **Character limit:** the validator allows up to **2 000 000** characters per `content` field — large states may need **pruning** or top-N resources.

---

## Troubleshooting

| Symptom | What to check |
|--------|----------------|
| **400** on create | `description` length, `requestId` uniqueness, `cloudProvider` = `Azure` in V1 |
| **400** on `infrastructureDeclarations` | `format` must be `json` or `simple-terraform` on the public API; inner JSON must match `json` parser expectations |
| **403** on POST | **Execute** policy missing for this key (Reader-only keys can list runs but not create) |

**Correlation:** record `requestId` from the create response; it appears in `GET /v1/architecture/run/{runId}.run` as part of the run metadata. Match `runId` to your CI logs and Application Insights.

---

## How this maps to the Terraform parser in code

- **`terraform show -json` →** full state document.  
- **`terraform-show-json` (host)** — parser walks `values.root_module` (and child modules) into **CanonicalObject** graph (see the **C#** file).  
- **This recipe** flattens **root** **resources** into the **`json` DTO** the API accepts, so the same import works **today** on the V1 public surface. If a future product version **allows** the `terraform-show-json` string directly on the wire, you can drop the conversion step and set `content` to the file text from `terraform show -json` and `format` to `terraform-show-json` (watch release notes; do **not** depend on that here).
