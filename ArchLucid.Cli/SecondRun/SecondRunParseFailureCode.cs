namespace ArchLucid.Cli.SecondRun;

/// <summary>Maps parser outcomes to HTTP-shaped codes so operators and tests speak the same language.</summary>
public enum SecondRunParseFailureCode
{
    /// <summary>Parse succeeded.</summary>
    None = 0,

    /// <summary>Missing required field, invalid shape, or malformed document.</summary>
    BadRequest = 400,

    /// <summary>Input file exceeds the configured byte budget.</summary>
    PayloadTooLarge = 413,
}
