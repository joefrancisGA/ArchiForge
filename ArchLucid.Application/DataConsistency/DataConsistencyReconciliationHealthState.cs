namespace ArchLucid.Application.DataConsistency;

/// <summary>Published by <see cref="DataConsistencyReconciliationHostedService"/> after each reconciliation attempt.</summary>
public sealed class DataConsistencyReconciliationHealthState
{
    private readonly Lock _sync = new();

    private bool _hasCompletedRun;

    private DataConsistencyReport? _lastReport;

    private string? _lastErrorMessage;

    public void RecordSuccess(DataConsistencyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        lock (_sync)
        {
            _hasCompletedRun = true;
            _lastReport = report;
            _lastErrorMessage = null;
        }
    }

    public void RecordFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_sync)
        {
            _hasCompletedRun = true;
            _lastReport = null;
            _lastErrorMessage = exception.Message;
        }
    }

    public bool TrySnapshot(out bool hasCompletedRun, out DataConsistencyReport? lastReport, out string? lastErrorMessage)
    {
        lock (_sync)
        {
            hasCompletedRun = _hasCompletedRun;
            lastReport = _lastReport;
            lastErrorMessage = _lastErrorMessage;

            return true;
        }
    }
}
