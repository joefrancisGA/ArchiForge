using System.Diagnostics.CodeAnalysis;

using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ConsultingDocxProfileRecommendationResponse
{
    public ConsultingDocxProfileRecommendation Recommendation { get; set; } = new();
}

