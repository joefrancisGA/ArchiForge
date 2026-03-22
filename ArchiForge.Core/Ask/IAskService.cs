using ArchiForge.Core.Scoping;

namespace ArchiForge.Core.Ask;

public interface IAskService
{
    Task<AskResponse> AskAsync(AskRequest request, ScopeContext scope, CancellationToken ct);
}
