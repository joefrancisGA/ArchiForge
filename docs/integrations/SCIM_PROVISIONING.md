> **Scope:** Security, procurement, and IT stakeholders evaluating inbound SCIM automation and IdP integration; not operator runbooks, SQL DDL, or the full threat model (those live in linked docs).

# SCIM 2.0 inbound provisioning (buyer overview)

ArchLucid acts as a **SCIM 2.0 Service Provider** (RFC 7644). Your identity provider (Microsoft Entra ID, Okta, OneLogin, or any SCIM client speaking core User/Group semantics) can **provision, update, and deactivate** users mapped into ArchLucid **tenant-scoped** SCIM tables.

## What you configure in your IdP

| Setting | Value |
|--------|--------|
| **SCIM base URL** | `https://<your-host>/scim/v2` |
| **Authentication** | HTTP `Authorization: Bearer <token>` using the plaintext token issued from ArchLucid (see operator runbook). |
| **Users resource** | `/Users` |
| **Groups resource** | `/Groups` (membership drives **role hints** via configured group→role mapping). |

## Enterprise prerequisites

1. **Tenant tier** must support enterprise automation (per your order / contract).
2. **Seat limit** — `EnterpriseSeatsLimit` may be set on the tenant row; active SCIM users count toward `EnterpriseSeatsUsed`.
3. **Admin issues SCIM token** — `POST /v1/admin/scim/tokens` (interactive admin session, not SCIM bearer).

## Behaviour highlights

- **No anonymous SCIM** — unauthenticated calls receive **401**.
- **Filter support** — `eq`, `ne`, `co`, `sw`, `ew`, `gt`, `lt`, `ge`, `le`, `pr`, `and`, `or`, `not` over flat user attributes mapped to SQL (or in-memory evaluator in dev).
- **PATCH** — flat attribute paths only; complex attribute selectors return **400** with SCIM error type `invalidPath`.
- **Audit** — token mint/revoke and user/group mutations emit typed `AuditEventTypes.Scim*` rows to durable audit when configured.

## Further reading

- Operator procedures: [`SCIM_OPERATOR_RUNBOOK.md`](SCIM_OPERATOR_RUNBOOK.md)
- Threat model: [`../security/SCIM_THREAT_MODEL.md`](../security/SCIM_THREAT_MODEL.md)
- ADR: [`../adr/0032-scim-v2-service-provider.md`](../adr/0032-scim-v2-service-provider.md)
