namespace ArchLucid.Decisioning.Manifest.Sections;

/// <summary>A single policy control resolved from the active policy pack(s).</summary>
public class PolicyControlItem
{
    /// <summary>Stable identifier for this control (e.g. "NIST-AC-2", "CIS-1.1").</summary>
    public string ControlId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Human-readable name of the control.</summary>
    public string ControlName
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Policy pack or framework that defines this control.</summary>
    public string PolicyPack
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Short description of what the control requires.</summary>
    public string Description
    {
        get;
        set;
    } = string.Empty;
}
