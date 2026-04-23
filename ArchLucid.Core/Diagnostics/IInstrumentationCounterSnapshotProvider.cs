namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Returns a point-in-time copy of cumulative ArchLucid counter measurements (see
///     <see cref="InstrumentationCounterSnapshot" />). Implementations are expected to be safe for repeated
///     concurrent calls; callers should treat the returned snapshot as immutable.
/// </summary>
public interface IInstrumentationCounterSnapshotProvider
{
    /// <summary>Returns cumulative counter values observed since the process started.</summary>
    InstrumentationCounterSnapshot GetSnapshot();
}
