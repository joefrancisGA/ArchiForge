using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Application.Governance.FindingReview;

/// <summary>
///     Application-layer façade for inserting human-review trail rows (
///     <see cref="IFindingReviewTrailRepository.AppendAsync" />)
///     paired with durable <see cref="IAuditService" /> events (finding repositories stay audit-free).
/// </summary>
public interface IFindingReviewTrailAppendService
{
    /// <inheritdoc cref="IFindingReviewTrailRepository.AppendAsync" />
    Task AppendAsync(FindingReviewEventRecord reviewEvent, CancellationToken cancellationToken = default);
}
