namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class HostLeaderElectionRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        bool enabled = configuration.GetValue("HostLeaderElection:Enabled", true);

        if (!enabled)
            return;

        int leaseSeconds = configuration.GetValue("HostLeaderElection:LeaseDurationSeconds", 90);

        if (leaseSeconds is < 15 or > 3600)

            errors.Add(
                "HostLeaderElection:LeaseDurationSeconds must be between 15 and 3600 inclusive when HostLeaderElection:Enabled is true.");

        int renewSeconds = configuration.GetValue("HostLeaderElection:RenewIntervalSeconds", 25);

        if (renewSeconds < 5)

            errors.Add(
                "HostLeaderElection:RenewIntervalSeconds must be at least 5 when HostLeaderElection:Enabled is true.");

        if (renewSeconds >= leaseSeconds)

            errors.Add(
                "HostLeaderElection:RenewIntervalSeconds must be less than HostLeaderElection:LeaseDurationSeconds when HostLeaderElection:Enabled is true.");

        int followerMs = configuration.GetValue("HostLeaderElection:FollowerPollMilliseconds", 2000);

        if (followerMs is < 100 or > 120_000)

            errors.Add(
                "HostLeaderElection:FollowerPollMilliseconds must be between 100 and 120000 when HostLeaderElection:Enabled is true.");
    }
}
