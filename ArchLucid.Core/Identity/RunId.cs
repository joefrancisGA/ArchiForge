using System.Text.Json.Serialization;

namespace ArchLucid.Core.Identity;

/// <summary>
///     Strongly-typed identifier for authority runs, wrapping a <see cref="Guid" />.
///     Use this at boundaries to avoid confusing a run id with other <see cref="Guid" /> keys (tenant, workspace,
///     snapshot).
/// </summary>
/// <remarks>
///     Full migration from raw <see cref="Guid" /> on run records and HTTP APIs is incremental; adopt this type
///     module-by-module.
/// </remarks>
[JsonConverter(typeof(RunIdJsonConverter))]
public readonly record struct RunId(Guid Value) : IComparable<RunId>
{
    public static RunId Empty => new(Guid.Empty);

    public int CompareTo(RunId other)
    {
        return Value.CompareTo(other.Value);
    }

    public static RunId New()
    {
        return new RunId(Guid.NewGuid());
    }

    public static implicit operator Guid(RunId id)
    {
        return id.Value;
    }

    public static explicit operator RunId(Guid guid)
    {
        return new RunId(guid);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
