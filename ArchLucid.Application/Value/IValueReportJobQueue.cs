namespace ArchLucid.Application.Value;

public interface IValueReportJobQueue
{
    Guid Enqueue(ValueReportJobRequest request);

    ValueReportJobPollResult TryPoll(Guid jobId, Guid scopedTenantId);
}
