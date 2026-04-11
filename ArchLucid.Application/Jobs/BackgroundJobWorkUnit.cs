using System.Text.Json.Serialization;

namespace ArchLucid.Application.Jobs;

/// <summary>
/// Polymorphic work description for durable background export jobs (serialized to SQL and executed on the worker).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(AnalysisReportDocxWorkUnit), "analysisReportDocx")]
[JsonDerivedType(typeof(ConsultingDocxWorkUnit), "consultingDocx")]
public abstract record BackgroundJobWorkUnit;

/// <summary>Standard analysis report exported as DOCX.</summary>
public sealed record AnalysisReportDocxWorkUnit(
    AnalysisReportDocxJobPayload Payload,
    string FileName,
    string ContentType) : BackgroundJobWorkUnit;

/// <summary>Consulting-style analysis report exported as DOCX.</summary>
public sealed record ConsultingDocxWorkUnit(
    ConsultingDocxJobPayload Payload,
    string FileName,
    string ContentType) : BackgroundJobWorkUnit;
