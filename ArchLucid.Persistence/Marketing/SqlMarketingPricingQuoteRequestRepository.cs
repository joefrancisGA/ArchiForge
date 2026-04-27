using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.Marketing;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Marketing;

/// <inheritdoc />
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository.")]
public sealed class SqlMarketingPricingQuoteRequestRepository(ISqlConnectionFactory connectionFactory)
    : IMarketingPricingQuoteRequestRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<MarketingPricingQuoteRequestInsertResult?> AppendAsync(
        string workEmail,
        string companyName,
        string tierInterest,
        string message,
        byte[]? clientIpSha256,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workEmail))
            throw new ArgumentException("Work email is required.", nameof(workEmail));
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));
        if (string.IsNullOrWhiteSpace(tierInterest))
            throw new ArgumentException("Tier interest is required.", nameof(tierInterest));
        if (message is null) throw new ArgumentNullException(nameof(message));

        const string sql = """
                           INSERT INTO dbo.MarketingPricingQuoteRequests (WorkEmail, CompanyName, TierInterest, Message, ClientIpHash)
                           OUTPUT INSERTED.Id, INSERTED.CreatedUtc
                           VALUES (@WorkEmail, @CompanyName, @TierInterest, @Message, @ClientIpHash);
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        CommandDefinition cmd = new(
            sql,
            new
            {
                WorkEmail = workEmail.Trim(),
                CompanyName = companyName.Trim(),
                TierInterest = tierInterest.Trim(),
                Message = message.Trim(),
                ClientIpHash = clientIpSha256
            },
            cancellationToken: cancellationToken);

        (Guid Id, DateTime CreatedUtc) row =
            await connection.QuerySingleAsync<(Guid Id, DateTime CreatedUtc)>(cmd).ConfigureAwait(false);

        return new MarketingPricingQuoteRequestInsertResult(row.Id, row.CreatedUtc);
    }
}
