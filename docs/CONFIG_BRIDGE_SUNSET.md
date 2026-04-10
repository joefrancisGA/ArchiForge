# Configuration bridge — removed (Phase 7)

## Status

**Phase 7 (2026-04-08):** Dual-read configuration bridges for legacy `ArchiForge*` product sections, `ArchiForgeAuth`, `ConnectionStrings:ArchiForge`, CLI/UI `ARCHIFORGE_*` environment variables, and OIDC `archiforge_oidc_*` session keys have been **removed** from application code.

## Operator impact

- **API / Worker:** Only **`ConnectionStrings:ArchLucid`**, **`ArchLucid:*`**, and **`ArchLucidAuth:*`** are honored. If legacy keys are still present in configuration (for example Key Vault secret names mapped to old keys), **`ArchLucidLegacyConfigurationWarnings`** logs a **single warning** at startup listing which legacy keys were detected; those values are **not** applied.
- **CLI:** Global tool command is **`archlucid`**; project manifest file is **`archlucid.json`** (with a one-time stderr notice if only the legacy manifest filename exists).
- **Operator UI:** Only **`ARCHLUCID_*`** / **`NEXT_PUBLIC_ARCHLUCID_*`** are read; legacy **`ARCHIFORGE_*`** env vars trigger a **console warning** and are ignored.

## Historical context

Earlier phases documented merge behavior under `ArchLucidConfigurationBridge` / auth binding. That documentation applied before Phase 7; see `docs/ARCHLUCID_RENAME_CHECKLIST.md` and git history for the retired behavior.

## Security model

Removing silent fallbacks avoids the case where operators believe the system is using new-key configuration while old keys still drive behavior.

## References

- `docs/ARCHLUCID_RENAME_CHECKLIST.md` — Phase 7 items and deferred infrastructure renames (Terraform state mv, repo rename, Entra, workspace path).
- `docs/CONFIGURATION_KEY_VAULT.md` — secret and key naming for deployments.
