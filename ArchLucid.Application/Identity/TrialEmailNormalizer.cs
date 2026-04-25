namespace ArchLucid.Application.Identity;

public static class TrialEmailNormalizer
{
    public static string Normalize(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Email is required.", nameof(email)) : email.Trim().ToUpperInvariant();
    }
}
