> **Scope:** Repository hygiene (clone and release surfaces) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Repository hygiene (clone and release surfaces)

**Audience:** Contributors, release engineers, and pilots unpacking the repo who need to know **what is shipped**, **what is generated locally**, and **what should never be committed**.

---

## What belongs in version control

| Area | Notes |
|------|--------|
| **Source** | `.cs`, `.ts`, `.tsx`, Terraform, SQL **migrations** (`ArchLucid.Persistence/Migrations/`), docs under `docs/`. |
| **OpenAPI / contract snapshots** | Checked-in snapshots used by CI and client generation (see [BUILD.md](BUILD.md)). |
| **`ArchLucid.Api.Client/Generated/`** | **`ArchLucidApiClient.g.cs`** is **committed** on purpose. Regenerate with NSwag when the API contract changes; review the diff like any other source file. |
| **Historical design logs** | `docs/archive/` is intentional history — not “noise” to delete for V1 polish. |

---

## Typical local-only output (do not commit)

| Path / pattern | Origin | Safe to delete locally |
|----------------|--------|-------------------------|
| **`artifacts/`** | `package-release.ps1`, local publishes | Yes — recreated by scripts ([RELEASE_LOCAL.md](RELEASE_LOCAL.md)). |
| **`[Bb]in/`, `[Oo]bj/`** | `dotnet build` / `dotnet test` | Yes. |
| **`TestResults/`**, coverage files | `dotnet test`, coverlet | Yes (already ignored via standard VS `.gitignore` patterns). |
| **`archlucid-ui/.next/`**, **`node_modules/`** | `npm run build` / `npm ci` | Yes. |
| **`_docker_*` dirs (repo root)** | Publish/config parity snapshots used in Docker troubleshooting docs and local experiments | May be **tracked** in this repository; do not delete from git without maintainer agreement. You may ignore them for day-to-day API/UI work. |

If you see **root-level** files such as `testresults.txt`, `commit2-log.txt`, or similar, they are **local command transcripts**, not release artifacts — keep them out of git (see `.gitignore` at repo root).

---

## Release-facing docs map

| Need | Doc |
|------|-----|
| V1 boundary and gates | [V1_SCOPE.md](V1_SCOPE.md), [V1_RELEASE_CHECKLIST.md](V1_RELEASE_CHECKLIST.md) |
| Pilot narrative | [PILOT_GUIDE.md](PILOT_GUIDE.md) |
| Package an RC / `artifacts/release/` | [RELEASE_LOCAL.md](RELEASE_LOCAL.md) |
| Change summaries | [CHANGELOG.md](../CHANGELOG.md) |
| Operational breaking changes | [../BREAKING_CHANGES.md](../../BREAKING_CHANGES.md) |

---

## Keeping the tree clean

1. Run **`run-readiness-check`** (or CI-equivalent) before pushing when you touch API, CLI, or UI.  
2. After **`package-release`**, hand off **`artifacts/release/`** per [RELEASE_LOCAL.md](RELEASE_LOCAL.md); do not commit that folder.  
3. Regenerate **`ArchLucid.Api.Client`** when OpenAPI changes and **commit** the updated `Generated` file so consumers stay in sync.

For first-time orientation, start at [START_HERE.md](../START_HERE.md) and the root [README.md](../../README.md).
