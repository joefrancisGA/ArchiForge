using ArchiForge.Application.Diagrams;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using DrBlip = DocumentFormat.OpenXml.Drawing.Blip;
using DrBlipFill = DocumentFormat.OpenXml.Drawing.Pictures.BlipFill;
using DrFillRectangle = DocumentFormat.OpenXml.Drawing.FillRectangle;
using DrGraphicFrameLocks = DocumentFormat.OpenXml.Drawing.GraphicFrameLocks;
using DrNonVisualDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using DrNonVisualPictureDrawingProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties;
using DrNonVisualPictureProperties = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties;
using DrPicture = DocumentFormat.OpenXml.Drawing.Pictures.Picture;
using DrShapeProperties = DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties;
using DrStretch = DocumentFormat.OpenXml.Drawing.Stretch;
using WpBottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using WpBreak = DocumentFormat.OpenXml.Wordprocessing.Break;
using WpInsideHorizontalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder;
using WpInsideVerticalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder;
using WpLeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using WpNonVisualGraphicFrameDrawingProperties =
    DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WpParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using WpRightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using WpRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WpRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using WpShading = DocumentFormat.OpenXml.Wordprocessing.Shading;
using WpSpacingBetweenLines = DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines;
using WpTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using WpTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using WpTableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using WpTableCellWidth = DocumentFormat.OpenXml.Wordprocessing.TableCellWidth;
using WpTableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using WpTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using WpText = DocumentFormat.OpenXml.Wordprocessing.Text;
using WpTopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;

namespace ArchiForge.Application.Analysis;

/// <summary>
/// Consolidates all OpenXML usage for the consulting DOCX export into one place.
/// Export services should depend on this composer instead of manipulating OpenXML types directly.
/// </summary>
internal static class ConsultingDocxOpenXmlComposer
{
    private const string MermaidLanguage = "mermaid";

    public static async Task<byte[]> GenerateAsync(
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options,
        IDiagramImageRenderer diagramImageRenderer,
        IDocumentLogoProvider logoProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagramImageRenderer);
        ArgumentNullException.ThrowIfNull(logoProvider);

        using MemoryStream stream = new();

