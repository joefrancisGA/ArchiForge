> **Scope:** Azure Well-Architected–analogue policy pack starter — import body and curl for `POST …/policy-packs`; not a Microsoft-certified WAF assessment artifact.

# Policy pack templates (`docs/templates/policy-packs`)

These files support **`POST /v1/governance/policy-packs`** (Administrator role; **Standard** commercial tier).

## Azure Well-Architected analogue (starter)

| File | Role |
|------|------|
| [azure-well-architected-content.json](./azure-well-architected-content.json) | **`PolicyPackContentDocument`** only (what goes in **`initialContentJson`** if you assemble the request by hand). Uses existing **`saas-ctrl-00x`** keys from `templates/policy-packs/saas/`. |
| [create-azure-waf-policy-pack.request.json](./create-azure-waf-policy-pack.request.json) | Full **`CreatePolicyPackRequest`** body for **`curl`** / REST clients. |

**Important:** This mapping is **documentation and pilot convenience**—it is **not** an official Microsoft Azure Well-Architected assessment. Extend **`complianceRuleKeys`** with governance rules your organization authors or imports.

### Import with curl

Requires a Standard-tier tenant and bearer token with **Admin** capability.

```bash
curl -sS -X POST "$BASE/v1/governance/policy-packs" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  --data-binary @docs/templates/policy-packs/create-azure-waf-policy-pack.request.json
```

After create, **publish** version `1.0.0` and **assign** scope per [PRE_COMMIT_GOVERNANCE_GATE.md](../../library/PRE_COMMIT_GOVERNANCE_GATE.md) so the pre-commit gate can reference the pack.

### Why reuse `saas-ctrl-*` keys?

**`PolicyPackContentDocument`** selects rules by **`complianceRuleKeys`** that must exist in your environment’s compliance rule catalog. The bundled SaaS vertical pack ships with stable ids (`saas-ctrl-001` … `008`) and maps cleanly to reliability, security, operations, and monitoring themes—useful for demos before you author bespoke rules.

## References

- **`CreatePolicyPackRequest`:** `ArchLucid.Api.Controllers.Governance.CreatePolicyPackRequest`
- **Content shape:** `ArchLucid.Decisioning.Governance.PolicyPacks.PolicyPackContentDocument`
- **Pre-commit gate:** [PRE_COMMIT_GOVERNANCE_GATE.md](../../library/PRE_COMMIT_GOVERNANCE_GATE.md)
