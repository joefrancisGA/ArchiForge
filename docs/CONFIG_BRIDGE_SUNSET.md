> **Scope:** Configuration bridge — removed (Phase 7) - full detail, tables, and links in the sections below.

# Configuration bridge — removed (Phase 7)

## Status

**Phase 7 (2026-04-08):** Dual-read configuration bridges that merged **historic** keys (see **`BREAKING_CHANGES.md`**) have been **removed** from application code. Only **`ArchLucid*`**, **`ArchLucidAuth*`**, **`ConnectionStrings:ArchLucid`**, **`ARCHLUCID_*`**, and **`NEXT_PUBLIC_ARCHLUCID_*`** are read; obsolete keys/env names surface **warnings** only.

## Sunset timeline

**Warnings-only period:** legacy **`ArchiForge*`** configuration keys (connection string name, product section, auth section) are detected at API/Worker startup and logged; values are **ignored**.

**Earliest hard enforcement:** not before **`2027-07-01`** (UTC calendar date). That date is also **`ArchLucidLegacyConfigurationWarnings.LegacyConfigurationKeysHardEnforcementNoEarlierThan`** in code so operators see the same target in logs. CI runs **`scripts/ci/assert_legacy_config_sunset_not_passed.py`** so merges fail if that constant is ever set to a **past** date without removing legacy handling. Turning warnings into **startup failure** requires an explicit product decision and checklist update — do not treat the date as automatic without release notes.

## Operator impact

- **API / Worker:** Only **`ConnectionStrings:ArchLucid`**, **`ArchLucid:*`**, and **`ArchLucidAuth:*`** are honored. If legacy keys are still present in configuration (for example Key Vault secret names mapped to old keys), **`ArchLucidLegacyConfigurationWarnings`** logs a **single warning** at startup listing which legacy keys were detected; those values are **not** applied.
- **CLI:** Global tool command is **`archlucid`**; project manifest file is **`archlucid.json`** (with a one-time stderr notice if only the legacy manifest filename exists).
- **Operator UI:** Only **`ARCHLUCID_*`** / **`NEXT_PUBLIC_ARCHLUCID_*`** are read; obsolete operator env names trigger a **console warning** and are ignored (see **`BREAKING_CHANGES.md`**).

## Historical context

Earlier phases documented merge behavior under `ArchLucidConfigurationBridge` / auth binding. That documentation applied before Phase 7; see `docs/ARCHLUCID_RENAME_CHECKLIST.md` and git history for the retired behavior.

## Security model

Removing silent fallbacks avoids the case where operators believe the system is using new-key configuration while old keys still drive behavior.

## References

- `docs/ARCHLUCID_RENAME_CHECKLIST.md` — Phase 7 items and deferred infrastructure renames (Terraform state mv, repo rename, Entra, workspace path).
- `docs/CONFIGURATION_KEY_VAULT.md` — secret and key naming for deployments.
