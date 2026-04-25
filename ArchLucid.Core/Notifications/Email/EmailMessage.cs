namespace ArchLucid.Core.Notifications.Email;

public sealed class EmailMessageTags
{
    public Guid TenantId
    {
        get;
        init;
    }

    public string EventType
    {
        get;
        init;
    } = string.Empty;
}

public sealed class EmailMessage
{
    public required string To
    {
        get;
        init;
    }

    public required string Subject
    {
        get;
        init;
    }

    public required string HtmlBody
    {
        get;
        init;
    }

    public string? TextBody
    {
        get;
        init;
    }

    public required string IdempotencyKey
    {
        get;
        init;
    }

    public EmailMessageTags Tags
    {
        get;
        init;
    } = new();
}
