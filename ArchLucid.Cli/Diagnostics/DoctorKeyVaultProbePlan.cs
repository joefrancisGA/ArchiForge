namespace ArchLucid.Cli.Diagnostics;

/// <summary>
///     Parsed Key Vault probe decision for <c>archlucid doctor</c>.
/// </summary>
internal readonly struct DoctorKeyVaultProbePlan
{
    internal enum Decision
    {
        Skip,
        BadConfiguration,
        Ready
    }

    internal DoctorKeyVaultProbePlan(Decision decision, string message, Uri? vaultUri, string? optionalSecretName)
    {
        DecisionKind = decision;
        Message = message;
        VaultUri = vaultUri;
        OptionalSecretName = optionalSecretName;
    }

    internal Decision DecisionKind
    {
        get;
    }

    /// <summary>Skip or misconfiguration text; empty when <see cref="DecisionKind" /> is <see cref="Decision.Ready" />.</summary>
    internal string Message
    {
        get;
    }

    internal Uri? VaultUri
    {
        get;
    }

    /// <summary>When set, doctor performs <c>GetSecret</c> (name only — value is never printed).</summary>
    internal string? OptionalSecretName
    {
        get;
    }
}
