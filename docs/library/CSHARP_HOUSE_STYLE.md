> **Scope:** ArchLucid C# House Style - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid C# House Style

> **Source of truth:** the `.mdc` rule files under `.cursor/rules/`. This document is a **human-readable index** that consolidates them so a new contributor (or AI agent) can read one page and understand the project's day-to-day C# style. When this doc and a rule file disagree, **the rule file wins** — open a PR to fix this doc.

---

## Why this exists

ArchLucid is a large, multi-project .NET 10 solution where most code is read more often than it is written. The conventions below are tuned to:

1. **Keep the happy path flat** — guards, returns, and continues stay at the top of a member; the "real" logic lives unindented at the bottom.
2. **Stay terse without sacrificing safety** — concrete types, explicit nulls, no `var`, no silent fallbacks.
3. **Be enforceable** — every rule is a Cursor rule and (where possible) backed by EditorConfig / `dotnet format` / a small Roslyn fixer in `scripts/`.

Read [`docs/library/FORMATTING.md`](FORMATTING.md) for the **mechanical** side (`dotnet format`, brace removal tool, simple-property collapser).

---

## How to apply this style

| What | Tool |
|------|------|
| Whitespace, brace placement, `using` ordering | `dotnet format ArchLucid.sln` |
| Remove `{ }` from single-statement `if` / `for` / `foreach` etc. | `dotnet run --project scripts/RemoveEmbeddedStatementBraces/RemoveEmbeddedStatementBraces.csproj -c Release -- .` |
| Collapse multi-line simple auto-properties | `python scripts/collapse_simple_properties.py` |
| Everything semantic in this doc | Cursor rules (auto-applied to `*.cs`) |

---

## Rule index — always-on for `**/*.cs`

These rules are loaded into every Cursor session (`alwaysApply: true`). Each link points to the authoritative `.mdc` file.

### Brace & guard discipline

