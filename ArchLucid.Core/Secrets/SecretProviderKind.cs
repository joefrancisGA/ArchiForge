namespace ArchLucid.Core.Secrets;

/// <summary>Selects how optional secrets (Key Vault vs environment) are resolved.</summary>
public enum SecretProviderKind
{
    EnvironmentVariable = 0,

    KeyVault = 1
}
