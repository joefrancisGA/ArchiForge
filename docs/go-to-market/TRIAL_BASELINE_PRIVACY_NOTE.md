> **Scope:** How ArchLucid uses the optional trial-signup “baseline review-cycle hours” field — audience: prospects, security reviewers, and legal; not a substitute for your organization’s full privacy policy.

# Trial baseline review-cycle — privacy note

## What we collect

When you choose **“I will enter our median review-cycle hours”** on the self-serve signup form, we store:

- **BaselineReviewCycleHours** — a positive decimal you supply (median wall-clock hours for an architecture review cycle *before* ArchLucid).
- **BaselineReviewCycleSource** (optional) — a short free-text note you supply (for example “team estimate” or “last five reviews”).

When you stay on **“Use model default (modeled estimate)”**, we **do not** store tenant-specific hours at signup; the product uses the conservative default described in [`docs/PILOT_ROI_MODEL.md`](../PILOT_ROI_MODEL.md) for “before” comparisons until you commit runs.

## How we use it

- **Delta computation only** — we compare your supplied baseline (or the model default) to **measured** time-to-commit derived from your tenant’s committed runs (`PilotRunDeltaComputer` / pilot-run-deltas surface). This powers in-product copy such as the **Review-cycle delta (before vs measured)** panel and sponsor-facing value-report sections (`ValueReportReviewCycleSectionFormatter`).
- **Never published per-tenant in aggregate bulletins** — quarterly aggregate ROI bulletins (see [`AGGREGATE_ROI_BULLETIN_TEMPLATE.md`](AGGREGATE_ROI_BULLETIN_TEMPLATE.md)) intentionally **exclude per-tenant rows** and require a **minimum tenant count** plus **owner sign-off** before any external publication.

## What we do not do

- We do **not** sell this field as a standalone dataset.
- We do **not** use it for advertising retargeting.
- We do **not** bypass trial write gates or billing controls based solely on this field.

## Retention and access

Baseline fields live on the **tenant row** (`dbo.Tenants`) with the same access controls as other tenant metadata (RLS-scoped application paths; global admin surfaces are operator-only and audited). Exact retention aligns with your **tenant lifecycle** (see [`docs/runbooks/TRIAL_LIFECYCLE.md`](../runbooks/TRIAL_LIFECYCLE.md)).

## Related

- [`TRIAL_AND_SIGNUP.md`](TRIAL_AND_SIGNUP.md) — product design of the trial.
- [`docs/security/TRIAL_AUTH.md`](../security/TRIAL_AUTH.md) — who can read trial-scoped APIs after signup.
