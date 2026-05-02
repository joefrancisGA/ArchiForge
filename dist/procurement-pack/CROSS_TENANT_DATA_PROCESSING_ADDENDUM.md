> **Scope:** Cross-tenant optional processing addendum defining exact data classes, privacy floor controls, opt-in/withdrawal behavior, and audit evidence expectations for procurement and DPA alignment.

# Cross-Tenant Data Processing Addendum

**Audience:** Legal reviewers, procurement, and product/security teams documenting optional cross-tenant processing.

**Last reviewed:** 2026-05-01

---

## 1. Purpose

This addendum defines the operational controls for optional cross-tenant pattern processing referenced by `DPA_TEMPLATE.md` section 10.

The feature is optional, OFF by default, and separate from core tenant-private processing.

---

## 2. Data included and excluded

### Included (when opt-in is enabled)

- Non-identifying structural architecture fingerprints.
- Coarse-grained aggregate counters used to generate generalized guidance.
- Event metadata required to enforce minimum cohort thresholds and audit setting changes.

### Explicitly excluded

- Free-text architecture descriptions.
- URLs, hostnames, and endpoint strings.
- User names, email addresses, and identity claims.
- Tenant names, workspace names, project names, and customer labels.
- Raw run artifacts and export document content.

---

## 3. Privacy floor and publication control

- Cross-tenant outputs are only published when at least **k >= 5** distinct contributing tenants are present in a bucket.
- If a bucket drops below threshold after withdrawal or data hygiene events, that bucket is removed from publishable output.
- Threshold is enforced before output rendering, not after rendering.

---

## 4. Opt-in and withdrawal flow

- **Default:** OFF for all tenants.
- **Enablement:** Explicit tenant admin action in product controls plus contractual acknowledgment where required.
- **Withdrawal:** Tenant admin can disable at any time.
- **Propagation target:** Tenant contributions are removed from publishable aggregates within **24 hours**, excluding backup and rebuild windows documented in DPA and backup policy.

---

## 5. Audit evidence and controls

The system should emit auditable records for:

- Feature opt-in enabled.
- Feature opt-in disabled.
- Privacy-floor enforcement decision for each publishable bucket class.

These records support procurement and compliance evidence requests and should map to typed audit events in the standard audit pipeline.

---

## 6. Contract alignment notes

- This addendum is operational guidance and does not replace legal review.
- DPA language should reference this addendum for data classes and control behavior, while legal counsel finalizes jurisdiction-specific terms.

