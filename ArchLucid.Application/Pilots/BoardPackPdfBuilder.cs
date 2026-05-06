using System.Globalization;
using System.Text;
using ArchLucid.Application.ExecDigest;
using ArchLucid.Application.Value;
using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Scoping;
using Microsoft.Extensions.Options;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPdfDocument = QuestPDF.Fluent.Document;

namespace ArchLucid.Application.Pilots;
/// <summary>
///     Quarterly sponsor board pack — reuses <see cref = "ExecDigestComposer"/> and <see cref = "ValueReportBuilder"/>
///     without duplicating ROI math.
/// </summary>
public sealed class BoardPackPdfBuilder(IExecDigestComposer execDigestComposer, ValueReportBuilder valueReportBuilder, IScopeContextProvider scopeProvider, IOptionsMonitor<EmailNotificationOptions> emailOptionsMonitor)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(execDigestComposer, valueReportBuilder, scopeProvider, emailOptionsMonitor);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.ExecDigest.IExecDigestComposer execDigestComposer, ArchLucid.Application.Value.ValueReportBuilder valueReportBuilder, ArchLucid.Core.Scoping.IScopeContextProvider scopeProvider, Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Core.Configuration.EmailNotificationOptions> emailOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(execDigestComposer);
        ArgumentNullException.ThrowIfNull(valueReportBuilder);
        ArgumentNullException.ThrowIfNull(scopeProvider);
        ArgumentNullException.ThrowIfNull(emailOptionsMonitor);
        return (byte)0;
    }

    private readonly IOptionsMonitor<EmailNotificationOptions> _emailOptionsMonitor = emailOptionsMonitor ?? throw new ArgumentNullException(nameof(emailOptionsMonitor));
    private readonly IExecDigestComposer _execDigestComposer = execDigestComposer ?? throw new ArgumentNullException(nameof(execDigestComposer));
    private readonly IScopeContextProvider _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    private readonly ValueReportBuilder _valueReportBuilder = valueReportBuilder ?? throw new ArgumentNullException(nameof(valueReportBuilder));
    /// <summary>Builds a PDF for the current tenant scope and requested quarter (UTC).</summary>
    public async Task<byte[]> BuildPdfAsync(int year, int quarter, DateTimeOffset? overrideStartUtc, DateTimeOffset? overrideEndUtc, string operatorBaseUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operatorBaseUrl);
        ScopeContext scope = _scopeProvider.GetCurrentScope();
        (DateTimeOffset qStart, DateTimeOffset qEnd) = BoardPackQuarterWindow.Resolve(year, quarter, overrideStartUtc, overrideEndUtc);
        (DateTime digestStart, DateTime digestEnd) = BoardPackQuarterWindow.DigestWeekInsideQuarter(qStart, qEnd);
        string baseUrl = string.IsNullOrWhiteSpace(operatorBaseUrl) ? "http://localhost:3000" : operatorBaseUrl.Trim().TrimEnd('/');
        EmailNotificationOptions emailOptions = _emailOptionsMonitor.CurrentValue;
        string operatorBase = string.IsNullOrWhiteSpace(emailOptions.OperatorBaseUrl) ? baseUrl : emailOptions.OperatorBaseUrl.Trim();
        ExecDigestComposition digest = await _execDigestComposer.ComposeAsync(scope.TenantId, digestStart, digestEnd, scope, operatorBase, cancellationToken);
        string digestMd = ExecDigestCompositionMarkdownFormatter.Format(digest);
        ValueReportSnapshot value = await _valueReportBuilder.BuildAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, qStart, qEnd, cancellationToken);
        string valueMd = ValueReportSnapshotMarkdownFormatter.Format(value);
        StringBuilder combined = new();
        combined.AppendLine($"# ArchLucid board pack — Q{quarter.ToString(CultureInfo.InvariantCulture)} {year.ToString(CultureInfo.InvariantCulture)} (UTC)");
        combined.AppendLine();
        combined.AppendLine("This pack combines the **weekly executive digest pipeline** (one representative ISO week inside the quarter) with the **tenant value-report metrics** for the full quarter window. Figures come only from existing builders — no ad-hoc ROI math.");
        combined.AppendLine();
        combined.AppendLine("---");
        combined.AppendLine();
        combined.Append(digestMd);
        combined.AppendLine();
        combined.AppendLine("---");
        combined.AppendLine();
        combined.Append(valueMd);
        string markdown = combined.ToString();
        Settings.License = LicenseType.Community;
        QuestPdfDocument doc = QuestPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));
                page.Header().Text("ArchLucid — quarterly board pack").Bold().FontSize(14);
                page.Content().Column(column => MarkdownPdfRenderer.Render(column, markdown));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Tenant ");
                    text.Span(scope.TenantId.ToString("D")).Bold();
                });
            });
        });
        using MemoryStream stream = new();
        doc.GeneratePdf(stream);
        return stream.ToArray();
    }
}