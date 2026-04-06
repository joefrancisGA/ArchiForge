namespace ArchiForge.Api;

public static class ApiPaging
{
    public static bool TryParseUtcTicksIdCursor(
        string? cursor,
        out DateTime? createdUtc,
        out string? id,
        out string? error)
    {
        createdUtc = null;
        id = null;
        error = null;

        if (string.IsNullOrWhiteSpace(cursor))
        
            return true;
        

        string[] parts = cursor.Split(':', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out long ticks) || string.IsNullOrWhiteSpace(parts[1]))
        {
            error = "cursor must be formatted as '<utcTicks>:<comparisonRecordId>'.";
            return false;
        }

        createdUtc = new DateTime(ticks, DateTimeKind.Utc);
        id = parts[1];
        return true;
    }
}

