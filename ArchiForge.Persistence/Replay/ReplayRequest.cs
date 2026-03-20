namespace ArchiForge.Persistence.Replay;

public class ReplayRequest
{
    public Guid RunId { get; set; }
    public string Mode { get; set; } = ReplayMode.ReconstructOnly;
}
