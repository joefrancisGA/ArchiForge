# GitHub Copilot instructions for ArchLucid

> **Audience:** GitHub Copilot (PR review, chat, code completion). This file is read **automatically** by Copilot when it reviews pull requests in this repository. Keep it short, opinionated, and link out for depth.
>
> **Last reviewed:** 2026-04-17

## What this product is

**ArchLucid** is an Azure-native architecture-governance platform. The product is mid-rename from **ArchiForge → ArchLucid**; both spellings still appear by design in places listed under "Allowed legacy literals" below. Do **not** flag those as inconsistencies.

Read these in order if you need product context before reviewing:

1. `docs/FIRST_5_DOCS.md` — five-document onboarding spine (install → first run → pilot → poster → pending questions)
2. `docs/SYSTEM_MAP.md` — Mermaid system flows
3. `docs/ARCHITECTURE_COMPONENTS.md`
4. `docs/API_CONTRACTS.md`
5. `docs/ARCHLUCID_RENAME_CHECKLIST.md` — what is allowed to still say `ArchiForge`

## What to focus on in reviews (in priority order)

1. **Security regressions** — see "Security non-negotiables" below. These are blocking.
2. **Architectural drift** — code that bypasses the orchestration layer, leaks persistence into controllers, or introduces a hidden coupling across bounded contexts.
3. **Operational risk** — anything that changes startup, configuration, or migration order without a corresponding update to a runbook in `docs/runbooks/`.
4. **Test coverage** — flag new public methods, branches, or error paths without unit tests. The project targets ~100% line + branch coverage.
5. **Style** — only after the above. Style nits should be a single rolled-up comment, not many inline ones.

When reviewing, follow **Critique mode**: list weaknesses **first**, then suggest specific improvements. Do not produce generic praise ("LGTM!", "nice work!") — it adds no signal.

## Security non-negotiables (always block)

These map to `.cursor/rules/Security-Default-Rule-Port-445-Alignment.mdc` and the user's standing security defaults. **Flag any PR that violates these as a blocking review comment**:

- **SMB / port 445 must never be exposed publicly.** Any Terraform, NSG, firewall rule, or Azure Storage configuration that allows `445` from `Internet`, `*`, `0.0.0.0/0`, or a public IP range is a hard block.
- **Storage access must use private endpoints.** Public storage account network access (`public_network_access_enabled = true`, `default_action = "Allow"`, missing `private_endpoint`) is a hard block unless the PR description explicitly justifies the exception.
- **Identity is Entra ID.** Reject new local users, static API keys checked into config, or shared secrets that bypass Entra. `appsettings*.json` must not contain real secrets — use Key Vault references or environment variables.
- **Least privilege & deny-by-default.** Wildcard role assignments (`Owner`, `Contributor` at subscription scope), `*` in IAM actions, or `--allow-all` flags require explicit justification.
- **No new TLS-disabled or HTTP-only endpoints.** Inbound HTTP without redirect-to-HTTPS, or `MinimumTlsVersion < 1.2`, is a hard block.
- **Secrets handling.** Watch for secrets logged via `ILogger`, written to telemetry, or returned in API responses. The repo has a log-sanitizer barrier (`.github/codeql/archlucid-csharp-log-sanitizer-models/`) — flag attempts to weaken or bypass it.
- **Gitleaks gate.** New credential-shaped strings must either be obvious test fixtures or be added to `.gitleaks.toml` allowlists with a comment. **Do not** use `sk_test_` / `sk_live_` shaped literals in tests or config samples — gitleaks treats them as real Stripe tokens; use non-Stripe-shaped placeholders (see `ArchLucidConfigurationRulesTests` Stripe billing secret tests). Full-history scans still see old blobs; allowlist entries must name the exact retired string and why.

## Architecture rules to enforce

These map to the user's standing architecture rules. Treat violations as review comments, not silent passes.

### Decomposition

Every non-trivial change should still be expressible as **interfaces, services, data models, orchestration**. Flag PRs that:

- Put business logic in controllers (belongs in `Application` or domain services).
- Put SQL or Dapper calls outside `ArchLucid.Persistence.Data.*` (Dapper repos) or `ArchLucid.Persistence` (domain ports).
- Bypass `AuthorityRunOrchestrator` or pipeline stages for run lifecycle changes.
- Introduce a new "manager" / "helper" / "util" class that obviously belongs in an existing seam (`IFindingEngine`, `IContextConnector`, `IAgentHandler`, `IAlertDeliveryChannel`). See `.cursor/rules/Navigation.mdc` for the canonical seams.

### Reuse first

Before approving a new helper, search the repo and the change description for an existing equivalent. The user is **aggressive about reuse** — duplicated parsers, duplicated DTOs, duplicated null-checks, or a second "options resolver" should be a comment.

### Infrastructure

- **All infrastructure must be representable in Terraform** (`infra/terraform-*/`). Reject PRs that introduce Azure resources via portal-only steps, ARM templates, or one-off scripts without a corresponding Terraform change.
- **Greenfield IaC** uses `archlucid` Terraform resource labels; do not reintroduce the substring `archiforge` in `infra/**/*.tf` (run `rg "archiforge" infra --glob "*.tf"` on Terraform PRs — merge-blocking grep job retired). Brownfield state migration (if any) is documented in `docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`.

