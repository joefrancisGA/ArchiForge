> **Scope:** How to add a finding engine plugin (ArchLucid) - full detail, tables, and links in the sections below.

# How to add a finding engine plugin (ArchLucid)

## Objective

Load additional **`IFindingEngine`** implementations from **external assemblies** dropped into a plugin directory, without modifying core `ArchLucid.Decisioning` engine registration code.

## Configuration

Set **`ArchLucid:FindingEngines:PluginDirectory`** to an absolute or relative folder path (relative paths resolve from the process working directory — typically the API or worker content root).

When the directory is missing or empty, discovery is a no-op.

## Contract

- Implement **`ArchLucid.Decisioning.Interfaces.IFindingEngine`**.
- Expose a **parameterless public constructor** (plugins are instantiated once during discovery for metadata checks).
- Return a unique **`EngineType`** string that does **not** collide with built-in ids (see **`FindingEnginePluginDiscovery.BuiltInEngineTypeIds`**).
- Place the compiled assembly in the plugin directory as a **`.dll`** that does **not** start with `ArchLucid.` (those files are skipped to avoid loading product assemblies twice).

Discovery and registration are implemented in **`ArchLucid.Decisioning.Plugins.FindingEnginePluginDiscovery`** and **`RegisterPluginFindingEngines`** in **`ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.Decisioning.cs`**.

## Tests

- **`ArchLucid.Decisioning.Tests/Plugins/FindingEnginePluginDiscoveryTests.cs`**
- Template: **`templates/archlucid-finding-engine/`**

## Related

- **`docs/HOWTO_ADD_COMPARISON_TYPE.md`** — comparison domain extensions.