| ID | Rule | Summary |
|----|------|---------|
| — | [`CSharp-EmbeddedStatements-NoBraces.mdc`](../../.cursor/rules/CSharp-EmbeddedStatements-NoBraces.mdc) | Omit `{ }` for single-statement `if` / `else` / `for` / `foreach` / `while` / `lock` / `using`. |
| — | [`CSharp-SingleEmbeddedStatement-OneLinePrefer.mdc`](../../.cursor/rules/CSharp-SingleEmbeddedStatement-OneLinePrefer.mdc) | Prefer **same physical line** for short braceless bodies; break when the assembled line is too long. |
| — | [`SingleLineThrowNoBraces.mdc`](../../.cursor/rules/SingleLineThrowNoBraces.mdc) | Single-line `throw` after `if`, no braces. |
| — | [`SingleLineContinueNoBraces.mdc`](../../.cursor/rules/SingleLineContinueNoBraces.mdc) | Single-line `continue` after `if`, same line when it reads cleanly. |
| 01 | [`CSharp-Terse-01-GuardClausesSameLine.mdc`](../../.cursor/rules/CSharp-Terse-01-GuardClausesSameLine.mdc) | Guards on the **same line** as `if` — `return;`, **`return expr;`**, `throw`, etc., when the assembled line stays short. |
| 12 | [`CSharp-Terse-12-EarlyReturnNoTrailingElse.mdc`(../../.cursor/rules/CSharp-Terse-12-EarlyReturnNoTrailingElse.mdc) | No `else` after a guarded `return` / `throw` / `continue`. |

### Null-safety

| ID | Rule | Summary |
|----|------|---------|
| 02 | [`CSharp-Terse-02-PatternMatching.mdc`(../../.cursor/rules/CSharp-Terse-02-PatternMatching.mdc) | Use `is null` / `is not null` / `is { … }` / `is T t` instead of `==` / `!=` / `as`. |
| 06 | [`CSharp-Terse-06-IsNullPatterns.mdc`(../../.cursor/rules/CSharp-Terse-06-IsNullPatterns.mdc) | Strict subset of #02 — **all** null checks use `is null` / `is not null`. |
| 07 | [`CSharp-Terse-07-NullCoalescing.mdc`(../../.cursor/rules/CSharp-Terse-07-NullCoalescing.mdc) | `??` and `??=` over if-null fallback blocks; throw via guard, not via `??`. |

### Expression style

| ID | Rule | Summary |
|----|------|---------|
| 03 | [`CSharp-Terse-03-SwitchExpressions.mdc`(../../.cursor/rules/CSharp-Terse-03-SwitchExpressions.mdc) | `switch` expression for value mappings; always include a discard arm `_ =>`. |
| 09 | [`CSharp-Terse-09-ExpressionBodiedMembers.mdc`(../../.cursor/rules/CSharp-Terse-09-ExpressionBodiedMembers.mdc) | `=>` for trivial methods, properties, ctors, operators. |
| 10 | [`CSharp-Terse-10-LinqGuardPipelines.mdc`(../../.cursor/rules/CSharp-Terse-10-LinqGuardPipelines.mdc) | LINQ pipeline with null-safety over manual `foreach` + accumulator. |

### Construction

| ID | Rule | Summary |
|----|------|---------|
| 04 | [`CSharp-Terse-04-CollectionExpressions.mdc`(../../.cursor/rules/CSharp-Terse-04-CollectionExpressions.mdc) | `[]`, `[a, b]`, `[..src, extra]` over `new T[]{}` / `Array.Empty<T>()`. |
| 05 | [`CSharp-Terse-05-TargetTypedNew.mdc`(../../.cursor/rules/CSharp-Terse-05-TargetTypedNew.mdc) | `new()` on the right when the type is on the left. |
| 11 | [`CSharp-Terse-11-PrimaryConstructors.mdc`(../../.cursor/rules/CSharp-Terse-11-PrimaryConstructors.mdc) | Primary ctors for DI services and value-shaped records; null checks **stay explicit**. |

### Slicing

| ID | Rule | Summary |
|----|------|---------|
| 08 | [`CSharp-Terse-08-RangeAndIndex.mdc`(../../.cursor/rules/CSharp-Terse-08-RangeAndIndex.mdc) | `^n` and `..` over `Length-n` / `Substring` / `.Slice`. |

### Layout

| Rule | Summary |
|------|---------|
| [`CSharp-SimpleProperties-OneLine.mdc`(../../.cursor/rules/CSharp-SimpleProperties-OneLine.mdc) | Simple auto-properties on **one line**, no blank lines between consecutive simple properties. |

---

## The non-negotiables (project-wide user rules)

These are applied above and beyond the Cursor rules — they come from the user's permanent preferences and are **never** relaxed by a terseness rule:

1. **Concrete types over `var`.** Always.
2. **Each class in its own file.** Always.
3. **Always check nulls.** Validate at the boundary, throw with the same-line guard form.
4. **One blank line before `if` and `foreach`** — except when it's the first line of the method/block.
5. **LINQ over `foreach`** unless LINQ would degrade performance (call out the perf reason in a comment if so).
6. **No `ConfigureAwait(false)` in tests.**
7. **All SQL DDL in one file per database** (`ArchLucid.sql`, plus migration scripts under `sql/migrations/`).
8. **Explain non-obvious code with a comment** ("would a developer with two years of experience understand this in 30 seconds?"). Do **not** narrate obvious code (`// Increment counter` is forbidden).
9. **Architectural intent before code.** If requirements are ambiguous, ask or state assumptions explicitly.
10. **Modular methods are fine even if a method has only one line of code.** Reuse aggressively.

---

## Putting it together — a worked example

Below is what a typical service method looks like when **all** the rules are applied together. Compare with the "before" form to see why the rules exist.

### Before

```csharp
public class TenantSummaryService : ITenantSummaryService
{
    private readonly ITenantRepository _repository;
    private readonly IClock _clock;

    public TenantSummaryService(ITenantRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<List<TenantSummary>> GetActiveAsync(
        Guid scopeId,
        CancellationToken ct)
    {
        if (scopeId == Guid.Empty)
        {
            throw new ArgumentException("Scope id is required.", nameof(scopeId));
        }

        var tenants = await _repository.LoadByScopeAsync(scopeId, ct);
        if (tenants == null)
        {
            tenants = new List<Tenant>();
        }

        var result = new List<TenantSummary>();
        foreach (var tenant in tenants)
        {
            if (tenant == null)
            {
                continue;
            }
            if (!tenant.IsActive)
            {
                continue;
            }
            else
            {
                result.Add(new TenantSummary(tenant.Id, tenant.Name, _clock.UtcNow));
            }
        }
        return result;
    }
}
```

### After

```csharp
public sealed class TenantSummaryService(
    ITenantRepository repository,
    IClock clock) : ITenantSummaryService
{
    public async Task<List<TenantSummary>> GetActiveAsync(Guid scopeId, CancellationToken ct)
    {
        if (scopeId == Guid.Empty) throw new ArgumentException("Scope id is required.", nameof(scopeId));
        if (repository is null)    throw new ArgumentNullException(nameof(repository));
        if (clock is null)         throw new ArgumentNullException(nameof(clock));

        IReadOnlyList<Tenant> tenants = await repository.LoadByScopeAsync(scopeId, ct) ?? [];
        DateTime now = clock.UtcNow;

        return tenants
            .Where(t => t is { IsActive: true })
            .Select(t => new TenantSummary(t.Id, t.Name, now))
            .ToList();
    }
}
```

What changed and which rule pulled the lever:

| Edit | Rule(s) |
|------|---------|
| `class … : I…` → primary ctor | 11 |
| Hand-rolled field assignment removed | 11 |
| `if (… == Guid.Empty) { throw … }` → same-line throw | 01 + `SingleLineThrowNoBraces` |
| `== null` → `is null` | 02, 06 |
| `tenants ?? new List<Tenant>()` → `?? []` | 04, 07 |
| `var` → `IReadOnlyList<Tenant>` / `DateTime` | user rule (concrete types) |
| `foreach` + `result.Add(...)` → LINQ pipeline | 10 |
| Property pattern `is { IsActive: true }` | 02 |
| Trailing `else { … }` after `continue` removed | 12 |
| Final block reduced to `return …;` (single expression) | candidate for 09 if extracted to its own helper |

---

## When to break the rules

Every rule lists a **Scope** section that calls out exemptions. The recurring themes:

- **Block bodies stay** when there are multiple statements, locals, `try` / `catch`, or in-method comments that need to live with the code.
- **Expression trees** (Moq `It.Is<T>(e => …)`, EF Core predicates compiled to SQL, etc.): C# disallows `is` / `is not` / other pattern forms inside expression trees (**CS8122**). Keep `== null` / `!= null` there and add a one-line comment pointing at CS8122 so reviewers know it is intentional.
- **`switch` statements stay** when arms need awaits or side effects.
- **`foreach` stays** when awaiting per element, writing to multiple sinks, or a measured allocation matters (call out the reason in a comment).
- **Explicit constructors stay** when you need overload chaining, capacity, comparer, or other ctor arguments.
- **Block guards stay** when the guard does more than throw/return/continue — e.g. logging then throwing.

When a rule and an enterprise-realism concern collide (incomplete requirements, organizational constraints, imperfect teams), prefer the **safer, clearer** form and add a one-line comment explaining why.

---

## Maintaining this doc

- Add a new terseness rule: drop a new `CSharp-Terse-NN-…mdc` file under `.cursor/rules/` with `alwaysApply: true`, then add it to the index table above.
- Retire a rule: change `alwaysApply` to `false` (and explain why in this doc) before deleting the file, so existing references in PRs don't 404.
- Bulk style fixes belong in `scripts/` and should be linked from [`docs/FORMATTING.md`](FORMATTING.md), not from here.

---

## Related docs

- [`docs/FORMATTING.md`](FORMATTING.md) — `dotnet format`, brace removal tool, property collapser.
- [`docs/SQL_DDL_DISCIPLINE.md`](SQL_DDL_DISCIPLINE.md) — single-DDL-file rule for SQL.
- [`docs/METHOD_DOCUMENTATION.md`](METHOD_DOCUMENTATION.md) — when and how to add comments on methods.
- [`docs/REPO_HYGIENE.md`](REPO_HYGIENE.md) — file layout, naming, and repo-level conventions.
