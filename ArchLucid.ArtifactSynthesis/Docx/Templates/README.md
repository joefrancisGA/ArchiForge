# Architecture DOCX template

## `architecture-template.docx` (optional)

If this file is present next to `ArchiForge.ArtifactSynthesis.dll` under `Docx/Templates/`, it is used as the **branded shell** (header, footer, fonts, styles, logo you add in Word).

If the file is **missing**, a built-in template is generated in memory (ArchiForge header/footer + custom paragraph styles).

### Layout contract

- The exporter **clears the document body** and rewrites section content.
- **Header, footer, and style definitions** from your template are preserved.
- Keep **one** final `w:sectPr` in the body (Word does this by default). The exporter moves it to the end after injecting sections.

### Customize in Word

1. Save a package as `architecture-template.docx` here (or build output `Docx/Templates/`).
2. Set up title page feel, header logo, footer page numbers, and styles: `Title`, `Heading1`, `Heading2`, plus custom `BodyText` and `TableHeader` (or rely on the generator’s `BodyText` / `TableHeader` ids when using the built-in template).

### Build output

The project copies `architecture-template.docx` to the output directory **only if the file exists** (see `ArchiForge.ArtifactSynthesis.csproj`).
