namespace ArchLucid.Cli.SecondRun;

/// <summary>Wire shape for <c>SECOND_RUN.json</c> (camelCase or snake_case keys via JSON options).</summary>
internal sealed class SecondRunWireDto
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public List<string>? Components { get; set; }

    public List<string>? DataStores { get; set; }

    public List<string>? PublicEndpoints { get; set; }

    public List<string>? CompliancePosture { get; set; }

    public string? Environment { get; set; }

    public string? CloudProvider { get; set; }

    public List<string>? Constraints { get; set; }

    public List<string>? Assumptions { get; set; }

    public string? RequestId { get; set; }

    public List<string>? InlineRequirements { get; set; }
}
