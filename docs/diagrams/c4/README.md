# C4 diagrams (PNG for exec / security)

**Purpose:** **Static PNGs** for audiences who will not open Mermaid in markdown. **Source files** (`.mmd`) are the canonical definitions; regenerate PNGs when the architecture changes.

| File | C4 level | Description |
|------|----------|-------------|
| [c4-context.mmd](c4-context.mmd) | **1 — Context** | People, ArchiForge, and main external systems (SQL, blob, Entra, Azure OpenAI). |
| [c4-container.mmd](c4-container.mmd) | **2 — Containers** | API, Worker, optional UI, data plane, identity, LLM. |
| [c4-component-api.mmd](c4-component-api.mmd) | **3 — Components (API only)** | Simplified internals of the API process. |

| Generated PNG | Source |
|---------------|--------|
| [c4-context.png](c4-context.png) | `c4-context.mmd` |
| [c4-container.png](c4-container.png) | `c4-container.mmd` |
| [c4-component-api.png](c4-component-api.png) | `c4-component-api.mmd` |

## Regenerate PNGs (maintainers)

Requires [Node.js](https://nodejs.org/) (LTS). From **this directory**:

```bash
npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-context.mmd -o c4-context.png -b white
npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-container.mmd -o c4-container.png -b white
npx -p "@mermaid-js/mermaid-cli@11" mmdc -i c4-component-api.mmd -o c4-component-api.png -b white
```

On **Windows PowerShell**, quote the package name so `@` is not treated as splatting. From repo root you can run **`scripts/generate-c4-png.ps1`**.

**Tips:**

- Use **`-b white`** (or **transparent**) so slides and Word render predictably.
- If a diagram fails to parse, update **Mermaid CLI** (`@mermaid-js/mermaid-cli`) to a current 11.x release.
- Keep labels **ASCII-heavy**; some renderers choke on smart quotes in `.mmd` files.

## Related narrative docs

- [GOLDEN_PATH.md](../../GOLDEN_PATH.md) — environment path.
- [ARCHITECTURE_CONTEXT.md](../../ARCHITECTURE_CONTEXT.md), [ARCHITECTURE_CONTAINERS.md](../../ARCHITECTURE_CONTAINERS.md) — prose + Mermaid in markdown.

**Last reviewed:** 2026-04-04
