> **Scope:** Governance workflow - full detail, tables, and links in the sections below.

# Governance workflow

ArchLucid governance covers **approval requests**, **manifest promotions** between deployment environments, and **environment activation** (which manifest version is live in a given environment). The primary HTTP API is under `POST /v1/governance/...` (`GovernanceController`). The durable audit path dual-writes `IAuditService` and baseline mutation logs from `GovernanceWorkflowService` (see `docs/AUDIT_COVERAGE_MATRIX.md`).

## Segregation of duties (approve / reject)

A reviewer **must not** approve or reject a governance approval request they **submitted**. `GovernanceApprovalRequest.RequestedBy` (set at submission) is compared to the review identity (`reviewedBy` on `ApproveAsync` / `RejectAsync`) using **ordinal, case-insensitive** matching.

- **Violation:** `GovernanceWorkflowService` emits a durable `IAuditService` event with type **`GovernanceSelfApprovalBlocked`** and `DataJson` `{ approvalRequestId, requestedBy, attemptedReviewerBy }`, then throws **`GovernanceSelfApprovalException`** (subclass of `InvalidOperationException`) with a message naming the actor and request id.
- **HTTP API:** `GovernanceController` maps that exception to **400 Bad Request** with RFC 9457 problem type **`ProblemTypes.GovernanceSelfApproval`** (`https://archlucid.example.org/errors#governance-self-approval`).
- **Promotion (`PromoteAsync`)** is unchanged; prod promotion continues to validate the approval chain separately.

## Dry-run mode (`?dryRun=true`)

Operators can validate whether a **submit approval** or **promotion** request would pass business rules **without** persisting data, writing audit rows, or publishing integration events.

### What is validated

- **Submit approval** (`POST /v1/governance/approval-requests?dryRun=true`): argument checks, run existence (`IRunDetailQueryService`), and construction of the same `GovernanceApprovalRequest` shape returned on success.
- **Promote** (`POST /v1/governance/promotions?dryRun=true`): argument checks, run existence, and the full **prod promotion guard** when the target environment is production (approved approval request id required; approval must match run id, manifest version, and target environment).

### What is skipped

- Repository writes (`CreateAsync` / `UpdateAsync` on approval or promotion repositories).
- `IBaselineMutationAuditService.RecordAsync` and `IAuditService.LogAsync`.
- Integration event publish / outbox enqueue for approval submission.

Dry-run does **not** apply to **approve**, **reject**, or **activate** in the current API (activation uses a transactional unit-of-work and is intentionally excluded).

### Detecting dry-run responses

- The query parameter `dryRun=true` is echoed by the client’s request URL.
- The API sets response header **`X-ArchLucid-DryRun: true`** when dry-run was used (see `ArchLucidHttpHeaders.DryRun`).
- The JSON body shape matches a successful non–dry-run response; status remains **200 OK** for both (use the header or query flag to distinguish intent).

### Preview vs dry-run

`GovernancePreviewService` / `GovernancePreviewController` answer **manifest diff** questions (what would change if a version were activated). Dry-run answers **workflow gate** questions (would submit/promote validation pass?) without side effects.
