> **Scope:** Operator playbook for orphan-probe modes and Prometheus counters (short).

# Data consistency enforcement (operator snippet)

Use this card when Grafana fires **ArchLucidDataConsistencyOrphansDetected**, **ArchLucidDataConsistencyAlertsRaised**, or **ArchLucidDataConsistencyOrphansQuarantinedActivity** (`infra/prometheus/archlucid-alerts.yml`). **Dashboard:** import or sync **`infra/grafana/dashboard-archlucid-authority.json`** — data-consistency panel plots **`archlucid_data_consistency_orphans_detected_total`**, **`archlucid_data_consistency_alerts_total`**, and **`archlucid_data_consistency_orphans_quarantined_total`** (rates). Design detail lives in **[../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md](../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md)**.

## Modes (`DataConsistency:Enforcement:Mode`)

| Mode | Detection counter (`archlucid_data_consistency_orphans_detected_total`) | Alert counter (`archlucid_data_consistency_alerts_total`) | Golden-manifest quarantine insert |
|------|---------------------------------------------------------------------------|------------------------------------------------------------|----------------------------------|
| Off | Still increments **detection** when orphans exist (counts run before enforcement); **alerts/quarantine inserts** are skipped | No | No |
| Warn | Yes (warnings in logs when orphans &gt; 0) | No | Only if **`DataConsistency:Enforcement:AutoQuarantine`** is **true** |
| Alert | Yes | Yes, per table/column slice when count ≥ **`AlertThreshold`** | Only if **`AutoQuarantine`** is **true** |
| Quarantine | Yes | Same as Alert | Yes (bounded batch per **`MaxRowsPerBatch`**), plus optional **`AutoQuarantine`** semantics |

There is **no automated delete or corrective SQL** inside the orphan probe loop: **`Quarantine`** only **INSERT**s staging rows into **`dbo.DataConsistencyQuarantine`** so humans can reconcile. Comparisons/other slices still follow **[COMPARISON_RECORD_ORPHAN_REMEDIATION.md](./COMPARISON_RECORD_ORPHAN_REMEDIATION.md)** (manual dry-run, then remediation).

## Triage checklist

1. Filter detection counter by **`table`** / **`column`** — identify which FK slice drifted versus **`dbo.Runs`**.
2. If **`archlucid_data_consistency_alerts_total`** spikes, confirm **`Mode`** is **Alert** or **Quarantine** and compare counts to **`AlertThreshold`** on the emitting host/container app revision.
3. If quarantine increments appear, inspect **`dbo.DataConsistencyQuarantine`** newest rows (**`ReasonJson`**, **`TenantId`**) and route to remediation runbooks linked above rather than deleting source rows blindly.
