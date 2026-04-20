> **Scope:** RemoveEmbeddedStatementBraces assembly sweep - full detail, tables, and links in the sections below.

# `RemoveEmbeddedStatementBraces` assembly sweep

Tracks [`scripts/RemoveEmbeddedStatementBraces`](../scripts/RemoveEmbeddedStatementBraces/RemoveEmbeddedStatementBraces.csproj) runs (brace unwrap + **Terse-01** same-line guards where eligible). The tool also **refuses** to unwrap a braced `then` when the `if` has an `else` and the inner statement is a nested `if` (dangling-else safety).

**Command (scoped):**

```bash
dotnet run --project scripts/RemoveEmbeddedStatementBraces/RemoveEmbeddedStatementBraces.csproj -c Release -- "<repo-root>/<AssemblyDir>"
```

## Status (2026-04-19 session)

| Assembly | Rewriter | Verified |
|----------|----------|----------|
| ArchLucid.Api | prior session | `dotnet test ArchLucid.Api.Tests` |
| ArchLucid.Contracts.Abstractions | no-op (0 files) | (no tests project) |
| ArchLucid.Contracts | done | `dotnet test ArchLucid.Contracts.Tests` |
| ArchLucid.AgentSimulator | done | `dotnet build` (no `*.Tests`) |
| ArchLucid.ContextIngestion | done | `dotnet test ArchLucid.ContextIngestion.Tests` |
| ArchLucid.KnowledgeGraph | done | `dotnet test ArchLucid.KnowledgeGraph.Tests` |
| ArchLucid.Provenance | done | `dotnet test ArchLucid.Provenance.Tests` |
| ArchLucid.Retrieval | done | `dotnet test ArchLucid.Retrieval.Tests` |
| ArchLucid.ArtifactSynthesis | done | `dotnet test ArchLucid.ArtifactSynthesis.Tests` |
| ArchLucid.Decisioning | done | `dotnet test ArchLucid.Decisioning.Tests` |
| ArchLucid.Core | done | `dotnet test ArchLucid.Core.Tests` |
| ArchLucid.Application | done + manual fix `RunDetailQueryService` (dangling-else) | `dotnet test ArchLucid.Application.Tests` |
| ArchLucid.Coordinator | done | `dotnet test ArchLucid.Coordinator.Tests` |
| ArchLucid.AgentRuntime | done | `dotnet test ArchLucid.AgentRuntime.Tests` |
| ArchLucid.Persistence | done | `dotnet test ArchLucid.Persistence.Tests` |
| ArchLucid.Persistence.Integration | done | (covered by Persistence.Tests build graph) |
| ArchLucid.Persistence.Coordination | done | (covered by Persistence.Tests build graph) |
| ArchLucid.Persistence.Advisory | done | (covered by Persistence.Tests build graph) |
| ArchLucid.Persistence.Alerts | done | (covered by Persistence.Tests build graph) |
| ArchLucid.Persistence.Runtime | done | (covered by Persistence.Tests build graph) |
| ArchLucid.Host.Core | done | (covered by Host.Composition.Tests + downstream) |
| ArchLucid.Host.Composition | done | `dotnet test ArchLucid.Host.Composition.Tests` |
| ArchLucid.Api.Client | no-op (0 files) | `dotnet test ArchLucid.Api.Client.Tests` |
| ArchLucid.Cli | done | `dotnet test ArchLucid.Cli.Tests` |
| ArchLucid.Backfill.Cli | done | `dotnet build` (no tests project) |
| ArchLucid.Jobs.Cli | done | `dotnet test ArchLucid.Jobs.Cli.Tests` |
| ArchLucid.Worker | done | `dotnet build` (no `ArchLucid.Worker.Tests`) |

**Skipped (not product code or out of scope):** `ArchLucid.TestSupport`, `ArchLucid.Benchmarks`, `*.Tests`, `ArchLucid.Architecture.Tests`.

**Follow-up:** Re-run `dotnet test ArchLucid.Api.Tests` after large dependency changes if CI is sensitive; this session re-verified Api tests once after Host/Persistence edits.
