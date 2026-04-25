> **Scope:** Platform and tenant operators configuring SCIM bearer tokens, IdP provisioning, and rotation; not a buyer-facing product overview (see `SCIM_PROVISIONING.md`) or legal terms.

# SCIM operator runbook

## Issue a tenant SCIM token

1. Sign in as **tenant admin** with permission to call **`POST /v1/admin/scim/tokens`**.
2. Call the endpoint; capture the **plaintext token** from the JSON response **once** — it is not shown again.
3. Store the secret in your **IdP vault** (Entra enterprise app credential, Okta API token secret, etc.).

## Rotate / coexistence

- Issuing a **new** token does **not** invalidate older tokens until you **revoke** them explicitly.
- Revoke with **`DELETE /v1/admin/scim/tokens/{id}`** (id from the list response).

## Entra ID (Microsoft) checklist

1. Create an **enterprise application** with **automatic provisioning** enabled.
2. Set the SCIM URL to `https://<host>/scim/v2` and paste the bearer token.
3. Map **user** attributes (`userName`, `emails`, `active`, …) per your directory schema.
4. Map **groups** if you rely on `archlucid:*` well-known group keys or overrides in `Scim:GroupRoleMappingOverrides`.

### TODO (owner-only): Microsoft Entra application gallery

Publishing a **gallery** listing is a Microsoft partner workflow outside repository automation. Track packaging, publisher verification, and support contacts separately from this runbook.

## Okta / OneLogin

Follow each vendor’s “SCIM bearer token” documentation; the ArchLucid surface is vendor-agnostic **RFC 7644** JSON over HTTPS.

## Seat troubleshooting

- If provisioning fails with **seat limit** errors, raise `EnterpriseSeatsLimit` (SQL) or deactivate stale users (`Active = false`) to free capacity.

## References

- Buyer-facing summary: [`SCIM_PROVISIONING.md`](SCIM_PROVISIONING.md)
- Threat model: [`../security/SCIM_THREAT_MODEL.md`](../security/SCIM_THREAT_MODEL.md)
