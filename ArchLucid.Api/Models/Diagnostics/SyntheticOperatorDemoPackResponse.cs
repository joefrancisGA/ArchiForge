namespace ArchLucid.Api.Models.Diagnostics;

/// <summary>Result for <c>POST /v1/diagnostics/synthetic-operator-demo-pack</c>.</summary>
public sealed class SyntheticOperatorDemoPackResponse
{
    public int AuditEventsWritten
    {
        get;
        init;
    }
}
