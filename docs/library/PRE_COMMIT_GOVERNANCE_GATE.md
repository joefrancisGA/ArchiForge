> **Scope:** Pre-commit governance gate (optional) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Pre-commit governance gate (optional)

## Objective

Give governance teams a **preventive** control: block **`POST /v1/architecture/run/{runId}/commit`** when findings at or above a configurable severity threshold exist and an assigned policy pack **enforces** the gate.

## Configuration

| Key | Default | Effect |
|-----|---------|--------|
| **`ArchLucid:Governance:PreCommitGateEnabled`** | **false** | When **false**, the gate is **not evaluated** (no findings or assignment load on the commit path beyond existing behavior). |
| **`ArchLucid:Governance:WarnOnlySeverities`** | **null** | Array of severity names (e.g. `["Warning", "Error"]`) where the gate **warns** but does **not block**. Findings are reported via `GovernancePreCommitWarned` audit event and commit proceeds. |
| **`ArchLucid:Governance:ApprovalSlaHours`** | **null** | Hours allowed before an approval request is considered SLA-breached. When set, `SlaDeadlineUtc` is populated on new approval requests. |
| **`ArchLucid:Governance:ApprovalSlaEscalationWebhookUrl`** | **null** | Webhook URL for SLA breach notifications. HMAC-signed if `EscalationWebhookSecret` is set. |
| **`ArchLucid:Governance:EscalationWebhookSecret`** | **null** | HMAC-SHA256 secret for signing SLA breach webhook payloads (`X-ArchLucid-Signature` header). |

## Policy assignment

SQL migration **`054_PolicyPackAssignments_BlockCommitOnCritical.sql`** adds **`BlockCommitOnCritical`** (**bit**, default **0**) to **`dbo.PolicyPackAssignments`**.

SQL migration **`057_PolicyPackAssignments_BlockCommitMinimumSeverity.sql`** adds **`BlockCommitMinimumSeverity`** (**int**, nullable) to **`dbo.PolicyPackAssignments`**.

- When **`IsEnabled`** and either **`BlockCommitOnCritical`** or **`BlockCommitMinimumSeverity`** is set for an assignment in scope, the gate **may** block commit.
- Assignments are evaluated using the same hierarchical listing as other governance features (**`IPolicyPackAssignmentRepository.ListByScopeAsync`**).

### Severity threshold logic

| `BlockCommitMinimumSeverity` | `BlockCommitOnCritical` | Behavior |
|------------------------------|------------------------|----------|
| **null** | **true** | Block on **Critical** only (legacy behavior) |
| **2** (Error) | any | Block on **Error** and above (Error=2, Critical=3) |
| **1** (Warning) | any | Block on **Warning** and above |
| **0** (Info) | any | Block on all findings |
| **null** | **false** | No enforcement — assignment is not considered enforcing |

`FindingSeverity` enum: `Info=0`, `Warning=1`, `Error=2`, `Critical=3`.

## Enforcement logic

1. Resolve the **run** and its **`FindingsSnapshotId`**.
2. Load **`FindingsSnapshot`**; determine the effective minimum severity from the enforcing assignment.
3. Collect findings where `(int)Severity >= effectiveMinimumSeverity`.
4. If **no** enforcing assignment or **no** matching findings → commit proceeds.
5. If the effective severity label is in **`WarnOnlySeverities`** → durable audit **`GovernancePreCommitWarned`**, commit proceeds with warnings.
6. Otherwise → **`409`** **`#governance-pre-commit-blocked`** and durable audit **`GovernancePreCommitBlocked`** (see **`docs/AUDIT_COVERAGE_MATRIX.md`**).

## Warning-only mode

When a severity is listed in `WarnOnlySeverities`, findings at that threshold still appear in the gate result but the commit is **not blocked**. The orchestrator emits a `GovernancePreCommitWarned` audit event and logs a warning. This lets teams phase in enforcement by first observing what would be blocked.

## Approval SLA

When **`ApprovalSlaHours`** is configured, newly submitted governance approval requests receive a **`SlaDeadlineUtc`** set to `RequestedUtc + SlaHours`. The **`ApprovalSlaMonitor`** periodically checks for pending requests that have passed their deadline:

1. Emits **`GovernanceApprovalSlaBreached`** durable audit event.
2. Sends an HMAC-signed webhook to **`ApprovalSlaEscalationWebhookUrl`** (if configured).
3. Patches **`SlaBreachNotifiedUtc`** to prevent repeat notifications.

SQL migration **`058_GovernanceApprovalRequests_Sla.sql`** adds **`SlaDeadlineUtc`** and **`SlaBreachNotifiedUtc`** columns to **`dbo.GovernanceApprovalRequests`**.

## API surface

Problem details **`type`**: **`https://archlucid.example.org/errors#governance-pre-commit-blocked`**. Extensions: **`blockingFindingIds`** (array of strings), optional **`policyPackId`**, optional **`minimumBlockingSeverity`**. **`correlationId`** follows normal **`ProblemCorrelation`** attachment.

## Trade-offs

| Benefit | Cost |
|---------|------|
| Stops non-compliant golden manifests before they land | May **block** teams until findings are resolved or policy is adjusted |
| Configurable severity threshold avoids over-blocking | Operators must understand severity semantics in findings |
| Warning-only mode enables phased rollout | Warnings may be ignored if not monitored |
| Approval SLA with webhook escalation | Requires external webhook receiver for escalation routing |
| Clear audit trail for blocks, warns, and SLA breaches | Additional audit volume |

## Related

- **`docs/API_CONTRACTS.md`** — commit conflict vs governance block.
- **`docs/V1_SCOPE.md`** §2.10 — optional feature flagging.
- **`docs/AUDIT_COVERAGE_MATRIX.md`** — `GovernancePreCommitBlocked`, `GovernancePreCommitWarned`, `GovernanceApprovalSlaBreached`.
