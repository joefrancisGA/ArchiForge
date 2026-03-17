namespace ArchiForge.Api.Services;

public interface IReplayDiagnosticsRecorder
{
    void Record(ReplayDiagnosticsEntry entry);

    IReadOnlyList<ReplayDiagnosticsEntry> GetRecent(int maxCount = 100);
}