### Data / DDL

- **All SQL DDL lives in a single master file per database**: `ArchLucid.sql`. New tables/columns go there.
- **Never modify historical migration files** `001` through `028` under `migrations/` (or wherever they live). They are immutable history. New schema changes go in a new numbered migration file plus the master DDL.

### Configuration & rename

- **Do not reintroduce silent fallbacks** for `ArchiForge*` config sections or `ARCHIFORGE_*` env vars. Phase 7.1–7.3 removed those by design; only **startup warnings** for legacy presence are acceptable.
- Treat any new `ArchiForge` literal in `.cs`, `.ts`, or `.tsx` as a CI failure waiting to happen — there's a CI guard in `.github/workflows/ci.yml` that rejects it.

### Allowed legacy literals (do **not** flag these)

- `docs/ARCHLUCID_RENAME_CHECKLIST.md` itself
- `docs/BREAKING_CHANGES.md` rows documenting **removed** legacy spellings
- `docs/MULTI_TENANT_RLS.md` SQL object names: `rls.ArchiforgeTenantScope`, `rls.archiforge_scope_predicate`, migration file `036_RlsArchiforgeTenantScope.sql`
- `.gitleaks.toml` dev-password allowlist entries

## C# / .NET coding conventions

These map to the user's standing C# rules and the `.cursor/rules/CSharp-*.mdc` files.

- **Each class in its own file.** Flag multi-class files unless they are tiny private records co-located with their owner.
- **Concrete types over `var`.** Prefer `Guid runId` over `var runId`. (Exception: `var` is acceptable for obvious anonymous types or LINQ projections where the type is unspeakable.)
- **LINQ over `foreach`** unless the loop has clear performance impact (hot path, allocation pressure, side-effects). Don't dogmatically convert imperative `foreach` to LINQ in PRs that aren't about performance.
- **Single-line `throw` after `if`** with no braces — see `.cursor/rules/SingleLineThrowNoBraces.mdc`.
- **Single-statement `if`/`for`/`foreach`/`while`/`using`/`lock` bodies** drop braces — see `.cursor/rules/CSharp-EmbeddedStatements-NoBraces.mdc`.
- **Simple auto-properties on one physical line**, no blank lines between adjacent ones — see `.cursor/rules/CSharp-SimpleProperties-OneLine.mdc`.
- **Blank line in front of `if` and `foreach`** unless it's the first statement in the method.
- **Always check nulls** at public method boundaries. Nullable reference types (`string?`) are a hint, not a substitute.
- **Modular methods.** A one-line method is fine and often preferred over an inline expression that future readers must parse.
- **Comment any code a developer with two years of experience may not understand.** Do **not** add narrating comments (`// loop over items`, `// return result`) — review such comments as noise.

### Tests

- **Do not use `ConfigureAwait(false)` in tests.** This is a project rule. Flag it.
- Target ~100% coverage; new public methods and new branches without tests are a comment.
- Test names should describe behavior; flag `Test1`, `ShouldWork`, etc.

### Data access

- **Prefer Dapper.** Heavy ORMs (EF Core entity tracking, change graphs) require explicit justification in the PR description.
- New repositories belong in `ArchLucid.Persistence.Data.*`; new domain ports belong in `ArchLucid.Persistence`.

## Architectural decisions in PR descriptions

When a PR adds or changes a component, the description should answer:

1. **What** is being built
2. **Why** this approach (trade-offs, alternatives considered)
3. **Security, scalability, reliability, cost** — explicitly, even if "not applicable, because …"

If the PR description is silent on these for a non-trivial change, ask for them in the review. Vague justifications ("best practice", "industry standard") are not sufficient.

## What **not** to do in reviews

- Do not suggest adding `ConfigureAwait(false)` in test projects.
- Do not suggest converting `var` → existing concrete type if the existing code already uses concrete types correctly (don't churn).
- Do not suggest replacing Dapper with Entity Framework.
- Do not suggest exposing storage publicly "for simplicity".
- Do not suggest removing the legacy `ArchiForge` literals listed in "Allowed legacy literals" above.
- Do not introduce libraries, APIs, or services that are not already in the repo's package manifests (`*.csproj`, `package.json`, `Pipfile`, `requirements.txt`) without flagging the new dependency for explicit human review.
- Do not produce content-free comments ("Looks good!", "Consider adding more tests" without specifics).

## Markdown / docs PRs

- Be **generous** with markdown. New features and ops-relevant changes should land with at least a brief doc update under `docs/` and, if operational, a runbook under `docs/runbooks/`.
- Architecture write-ups follow the **structured sections** convention: Objective, Assumptions, Constraints, Architecture Overview, Component Breakdown, Data Flow, Security Model, Operational Considerations.
- Anything describable as a system should be diagrammable: identify nodes, edges, flows. Mermaid diagrams in markdown are encouraged.

## When in doubt

- **Don't invent.** If you are uncertain whether an API, library, or Azure feature exists or behaves as claimed, **say so explicitly** in the review comment rather than asserting it.
- Ask the author to clarify architectural intent before suggesting alternatives — see the user's "Don't implement code unless the architectural intent is clear" rule.
