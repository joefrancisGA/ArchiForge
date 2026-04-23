> **Scope:** C# formatting (blank lines & layout) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# C# formatting (blank lines & layout)

> **See also:** [`docs/CSHARP_HOUSE_STYLE.md`](CSHARP_HOUSE_STYLE.md) is the human-readable index of every always-on Cursor rule (guard clauses, pattern matching, switch expressions, collection expressions, primary constructors, etc.). This page covers only the **mechanical** side — what `dotnet format` and the small Roslyn fixers under `scripts/` actually do.

The ArchLucid .NET codebase uses **EditorConfig** (repo root `.editorconfig`) so Visual Studio, Rider, and `dotnet format` share the same rules.

Notable choices for **readability**:

- **Blank line between `using` groups** (`dotnet_separate_import_directive_groups = true`), with `System.*` first.
- **Braces** on new lines for types, methods, and other block declarations (`csharp_new_line_before_open_brace = all`).
- **Single-statement control flow** (`if` / `else` / `for` / `foreach` / `while` / `lock` / `using`): **omit** `{ }` when syntax allows (`csharp_prefer_braces = false`, IDE0011). Use a block when there are multiple statements or you need a local scope.
- **No squeezed single-line blocks** (`csharp_preserve_single_line_blocks = false`).

## Apply formatting to the whole solution

From the repository root:

```bash
dotnet format ArchLucid.sln
```

To apply **only** the “omit braces for single statements” rule (IDE0011) without other style fixes:

```bash
dotnet format ArchLucid.sln style --diagnostics IDE0011 --severity info
```

Optional: whitespace-only (faster, fewer semantic changes):

```bash
dotnet format ArchLucid.sln --verbosity minimal --verify-no-changes
```

(`--verify-no-changes` is for CI; omit it when you want files updated.)

## Scripts

- **Windows:** `scripts\format-solution.cmd`
- **PowerShell:** `scripts\format-solution.ps1`

Run these on your machine if the IDE’s “Format Document” should match CI/team defaults.

### Simple auto-properties

`dotnet format` does not remove blank lines between adjacent `{ get; set; }` properties. To align with team layout (one line per simple property, **no** empty line between consecutive `get; set;` properties), run:

```bash
python scripts/collapse_simple_properties.py
```

See `.cursor/rules/CSharp-SimpleProperties-OneLine.mdc` for the full convention.

### Single-statement control flow (brace removal)

`dotnet format` with IDE0011 alone does not always rewrite every eligible `if { single; }` across a large tree. For a Roslyn pass that unwraps **only** blocks containing **exactly one** statement (and skips `try`/`catch`, locals, local functions, and multi-statement bodies), run from the repo root:

```bash
dotnet run --project scripts/RemoveEmbeddedStatementBraces/RemoveEmbeddedStatementBraces.csproj -c Release -- .
```

Pass a subdirectory (e.g. `ArchLucid.Api`) instead of `.` to limit the pass to one product assembly per session.

The tool also collapses eligible **`if` guard clauses** to the same physical line as the body (`if (cond) return …;`) when the body is a single-line `return` / `throw` / `continue` / `break` / `goto`, there is no comment trivia between `)` and the body, and the synthesized line length is at most **160** characters — see **`CSharp-Terse-01-GuardClausesSameLine.mdc`**. Multi-line `return` / `throw` bodies stay on the following line(s).

**Dangling `else` safety:** the tool does **not** unwrap a braced `then` branch when that `if` has an `else` and the branch contains a nested `if` (unwrap would reattach `else` to the inner `if` under C# parsing rules).

The tool excludes `bin/`, `obj/`, `.git/`, `*.g.cs`, and its own project folder. Many files may already match the convention or have two-statement bodies (e.g. `await` then `return`), so **fewer files may change** than you expect; that is normal.

See `.cursor/rules/CSharp-EmbeddedStatements-NoBraces.mdc` for the convention.

Per-assembly sweep progress: [`docs/TERSENESS_REWRITER_ASSEMBLY_CHECKLIST.md`](TERSENESS_REWRITER_ASSEMBLY_CHECKLIST.md).
