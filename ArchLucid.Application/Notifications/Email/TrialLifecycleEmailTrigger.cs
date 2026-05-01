using System.Text.Json.Serialization;

namespace ArchLucid.Application.Notifications.Email;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrialLifecycleEmailTrigger
{
    TrialProvisioned,

    FirstRunCommitted,

    MidTrialDay7,

    ApproachingRunLimit,

    ExpiringSoon,

    Expired,

    Converted
}
