> **Scope:** Consulting DOCX template configuration - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Consulting DOCX template configuration

## Objective

Document the **`ConsultingDocxTemplate`** and **`ConsultingDocxTemplateProfiles`** sections in `ArchLucid.Api/appsettings.json` (and environment-specific overrides). These settings drive branding, section toggles, and narrative defaults for consulting-style Word exports produced from architecture analysis.

## Assumptions

- Operators edit JSON or App Service / Key Vault–backed settings; no hot reload is assumed for template changes.
- Hex colour strings are **without** a leading `#` (see existing samples in `appsettings.json`).

## Configuration sections

### `ConsultingDocxTemplate`

| Key | Role |
|-----|------|
| **`OrganizationName`**, **`DocumentTitle`**, **`SubtitleFormat`**, **`GeneratedByLine`** | Cover and header copy. **`SubtitleFormat`** may include `{SystemName}` placeholders consumed by the export pipeline. |
| **`PrimaryColorHex`**, **`SecondaryColorHex`**, **`AccentFillHex`**, **`BodyColorHex`**, **`SubtleColorHex`** | Theme colours for headings, tables, and body text in the generated document. |
| **`IncludeDocumentControl`**, **`IncludeTableOfContents`**, section `Include*` flags | Boolean gates for major document sections (executive summary, evidence, governance, appendices, etc.). |
| **`IncludeLogo`**, **`LogoPath`** | Optional logo embedding; when **`IncludeLogo`** is false, **`LogoPath`** is ignored. |
| **`ExecutiveSummaryTextTemplate`**, **`ArchitectureOverviewIntro`**, **`ConclusionsText`** | Narrative templates; placeholders such as `{SystemName}`, `{OrganizationName}`, and aggregate counts (`{ServiceCount}`, etc.) are substituted at export time. |

Binding type: **`ConsultingDocxTemplateOptions`** (`ArchLucid.Api.Configuration`), registered in **`RegisterRunExportAndArchitectureAnalysis`**.

### `ConsultingDocxTemplateProfiles`

| Key | Role |
|-----|------|
| **`Profiles`** | Map of profile id → **`ConsultingDocxTemplateProfileOptions`** (e.g. `internal`, `executive`). Each profile can override colours, titles, which sections to include, and audience-facing metadata (`ProfileDisplayName`, `IntendedAudience`, **`DisplayOrder`**). |

Resolution is handled by **`IConsultingDocxTemplateProfileResolver`** / **`DefaultConsultingDocxTemplateProfileResolver`**: API and export flows pick a profile (often from request or consulting export selector) and merge with the base template.

## Operational notes

- **Security:** Do not put secrets in template JSON; paths like **`LogoPath`** should point to files readable by the API process only.
- **Reliability:** Invalid or missing profile ids should surface as validation or 4xx responses at the consulting export endpoints—see API tests and `IConsultingDocxExportProfileSelector` behaviour.
- **Cost:** Template changes do not affect inference or SQL cost; they only change document generation CPU time marginally.

## Related code

- **`ArchLucid.Api/Configuration/ConsultingDocxTemplateOptions.cs`** (and profile types).
- **`ArchLucid.Application`** consulting DOCX export services referenced from **`ArchitectureAnalysisConsultingDocxExportService`**.
- **`docs/ARCHITECTURE_COMPONENTS.md`** (application export surface).

## Evolution

- For multi-tenant branding, a future step is to move base template defaults to database or blob storage keyed by tenant, keeping **`appsettings.json`** as fallbacks only.
