using System.Text;
using System.Text.Json;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application.Import;
public static class JsonRequestDeserializer
{
    public static ArchitectureRequest DeserializeUtf8(ReadOnlySpan<byte> utf8)
    {
        ArchitectureRequest? req = JsonSerializer.Deserialize<ArchitectureRequest>(utf8, ImportArchitectureRequestSerializerOptions.StrictDeserialize);
        return req ?? throw new JsonException("Imported JSON deserialized to null.");
    }

    public static ArchitectureRequest DeserializeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text is null ? throw new ArgumentNullException(nameof(text)) : DeserializeUtf8(Encoding.UTF8.GetBytes(text));
    }
}