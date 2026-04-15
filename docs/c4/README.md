# C4 workspace (Structurizr DSL)

## Objective

Provide a **machine-readable** architecture model that can render **system context**, **containers**, and **components** without re-reading the entire repo.

## Files

| File | Purpose |
|------|---------|
| **`workspace.dsl`** | Structurizr DSL definitions (person, software system, containers, relationships). |

## Rendering

Use [Structurizr Lite](https://structurizr.com/help/lite) (Docker) or the Structurizr VS Code extension:

```bash
docker run -it --rm -p 8080:8080 -v "%cd%":/usr/local/structurizr structurizr/lite
```

Open `http://localhost:8080` and load **`workspace.dsl`** from this directory.

## Related

- **`docs/ARCHITECTURE_ON_A_PAGE.md`** — narrative + Mermaid.
- **`docs/bounded-context-map.md`** — domain boundaries.
