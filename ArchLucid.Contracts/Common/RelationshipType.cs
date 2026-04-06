namespace ArchiForge.Contracts.Common;

/// <summary>Describes the nature of a directed relationship between two topology entities.</summary>
public enum RelationshipType
{
    /// <summary>Source service makes synchronous calls to the target.</summary>
    Calls = 1,
    /// <summary>Source reads data from the target store.</summary>
    ReadsFrom = 2,
    /// <summary>Source writes data to the target store.</summary>
    WritesTo = 3,
    /// <summary>Source publishes messages or events to the target.</summary>
    PublishesTo = 4,
    /// <summary>Source subscribes to messages or events from the target.</summary>
    SubscribesTo = 5,
    /// <summary>Source uses the target for authentication or token validation.</summary>
    AuthenticatesWith = 6
}
