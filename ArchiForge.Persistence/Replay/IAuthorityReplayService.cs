namespace ArchiForge.Persistence.Replay;

public interface IAuthorityReplayService
{
    Task<ReplayResult?> ReplayAsync(
        ReplayRequest request,
        CancellationToken ct);
}
