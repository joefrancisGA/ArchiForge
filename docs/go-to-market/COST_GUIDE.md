> **Scope:** Buyer-facing **cost-of-operations** framing for ArchLucid-hosted and self-hosted pilots — estimates, not contractual pricing; verify against your Azure subscription and AOAI deployment.

# Cost guide (ArchLucid operations)

**Audience:** finance + platform owners sizing **LLM token burn** and **Azure footprint** before a pilot.

---

## 1. What this document is

- **Operational** cost (Azure resources + LLM usage)—**not** ArchLucid **commercial** subscription pricing (see **[ORDER_FORM_TEMPLATE.md](./ORDER_FORM_TEMPLATE.md)** / sales).
- Mixes **measured instrumentation** (meter names in **[OBSERVABILITY.md](../library/OBSERVABILITY.md)**) with **illustrative arithmetic** using **public Azure OpenAI** list pricing—**recompute** before board approval.

---

## 2. Variable: LLM tokens per run

| Signal | Where it lives |
|--------|----------------|
| Calls per run | Histogram **`archlucid_llm_calls_per_run`** |
| Token counters | **`archlucid_llm_*`** family (see **`ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs`**) |
| Baseline script | **[PERFORMANCE_BASELINES.md](../library/PERFORMANCE_BASELINES.md)** (simulator only) + **`tests/load`** for API smoke |

**Worked example (illustrative)** — replace with **your** measured `input+output` totals / 1e6 × $/MTok from the **Azure OpenAI pricing page** for **your** SKU/region (fetch day-stamped PDF/portal numbers).

| Hypothesis | Value |
|------------|-------|
| Runs / month | **50** pilot runs |
| Avg **input** tokens / run | **18k** (guess—measure) |
| Avg **output** tokens / run | **6k** |
| Model list price (placeholder) | **$5 / 1M input tok**, **$15 / 1M output tok** |

Monthly LLM subtotal ≈ `50 * (18 * 5 / 1000 + 6 * 15 / 1000)` = `50 * (0.09 + 0.09)` ≈ **$9** (pure fiction until you plug **real** token meters).

**Cost levers**

- **Simulator** mode → **$0** AOAI (CI / dev).
- **Smaller / cheaper** model deployment for non-critical agents.
- **`IHotPathReadCache` + explanation cache** → fewer duplicate LLM completions (see **[OBSERVABILITY.md](../library/OBSERVABILITY.md)** explanation cache ratio).

---

## 3. Shared Azure fabric (order-of-magnitude)

Rough **US East** list-style **orders of magnitude**—**not** quotes:

| Tier | Includes (typical pilot) | Monthly **ballpark** |
|------|--------------------------|-----------------------|
| Minimal | Azure SQL **Basic/Standard-small**, single **Container App**, **Storage** | **$150–$450** |
| Production-shaped | SQL **S2+**, **Front Door + WAF**, **Key Vault**, HA worker | **$800–$2500+** |

**Validate** with **[Azure pricing calculator](https://azure.microsoft.com/pricing/calculator/)** + `infra/terraform-*` modules you enable.

---

## 4. Compared to **manual** architecture review hours

Use **[PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)** to translate **hours saved × blended rate** vs. **fully loaded** ArchLucid + Azure + change mgmt—never double-count the same hour in two line items.

---

## 5. Next steps

1. Export **Prometheus** totals for **`archlucid_llm_*`** after a representative week.
2. Re-run the appropriate **[k6](../library/LOAD_TEST_BASELINE.md)** profile for your rollout tier.
3. Drop results into **[PROOF_OF_VALUE_SNAPSHOT.md](../library/PROOF_OF_VALUE_SNAPSHOT.md)** binder for exec sign-off.
