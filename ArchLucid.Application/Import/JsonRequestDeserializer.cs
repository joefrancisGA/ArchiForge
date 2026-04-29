using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Import;

public static class JsonRequestDeserializer
{
    public static ArchitectureRequest DeserializeUtf8(ReadOnlySpan<byte> utf8)
    {
        ArchitectureRequest? req = JsonSerializer.Deserialize<ArchitectureRequest>(
            utf8,
            ImportArchitectureRequestSerializerOptions.StrictDeserialize);

        if (req is null)
            throw new JsonException("Imported JSON deserialized to null.");

        return req;
    }

    public static ArchitectureRequest DeserializeText(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        return DeserializeUtf8(Encoding.UTF8.GetBytes(text));
    }
}
