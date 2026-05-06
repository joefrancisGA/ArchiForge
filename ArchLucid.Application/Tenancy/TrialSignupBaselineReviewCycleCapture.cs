namespace ArchLucid.Application.Tenancy;
/// <summary>Optional baseline review-cycle hours + provenance captured at self-service trial signup.</summary>
public sealed record TrialSignupBaselineReviewCycleCapture(decimal Hours, string? SourceNote, DateTimeOffset CapturedUtc)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(SourceNote);
    private static byte __ValidatePrimaryConstructorArguments(System.String? SourceNote)
    {
        return (byte)0;
    }
}