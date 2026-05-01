> **Scope:** Operators and API integrators importing Terraform state via `terraform-show-json` on architecture requests — wire format, parser behavior, and limits; not host Terraform CLI installation or non-Azure provider guarantees beyond documented mappings.

# Terraform state (`terraform show -json`) as infrastructure context

**Purpose:** ingest **existing Terraform state** without running `terraform` on the ArchLucid host, by attaching a **full** `terraform show -json` document to an architecture request.

**Audience:** Operators automating onboarding from IaC repos and integrators aligning with **`POST /v1/architecture/request`**.

---

## Supported wire format

Set each `infrastructureDeclarations[]` row to:

| Field | Value |
|-------|--------|
| **`format`** | **`terraform-show-json`** (case-insensitive) |
| **`content`** | String body of **`terraform show -json`** (UTF-8) |

Public API validation: **`InfrastructureDeclarationRequestValidator`** in **`ArchLucid.Api`** permits **`terraform-show-json`** alongside **`json`** and **`simple-terraform`**.

Parsing implementation: **`ArchLucid.ContextIngestion.Infrastructure.TerraformShowJsonInfrastructureDeclarationParser`** walks **`values.root_module`** and **`child_modules`** into **`CanonicalObject`** rows (**`InfrastructureDeclaration`** source type).

---

## What becomes a canonical object

- Each **`resources[]`** element with **`type`**, **`name`**, optional **`provider_name`**, **`mode`**, and **`values`** contributes one object.
- **Azure / azurerm semantics:** **`azurerm_*`** tails map to **`TopologyResource`** unless classified as **`SecurityBaseline`** (e.g. Key Vault / NSG families) or **`PolicyControl`** (policy assignment/definition resources). See **`ResolveObjectTypeFromTerraformType`** in the parser source.
- **`depends_on`** (array of Terraform addresses) is captured as **`terraformDependsOn`** in **`Properties`** (pipe-separated references, capped for safety).
- **Sensitive values:** when Terraform emits a top-level **`sensitive_values`** map with **`true`** for an attribute mirrored in **`values`**, corresponding **`tf.*`** property values are replaced with **`[REDACTED]`** (nested structures: future work).

Large states: **`content`** is capped at **2 000 000** characters — prune or shard very large workspaces.

---

## Usage patterns

### A. Direct API (`terraform-show-json`)

1. Produce state JSON: **`terraform show -json > state.json`**
2. Embed in **`infrastructureDeclarations`** with **`format`** = **`terraform-show-json`** and **`content`** = file text.
3. Use the same **`POST /v1/architecture/request`** contract as narrative-only requests (**Execute**-class API key).

### B. Scripted conversion (`json` DTO interop)

The template recipe **`templates/integrations/architecture-import/`** documents flattening root resources into **`json`** **`ResourceDeclarationDocument`** shapes for callers that prefer a smaller DTO (**`Import-TerraformStateToRequest.ps1`**). Prefer **`terraform-show-json`** when you want **module recursion** and **depends_on** without an extra projection step.

---

## Limitations (V1)

- **/azurerm-first:** other providers still ingest as **`TopologyResource`** unless explicitly classified; broaden mappings deliberately as product needs them.
- **Edges in graph:** **`terraformDependsOn`** is stored as property metadata today; deterministic graph synthesis from Terraform alone is intentionally conservative — pair with **`topologyHints`** on the request when you need authoritative edges before graph build.
- **No Terraform CLI on host:** ingestion is **pure JSON**; CI and serverless callers only need filesystem access to **`state.json`** on their side.

---

## Operational notes

- **Security:** never log full state bodies; scrub secrets at source and rely on declarative **`sensitive_values`** hints where present.
- **Reliability:** malformed JSON yields **skipped** ingestion with warnings in application logs (**parser stays non-fatal** by design — tighten client validation if your pipeline requires strict fail-fast behavior).
