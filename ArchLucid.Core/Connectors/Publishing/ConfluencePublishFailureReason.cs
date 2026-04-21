namespace ArchLucid.Core.Connectors.Publishing;

public enum ConfluencePublishFailureReason
{
    Unauthorized = 1,
    Forbidden = 2,
    NotFound = 3,
    RateLimited = 4,
    ServerError = 5,
    NetworkError = 6,
    BadResponse = 7,
    PayloadTooLarge = 8
}
