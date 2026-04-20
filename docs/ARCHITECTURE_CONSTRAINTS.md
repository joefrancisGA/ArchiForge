> **Scope:** Architecture constraint tests (NetArchTest) - full detail, tables, and links in the sections below.

# Architecture constraint tests (NetArchTest)

Automated checks that selected **ArchLucid** assemblies respect layering and dependency boundaries. Implementation: **`ArchLucid.Architecture.Tests`** ([`DependencyConstraintTests.cs`](../ArchLucid.Architecture.Tests/DependencyConstraintTests.cs)), using **[NetArchTest.Rules](https://github.com/BenMorris/NetArchTest)** (central version in [`Directory.Packages.props`](../Directory.Packages.props)).

**See also:** [ARCHITECTURE_COMPONENTS.md](ARCHITECTURE_COMPONENTS.md) (what each module is for), [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) (how `Suite=Core` and fast-core filters run in CI and locally).

---

## 1. Objective

Catch **accidental coupling** early: foundation assemblies pulling in hosts, domain modules referencing SQL/persistence facades, persistence sub-modules referencing the wrong sibling assemblies, or the CLI taking a dependency on the API **host** assembly instead of the HTTP **client**.

---

## 2. Assumptions

- **Namespace prefixes** are a stable proxy for “depends on area X” when using NetArchTest `HaveDependencyOn` / `HaveDependencyOnAny` (prefix semantics per library).
- **Persistence split assemblies** intentionally share logical areas under `ArchLucid.Persistence.*` in source; **assembly references** are the reliable signal for submodule boundaries (Tier 2), not namespace strings alone.
- The CLI legitimately uses **`ArchLucid.Api.Client`** (generated OpenAPI client under `ArchLucid.Api.Client.Generated`). That must **not** be confused with a reference to the **`ArchLucid.Api`** host assembly.

---

## 3. Constraints

- Rules are **test-only**: no production project references NetArchTest.
- **One `[Fact]` per rule** so CI output names the exact violation.
- Each test is tagged **`[Trait("Suite", "Core")]`** so it runs with the corset / fast-core pipelines (see [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)).
- Forbidden namespace lists for Tier 1 live in [`ArchitectureConstraintNamespaces.cs`](../ArchLucid.Architecture.Tests/ArchitectureConstraintNamespaces.cs); extend those arrays when new first-party `ArchLucid.*` areas appear.

---

## 4. Architecture overview

| Tier | Scope | Mechanism |
|------|--------|-----------|
| **1** | **Core**, **Contracts**, **Contracts.Abstractions** | NetArchTest `ShouldNot().HaveDependencyOnAny(...)` |
| **2** | **Persistence.Coordination**, **Persistence.Integration** vs Runtime / Advisory / Alerts | `Assembly.GetReferencedAssemblies()` + FluentAssertions |
| **3** | **Decisioning**, **KnowledgeGraph**, **ContextIngestion**, **ArtifactSynthesis** vs **`ArchLucid.Persistence`** | NetArchTest `ShouldNot().HaveDependencyOn("ArchLucid.Persistence")` |
| **4** | **Cli** vs persistence and API host | NetArchTest for `ArchLucid.Persistence`; **assembly metadata** for `ArchLucid.Api` (see below) |

### Why Tier 4 uses assembly metadata for `ArchLucid.Api`

`HaveDependencyOn("ArchLucid.Api")` matches any namespace that **starts with** that prefix, including **`ArchLucid.Api.Client`**. The intended rule is: **no project reference to the `ArchLucid.Api` assembly** (the ASP.NET host). The test **`Cli_must_not_reference_Api_assembly`** asserts `GetReferencedAssemblies()` does not contain `ArchLucid.Api`, while still allowing **`ArchLucid.Api.Client`**.

---

## 5. Component breakdown

| Piece | Role |
|--------|------|
| **`ArchLucid.Architecture.Tests`** | Holds rules; references only the assemblies under test (no product code changes). |
| **`ArchitectureConstraintNamespaces`** | Single place to maintain Tier 1 forbidden prefix lists. |
| **`DependencyConstraintTests`** | One fact per tier rule; uses anchor types (`typeof(...).Assembly`) so renaming types inside an assembly does not break resolution unnecessarily. |

---

## 6. Data flow

At test run time, NetArchTest loads the target assembly and walks type references to evaluate dependency rules. Tier 2 and the CLI API-host check use **assembly reference metadata** only (no IL graph).

---

## 7. Security model

These tests do not enforce runtime security controls. They reduce risk indirectly by **preventing layering escapes** that could drag sensitive infrastructure (SQL, host internals) into unintended tiers.

---

## 8. Operational considerations

**Run locally (repo root):**

```bash
dotnet test ArchLucid.sln --filter "Suite=Core"
```

Fast core (excludes slow/integration-tagged tests elsewhere; architecture tests use `Category=Unit`):

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

**NetArchTest quirk (string field constants):** For `ShouldNot().HaveDependencyOnAny(...)`, the library scans **compile-time string constants on fields** and treats **`:`** like **`.`** as a namespace separator (see `NamespaceTree` in NetArchTest). A `public const string` such as `"ArchLucid:Persistence"` is therefore parsed as **`ArchLucid.Persistence`** and can fail **`Core_must_not_depend_on_any_solution_project`** even though Core has no real reference to the persistence assembly. Prefer a **`static` property** that builds the path with `string.Concat` (or keep the literal only in a host layer), as in **`ArchLucidPersistenceOptions.SectionPath`**.

**Adding a rule:** Prefer a new **`[Fact]`** with `Suite=Core` (and `Category=Unit` unless you have a reason not to). Reuse `ArchitectureConstraintNamespaces` for Tier 1–style prefix sets. For “must not reference assembly X”, prefer **`GetReferencedAssemblies()`** when namespace-prefix checks would false-positive.

**CI:** The same `Suite=Core` filter used by the “fast core” job picks up this project once it is in **`ArchLucid.sln`**.

---

## 9. Evolution

If the solution gains new **leaf** or **foundation** assemblies, update **`ForbiddenFromCore`** / **`ForbiddenFromContracts`** / **`ForbiddenFromContractsAbstractions`** so Tier 1 stays complete. If persistence splits further, add Tier 2-style assembly reference facts mirroring the intended DAG.

**DDL smoke (tenant scope on `dbo.Runs`):** **`TenantScopedTableDdlTests`** in **`ArchLucid.Architecture.Tests`** reads **`ArchLucid.Persistence/Scripts/ArchLucid.sql`** and asserts the **`dbo.Runs`** `CREATE TABLE` block includes **`TenantId`**, **`WorkspaceId`**, and **`ProjectId`** — a cheap guard when extending the master DDL (not a substitute for full RLS reviews; see **`docs/security/MULTI_TENANT_RLS.md`**).
