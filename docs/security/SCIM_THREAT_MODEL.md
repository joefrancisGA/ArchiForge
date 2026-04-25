> **Scope:** Security reviewers assessing inbound SCIM risks and mitigations for v1; not a complete enterprise risk register, pen-test report, or IdP-specific configuration guide.

# SCIM threat model (inbound, v1)

## Assets

- **SCIM bearer tokens** — long-lived automation credentials per tenant.
- **`dbo.ScimUsers` / `dbo.ScimGroups` / `dbo.ScimGroupMembers`** — provisioned identity projections.
- **Enterprise seat counters** on `dbo.Tenants`.

## Adversaries

| Actor | Goal |
|-------|------|
| **External anonymous** | Enumerate or mutate SCIM without credentials. |
| **Token thief** | Replay a leaked bearer token to create rogue admins or exhaust seats. |
| **Malicious insider (tenant admin)** | Mint many tokens, exfiltrate hashes from DB backups. |

## Controls

| Risk | Mitigation |
|------|------------|
| Anonymous access | All SCIM controllers require **`ScimBearer` + `ScimWrite`**; architecture tests forbid `[AllowAnonymous]` under `Controllers/Scim`. |
| Token disclosure | Plaintext shown **once** at issuance; DB stores **Argon2id** hash with tenant-bound salt; verification uses **constant-time** compare. |
| Cross-tenant spoofing | After authentication, **`IScopeContextProvider`** supplies `tenantId`; SCIM services never trust a tenant id from SCIM JSON paths. |
| Seat exhaustion | `TryIncrementEnterpriseScimSeatAsync` gates activation; operators set `EnterpriseSeatsLimit`. |
| Audit gaps | Mutations emit **`AuditEventTypes.Scim*`** constants; matrix rows track coverage. |

## Residual risks

- **Bearer replay** until revoke — standard bearer-token limitation; mitigate with vault hygiene + short operational lifetime where IdPs allow secret rotation schedules.
- **No mTLS for SCIM in v1** — transport security relies on **TLS 1.2+** at the edge (App Gateway / ingress). Private-link-only deployments should pin listeners accordingly.

## Out of scope (v1)

- Outbound SCIM (ArchLucid → IdP).
- SCIM Bulk operations.
