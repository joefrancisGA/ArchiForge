# Security policy

ArchLucid takes security seriously. This document describes how to report vulnerabilities, what we consider in scope, and how we harden the product. Additional detail appears in the repository under `docs/security/` and in [README.md](README.md).

## Reporting vulnerabilities

**Please do not open public GitHub issues for security vulnerabilities.** Public disclosure can put users at risk before a fix is available.

Instead, email **security@archlucid.com** with:

- A clear description of the issue and its impact
- Steps to reproduce (or proof-of-concept), if possible
- Affected components or versions, if known

We will send an **initial acknowledgment within 48 hours** and aim to provide a **substantive update or resolution within 10 business days** for valid reports in scope. Complex issues may require more time; we will keep you informed.

**Accessibility barriers (non-security):** for WCAG / usability barriers in the product or public marketing pages, email **accessibility@archlucid.com**. This alias routes to the **same operational custodian** as **security@archlucid.com** (decision **2026-04-22** — see [`docs/security/ACCESSIBILITY_MAILBOX.md`](docs/security/ACCESSIBILITY_MAILBOX.md)); triage distinguishes accessibility follow-up from coordinated security disclosure.

## Scope

**In scope**

- ArchLucid **API** host, **Worker** host, **CLI**, and **operator UI** (`archlucid-ui`)
- **Terraform** modules and stacks in this repository
- **Dockerfiles** and published **container images** built from this repo
- **GitHub Actions** workflows defined in `.github/workflows/` (misconfigurations that affect supply chain or secrets)

**Out of scope**

- Vulnerabilities in **third-party dependencies** (report to the upstream maintainer; we track upgrades via NuGet/npm, SBOMs, and scanners)
- **Social engineering** or physical attacks
- **Denial-of-service** against non-production or best-effort demo environments
- Issues requiring **compromised operator credentials** or **misconfigured** Entra ID / Key Vault / network boundaries outside our default reference architecture

## Security posture (summary)

- **Authentication:** Entra ID (JWT Bearer) and optional **API key** mode; sensitive comparisons use fixed-time helpers (e.g. `CryptographicOperations.FixedTimeEquals`) where applicable.
- **Authorization:** **RBAC** (three app roles mapped to five authorization policies) and **SQL row-level security (RLS)** enforced for production SQL deployments — see [docs/security/MULTI_TENANT_RLS.md](docs/security/MULTI_TENANT_RLS.md).
- **LLM prompts:** Deny-list **redaction** (`LlmPromptRedaction`) before Azure OpenAI and for persisted trace text when enabled — see [docs/runbooks/LLM_PROMPT_REDACTION.md](docs/runbooks/LLM_PROMPT_REDACTION.md).
- **Independent test:** Penetration test **SoW** and **redacted summary** templates live under [docs/security/PEN_TEST_SOW_TEMPLATE.md](docs/security/PEN_TEST_SOW_TEMPLATE.md) and [docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md](docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md) for customer-facing engagements.
- **Transport:** **HTTPS** for production-style deployments; **HSTS** enabled in production hosting paths.
- **HTTP security headers:** Baseline headers include **Content-Security-Policy** (`default-src 'none'` for API JSON responses), **X-Frame-Options: DENY**, and **X-Content-Type-Options: nosniff** (see `ArchLucid.Host.Core` middleware).
- **Secrets:** Prefer **Azure Key Vault**; CI runs **gitleaks**; optional **server pre-receive** hook documented in [docs/security/GITLEAKS_PRE_RECEIVE.md](docs/security/GITLEAKS_PRE_RECEIVE.md); production startup validation rejects **placeholder API keys**.
- **Dependencies & artifacts:** **Trivy** (including container and IaC scans where configured), **CodeQL**, **NuGet** vulnerability audit in CI, and **CycloneDX** SBOM generation for supply-chain visibility.
- **Network:** **Private endpoints** for SQL and Blob-style storage; **SMB (port 445) is not exposed publicly** — align deployments with private connectivity and controlled boundaries.

## Supported versions

Security patches are applied to the **latest release branch / current default branch** of this repository. Older tags or forks may not receive backports unless explicitly communicated in release notes.

## PGP / encrypted email

**Public key publication procedure:** follow [`docs/security/PGP_KEY_GENERATION_RECIPE.md`](docs/security/PGP_KEY_GENERATION_RECIPE.md) (owner-self custodian, decision **2026-04-22** — [`docs/PENDING_QUESTIONS.md`](docs/PENDING_QUESTIONS.md) items **10**, **21**). Until the public key file exists at [`archlucid-ui/public/.well-known/pgp-key.txt`](../archlucid-ui/public/.well-known/pgp-key.txt) and is served at **`/.well-known/pgp-key.txt`** on the marketing site, use **plain email** to **`security@archlucid.com`** for coordinated disclosure.

**After publication:** add the **short fingerprint** (16 hex chars) from `gpg -K security@archlucid.com` on a line here for humans scanning `SECURITY.md` only — **Key ID:** *(owner completes)*.

**Contact file:** [`archlucid-ui/public/.well-known/security.txt`](../archlucid-ui/public/.well-known/security.txt) is published at `https://archlucid.com/.well-known/security.txt` when the marketing site is live.
