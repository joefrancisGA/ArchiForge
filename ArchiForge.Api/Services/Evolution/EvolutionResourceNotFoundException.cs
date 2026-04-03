namespace ArchiForge.Api.Services.Evolution;

/// <summary>Raised when a 60R evolution resource is missing for the current scope (maps to 404 problem details).</summary>
public sealed class EvolutionResourceNotFoundException : Exception
{
    public EvolutionResourceNotFoundException(string problemTypeUri, string message)
        : base(message)
    {
        ProblemTypeUri = problemTypeUri;
    }

    public string ProblemTypeUri { get; }
}