        using (WordprocessingDocument document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!;

            AddStylesPart(mainPart, options);

            await AddCoverPageAsync(mainPart, body, report, options, logoProvider, cancellationToken);
            AddPageBreak(body);

            if (options.IncludeDocumentControl)
            {
                AddDocumentControl(body, report);
                AddPageBreak(body);
            }

            if (options.IncludeTableOfContents)
            {
                AddTableOfContentsPlaceholder(body);
                AddPageBreak(body);
            }

            if (options.IncludeExecutiveSummary)
            
                AddExecutiveSummary(body, report, options);
            

            if (options.IncludeArchitectureOverview)
            
                await AddArchitectureOverviewAsync(body, mainPart, report, options, diagramImageRenderer, cancellationToken);
            

            if (options.IncludeEvidenceAndConstraints)
            
                AddEvidenceAndConstraints(body, report);
            

            if (options.IncludeArchitectureDetails)
            
                AddArchitectureDetails(body, report);
            

            if (options.IncludeGovernanceAndControls)
            
                AddGovernanceAndControls(body, report);
            

            if (options.IncludeExplainabilitySection)
            
                AddExplainabilitySection(body, report, options);
            

            if (options.IncludeConclusions)
            
                AddConclusions(body, report, options);
            

            AddAppendices(body, report, options);

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static async Task AddCoverPageAsync(
        MainDocumentPart mainPart,
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options,
        IDocumentLogoProvider logoProvider,
        CancellationToken cancellationToken)
    {
        if (options.IncludeLogo)
        {
            byte[]? logoBytes = await logoProvider.GetLogoBytesAsync(options, cancellationToken);
            if (logoBytes is not null && logoBytes.Length > 0)
            {
                AddImageToBody(mainPart, body, logoBytes, "Document Logo", 2_200_000L, 700_000L);
                AddSpacer(body, 2);
            }
        }

        AddStyledParagraph(body, options.DocumentTitle, "Title");

        string systemName = report.Evidence?.SystemName
                            ?? report.Manifest?.SystemName
                            ?? "Architecture Run";

        string subtitle = options.SubtitleFormat
            .Replace("{SystemName}", systemName, StringComparison.OrdinalIgnoreCase)
            .Replace("{RunId}", report.Run.RunId, StringComparison.OrdinalIgnoreCase)
            .Replace("{OrganizationName}", options.OrganizationName, StringComparison.OrdinalIgnoreCase);

        AddSpacer(body, 2);
        AddStyledParagraph(body, subtitle, "Subtitle");
        AddSpacer(body, 2);

        AddStyledParagraph(body, $"Run ID: {report.Run.RunId}", "BodyText");
        AddStyledParagraph(body, $"Request ID: {report.Run.RequestId}", "BodyText");
        AddStyledParagraph(body, $"Generated UTC: {DateTime.UtcNow:O}", "BodyText");

        if (!string.IsNullOrWhiteSpace(report.Run.CurrentManifestVersion))
        
            AddStyledParagraph(body, $"Manifest Version: {report.Run.CurrentManifestVersion}", "BodyText");
        

        AddSpacer(body, 6);
        AddStyledParagraph(body, options.GeneratedByLine, "Subtle");
    }

    private static void AddDocumentControl(Body body, ArchitectureAnalysisReport report)
    {
        AddHeading(body, "Document Control", 1);

        AddStyledParagraph(body, "This document was generated from the ArchiForge analysis pipeline.", "BodyText");
        AddSpacer(body);

        AddKeyValueTable(body, [
            ("Document Type", "Architecture Analysis Report"),
            ("Run ID", report.Run.RunId),
            ("Request ID", report.Run.RequestId),
            ("Run Status", report.Run.Status.ToString()),
            ("Created UTC", report.Run.CreatedUtc.ToString("O")),
            ("Completed UTC", report.Run.CompletedUtc?.ToString("O") ?? "n/a"),
            ("Manifest Version", report.Run.CurrentManifestVersion ?? "n/a")
        ]);
    }

    private static void AddTableOfContentsPlaceholder(Body body)
    {
        AddHeading(body, "Table of Contents", 1);
        AddStyledParagraph(body, "Update fields in Word to refresh the table of contents.", "Subtle");
        AddSpacer(body);

        foreach (string item in new[]
                 {
                     "1. Executive Summary",
                     "2. Architecture Overview",
                     "3. Evidence and Constraints",
                     "4. Architecture Details",
                     "5. Governance and Controls",
                     "6. Explainability and Execution Review",
                     "7. Conclusions",
                     "Appendix A. Mermaid Source",
                     "Appendix B. Execution Trace Index",
                     "Appendix C. Determinism and Comparison"
                 })
        
            AddBullet(body, item);
        
    }

    private static void AddExecutiveSummary(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        AddHeading(body, "Executive Summary", 1);

        string systemName = report.Manifest?.SystemName
                            ?? report.Evidence?.SystemName
                            ?? "the requested system";

        int serviceCount = report.Manifest?.Services.Count ?? 0;
        int datastoreCount = report.Manifest?.Datastores.Count ?? 0;
        int controlCount = report.Manifest?.Governance.RequiredControls.Count ?? 0;

        string text = options.ExecutiveSummaryTextTemplate
            .Replace("{SystemName}", systemName, StringComparison.OrdinalIgnoreCase)
            .Replace("{OrganizationName}", options.OrganizationName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ServiceCount}", serviceCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{DatastoreCount}", datastoreCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{ControlCount}", controlCount.ToString(), StringComparison.OrdinalIgnoreCase);

        AddStyledParagraph(body, text, "BodyText");

        if (report.Warnings.Count > 0)
        
            AddCallout(body, "Key warnings were identified during analysis and should be reviewed before approval.", options);
        
    }

    private static async Task AddArchitectureOverviewAsync(
        Body body,
        MainDocumentPart mainPart,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options,
        IDiagramImageRenderer diagramImageRenderer,
        CancellationToken cancellationToken)
    {
        AddHeading(body, "Architecture Overview", 1);

        if (report.Manifest is null)
        {
            AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");
            return;
        }

        AddStyledParagraph(body, options.ArchitectureOverviewIntro, "BodyText");

        if (!string.IsNullOrWhiteSpace(report.Diagram))
        {
            byte[]? imageBytes = await diagramImageRenderer.RenderMermaidPngAsync(
                report.Diagram,
                cancellationToken);

            if (imageBytes is not null && imageBytes.Length > 0)
            
                AddImageToBody(mainPart, body, imageBytes, "Architecture Overview Diagram", 6_200_000L, 3_600_000L);
            
            else
            
                AddCallout(body, "Diagram image rendering was unavailable. Mermaid source is included in Appendix A.", options);
            
        }
    }

    private static void AddEvidenceAndConstraints(Body body, ArchitectureAnalysisReport report)
    {
        AddHeading(body, "Evidence and Constraints", 1);

        if (report.Evidence is null)
        {
            AddStyledParagraph(body, "No evidence package was available for this run.", "BodyText");
            return;
        }

        AddHeading(body, "Request Context", 2);
        AddStyledParagraph(body, report.Evidence.Request.Description, "BodyText");

        if (report.Evidence.Request.Constraints.Count > 0)
        {
            AddHeading(body, "Constraints", 2);
            foreach (string item in report.Evidence.Request.Constraints)
            
                AddBullet(body, item);
            
        }

        if (report.Evidence.Request.RequiredCapabilities.Count > 0)
        {
            AddHeading(body, "Required Capabilities", 2);
            foreach (string item in report.Evidence.Request.RequiredCapabilities)
            
                AddBullet(body, item);
            
        }

        if (report.Evidence.Policies.Count <= 0)
            return;

        AddHeading(body, "Policy Evidence", 2);

        foreach (PolicyEvidence policy in report.Evidence.Policies.OrderBy(x => x.Title))
        {
            AddStyledParagraph(body, policy.Title, "Strong");
            AddBullet(body, $"Policy ID: {policy.PolicyId}");
            AddBullet(body, $"Summary: {policy.Summary}");

            if (policy.RequiredControls.Count > 0)
            
                AddBullet(body, $"Required Controls: {string.Join(", ", policy.RequiredControls)}");
            
        }
    }

    private static void AddArchitectureDetails(Body body, ArchitectureAnalysisReport report)
    {
        AddHeading(body, "Architecture Details", 1);

        if (report.Manifest is null)
        {
            AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");
            return;
        }

        if (report.Manifest.Services.Count > 0)
        {
            AddHeading(body, "Services", 2);

            foreach (ManifestService service in report.Manifest.Services.OrderBy(x => x.ServiceName))
            {
                AddStyledParagraph(body, service.ServiceName, "Strong");
                AddBullet(body, $"Type: {service.ServiceType}");
                AddBullet(body, $"Platform: {service.RuntimePlatform}");

                if (!string.IsNullOrWhiteSpace(service.Purpose))
                
                    AddBullet(body, $"Purpose: {service.Purpose}");
                

                if (service.RequiredControls.Count > 0)
                
                    AddBullet(body, $"Required Controls: {string.Join(", ", service.RequiredControls)}");
                

                AddSpacer(body);
            }
        }

        if (report.Manifest.Datastores.Count <= 0)
            return;

        AddHeading(body, "Datastores", 2);

        foreach (ManifestDatastore datastore in report.Manifest.Datastores.OrderBy(x => x.DatastoreName))
        {
            AddStyledParagraph(body, datastore.DatastoreName, "Strong");
            AddBullet(body, $"Type: {datastore.DatastoreType}");
            AddBullet(body, $"Platform: {datastore.RuntimePlatform}");
            AddBullet(body, $"Private Endpoint Required: {(datastore.PrivateEndpointRequired ? "Yes" : "No")}");
            AddBullet(body, $"Encryption At Rest Required: {(datastore.EncryptionAtRestRequired ? "Yes" : "No")}");
            AddSpacer(body);
        }
    }

    private static void AddGovernanceAndControls(Body body, ArchitectureAnalysisReport report)
    {
        AddHeading(body, "Governance and Controls", 1);

        if (report.Manifest is null)
        {
            AddStyledParagraph(body, "No manifest was available for this run.", "BodyText");
            return;
        }

        ManifestGovernance gov = report.Manifest.Governance;

        AddKeyValueTable(body, [
            ("Risk Classification", gov.RiskClassification),
            ("Cost Classification", gov.CostClassification),
            ("Required Controls", gov.RequiredControls.Count > 0 ? string.Join(", ", gov.RequiredControls) : "None"),
            ("Compliance Tags", gov.ComplianceTags.Count > 0 ? string.Join(", ", gov.ComplianceTags) : "None"),
            ("Policy Constraints", gov.PolicyConstraints.Count > 0 ? string.Join(", ", gov.PolicyConstraints) : "None")
        ]);
    }

    private static void AddExplainabilitySection(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        AddHeading(body, "Explainability and Execution Review", 1);

        if (report.ExecutionTraces.Count == 0)
        {
            AddStyledParagraph(body, "No execution traces were available for this run.", "BodyText");
            return;
        }

        AddStyledParagraph(
            body,
            "This section summarizes the agent execution path and highlights the available trace information.",
            "BodyText");

        AddBullet(body, $"Execution Trace Count: {report.ExecutionTraces.Count}");

        IOrderedEnumerable<IGrouping<AgentType, AgentExecutionTrace>> grouped = report.ExecutionTraces
            .GroupBy(x => x.AgentType)
            .OrderBy(x => x.Key);

        foreach (IGrouping<AgentType, AgentExecutionTrace> group in grouped)
        {
            AddStyledParagraph(body, group.Key.ToString(), "Strong");
            AddBullet(body, $"Trace Count: {group.Count()}");
            AddBullet(body, $"Latest Parse Success: {(group.OrderByDescending(x => x.CreatedUtc).First().ParseSucceeded ? "Succeeded" : "Failed")}");
        }

        if (report.Determinism is not null)
        {
            AddSpacer(body);
            AddStyledParagraph(body, "Determinism Snapshot", "Strong");
            AddBullet(body, $"Iterations: {report.Determinism.Iterations}");
            AddBullet(body, $"Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
        }

        if (report.ManifestDiff is null && report.AgentResultDiff is null)
            return;

        AddSpacer(body);
        AddCallout(body, "Comparison artifacts were included in this report. See Appendix C for detail.", options);
    }

    private static void AddConclusions(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        AddHeading(body, "Conclusions", 1);

        AddStyledParagraph(body, options.ConclusionsText, "BodyText");

        if (report.Warnings.Count <= 0)
            return;

        AddSpacer(body);
        AddCallout(body, "Open warnings remain and should be resolved or explicitly accepted.", options);
    }

    private static void AddAppendices(
        Body body,
        ArchitectureAnalysisReport report,
        ConsultingDocxTemplateOptions options)
    {
        if (options.IncludeAppendixMermaid)
        {
            AddHeading(body, "Appendix A. Mermaid Source", 1);

            if (!string.IsNullOrWhiteSpace(report.Diagram))
            
                AddCodeBlock(body, report.Diagram, MermaidLanguage);
            
            else
            
                AddStyledParagraph(body, "No Mermaid diagram source was available.", "BodyText");
            

            AddPageBreak(body);
        }

        if (options.IncludeAppendixExecutionTraceIndex)
        {
            AddHeading(body, "Appendix B. Execution Trace Index", 1);

            if (report.ExecutionTraces.Count > 0)
            
                foreach (AgentExecutionTrace trace in report.ExecutionTraces.OrderBy(x => x.AgentType).ThenBy(x => x.CreatedUtc))
                
                    AddBullet(body,
                        $"{trace.AgentType} | Task {trace.TaskId} | Parse {(trace.ParseSucceeded ? "Succeeded" : "Failed")} | {trace.CreatedUtc:O}");
                
            
            else
            
                AddStyledParagraph(body, "No execution traces were available.", "BodyText");
            

            AddPageBreak(body);
        }

        if (!options.IncludeAppendixDeterminismAndComparison)
            return;

        AddHeading(body, "Appendix C. Determinism and Comparison", 1);

        if (report.Determinism is not null)
        {
            AddStyledParagraph(body, "Determinism", "Strong");
            AddBullet(body, $"Iterations: {report.Determinism.Iterations}");
            AddBullet(body, $"Is Deterministic: {(report.Determinism.IsDeterministic ? "Yes" : "No")}");
        }

        if (report.ManifestDiff is not null)
        {
            AddSpacer(body);
            AddStyledParagraph(body, "Manifest Diff", "Strong");
            AddBullet(body, $"Added Services: {report.ManifestDiff.AddedServices.Count}");
            AddBullet(body, $"Removed Services: {report.ManifestDiff.RemovedServices.Count}");
            AddBullet(body, $"Added Required Controls: {report.ManifestDiff.AddedRequiredControls.Count}");
            AddBullet(body, $"Removed Required Controls: {report.ManifestDiff.RemovedRequiredControls.Count}");
        }

        if (report.AgentResultDiff is null)
            return;

        AddSpacer(body);
        AddStyledParagraph(body, "Agent Result Diff", "Strong");
        AddBullet(body, $"Agent Delta Count: {report.AgentResultDiff.AgentDeltas.Count}");
    }

    private static void AddStylesPart(
        MainDocumentPart mainPart,
        ConsultingDocxTemplateOptions options)
    {
        StyleDefinitionsPart stylePart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        stylePart.Styles = new Styles(
            BuildParagraphStyle("Title", "Title", options.PrimaryColorHex, "36"),
            BuildParagraphStyle("Subtitle", "Subtitle", options.SecondaryColorHex, "24"),
            BuildParagraphStyle("Strong", "Strong", options.BodyColorHex, "22", bold: true),
            BuildParagraphStyle("Subtle", "Subtle", options.SubtleColorHex, "18"),
            BuildParagraphStyle("BodyText", "BodyText", options.BodyColorHex, "22")
        );
        stylePart.Styles.Save();
    }

    private static Style BuildParagraphStyle(
        string styleId,
        string styleName,
        string colorHex,
        string fontSizeHalfPoints,
        bool bold = false)
    {
        Style style = new()
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
            CustomStyle = true
        };

        style.Append(new StyleName { Val = styleName });
        style.Append(new BasedOn { Val = "Normal" });
        style.Append(new UIPriority { Val = 1 });
        style.Append(new PrimaryStyle());

        StyleRunProperties runProps = new(
            new Color { Val = colorHex },
            new FontSize { Val = fontSizeHalfPoints });

        if (bold)
        
            runProps.Append(new Bold());
        

        style.Append(new StyleParagraphProperties(
            new SpacingBetweenLines
            {
                Before = "120",
                After = "120",
                Line = "300",
                LineRule = LineSpacingRuleValues.Auto
            }));

        style.Append(runProps);

        return style;
    }

    private static void AddHeading(Body body, string text, int level)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{level}" }),
            new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddStyledParagraph(Body body, string text, string styleId)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new ParagraphStyleId { Val = styleId }),
            new WpRun(new WpText(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddBullet(Body body, string text)
    {
        body.AppendChild(new WpParagraph(
            new WpParagraphProperties(
                new SpacingBetweenLines { After = "40" }),
            new WpRun(new WpText($"• {text}") { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddSpacer(Body body, int count = 1)
    {
        for (int i = 0; i < count; i++)
        
            body.AppendChild(new WpParagraph(new WpRun(new WpText(string.Empty))));
        
    }

    private static void AddPageBreak(Body body)
    {
        body.AppendChild(new WpParagraph(
            new WpRun(new WpBreak { Type = BreakValues.Page })));
    }

    private static void AddCallout(Body body, string text, ConsultingDocxTemplateOptions options)
    {
        WpParagraph paragraph = new(
            new WpParagraphProperties(
                new WpShading
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = options.AccentFillHex
                },
                new WpSpacingBetweenLines
                {
                    Before = "120",
                    After = "120",
                    Line = "280",
                    LineRule = LineSpacingRuleValues.Auto
                }),
            new WpRun(
                new WpRunProperties(new Bold(), new Color { Val = options.SecondaryColorHex }),
                new WpText(text) { Space = SpaceProcessingModeValues.Preserve }));

        body.AppendChild(paragraph);
    }

    private static void AddCodeBlock(Body body, string text, string language)
    {
        AddStyledParagraph(body, $"[{language}]", "Subtle");

        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            WpRun run = new(new WpText(line) { Space = SpaceProcessingModeValues.Preserve })
            {
                RunProperties = new WpRunProperties(
                    new RunFonts { Ascii = "Consolas" },
                    new FontSize { Val = "18" })
            };

            body.AppendChild(new WpParagraph(
                new WpParagraphProperties(
                    new WpShading
                    {
                        Val = ShadingPatternValues.Clear,
                        Fill = "F4F6F6"
                    }),
                run));
        }
    }

    private static void AddKeyValueTable(Body body, IEnumerable<(string Key, string Value)> rows)
    {
        WpTable table = new();

        WpTableProperties props = new(
            new TableBorders(
                new WpTopBorder { Val = BorderValues.Single, Size = 8 },
                new WpBottomBorder { Val = BorderValues.Single, Size = 8 },
                new WpLeftBorder { Val = BorderValues.Single, Size = 8 },
                new WpRightBorder { Val = BorderValues.Single, Size = 8 },
                new WpInsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new WpInsideVerticalBorder { Val = BorderValues.Single, Size = 6 }),
            new TableWidth { Width = "9000", Type = TableWidthUnitValues.Dxa });

        table.AppendChild(props);

        foreach ((string key, string value) in rows)
        {
            WpTableRow tr = new();

            tr.Append(
                BuildCell(key, bold: true, width: "2800"),
                BuildCell(value, bold: false, width: "6200"));

            table.Append(tr);
        }

        body.Append(table);
        AddSpacer(body);
    }

    private static WpTableCell BuildCell(string text, bool bold, string width)
    {
        WpRun run = new(new WpText(text) { Space = SpaceProcessingModeValues.Preserve });

        if (bold)
        
            run.RunProperties = new WpRunProperties(new Bold());
        

        return new WpTableCell(
            new WpTableCellProperties(
                new WpTableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width }),
            new WpParagraph(
                new WpParagraphProperties(
                    new WpSpacingBetweenLines { Before = "80", After = "80" }),
                run));
    }

    private static void AddImageToBody(
        MainDocumentPart mainPart,
        Body body,
        byte[] imageBytes,
        string imageName,
        long widthEmus,
        long heightEmus)
    {
        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);

        using (MemoryStream stream = new(imageBytes))
        
            imagePart.FeedData(stream);
        

        string relationshipId = mainPart.GetIdOfPart(imagePart);

        Drawing drawing = new(
            new Inline(
                new Extent { Cx = widthEmus, Cy = heightEmus },
                new EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DocProperties
                {
                    Id = 1U,
                    Name = imageName
                },
                new WpNonVisualGraphicFrameDrawingProperties(
                    new DrGraphicFrameLocks { NoChangeAspect = true }),
                new Graphic(
                    new GraphicData(
                        new DrPicture(
                            new DrNonVisualPictureProperties(
                                new DrNonVisualDrawingProperties
                                {
                                    Id = 0U,
                                    Name = imageName
                                },
                                new DrNonVisualPictureDrawingProperties()),
                            new DrBlipFill(
                                new DrBlip { Embed = relationshipId },
                                new DrStretch(new DrFillRectangle())),
                            new DrShapeProperties(
                                new Transform2D(
                                    new Offset { X = 0L, Y = 0L },
                                    new Extents { Cx = widthEmus, Cy = heightEmus }),
                                new PresetGeometry(new AdjustValueList())
                                {
                                    Preset = ShapeTypeValues.Rectangle
                                }))
                    )
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                    }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });

        body.AppendChild(new WpParagraph(new WpRun(drawing)));
    }
}

