using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>Lightweight guard against obvious prompt-injection patterns in operator-supplied architecture requests.</summary>
public interface IRequestContentSafetyPrecheck
{
    /// <summary>Returns reasons when the request should not be dispatched to agents.</summary>
    Task<RequestContentSafetyResult> EvaluateAsync(ArchitectureRequest request,
        CancellationToken cancellationToken = default);
}
