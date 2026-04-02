namespace ArchiForge.Api.Models.Learning;

public sealed class LearningThemesListResponse
{
    public DateTime GeneratedUtc { get; init; }

    public IReadOnlyList<LearningThemeResponse> Themes { get; init; } = [];
}
