namespace ArchLucid.Cli.Commands;

/// <summary>Parsed arguments for <c>archlucid roi-bulletin</c>.</summary>
public sealed class RoiBulletinCommandOptions
{
    public const int DefaultMinTenants = 5;

    public string Quarter
    {
        get; init;
    } = string.Empty;

    public int MinTenants
    {
        get; init;
    } = DefaultMinTenants;

    public string? OutPath
    {
        get; init;
    }

    public static RoiBulletinCommandOptions? Parse(string[] args, out string? error)
    {
        if (args is null) throw new ArgumentNullException(nameof(args));

        string? quarter = null;
        int minTenants = DefaultMinTenants;
        string? outPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--quarter":
                    if (!TryReadValue(args, ref i, arg, out string? q, out error)) return null;
                    quarter = q;
                    break;

                case "--min-tenants":
                    if (!TryReadValue(args, ref i, arg, out string? m, out error)) return null;
                    if (!int.TryParse(m, out int parsed) || parsed < 1)
                    {
                        error = "Invalid value for --min-tenants. Expected a positive integer.";
                        return null;
                    }
                    minTenants = parsed;
                    break;

                case "--out":
                    if (!TryReadValue(args, ref i, arg, out string? o, out error)) return null;
                    outPath = o;
                    break;

                default:
                    error = $"Unknown flag: {arg}. Try `archlucid roi-bulletin --help`.";
                    return null;
            }
        }

        if (string.IsNullOrWhiteSpace(quarter))
        {
            error = "Missing --quarter (format Q1-YYYY … Q4-YYYY).";
            return null;
        }

        error = null;

        return new RoiBulletinCommandOptions
        {
            Quarter = quarter.Trim(),
            MinTenants = minTenants,
            OutPath = outPath,
        };
    }

    private static bool TryReadValue(string[] args, ref int i, string flag, out string? value, out string? error)
    {
        if (i + 1 >= args.Length)
        {
            value = null;
            error = $"Missing value for {flag}.";
            return false;
        }

        i++;
        value = args[i];
        error = null;
        return true;
    }
}
