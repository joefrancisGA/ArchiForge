using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.ContextIngestion.Models;

using ArchLucid.TestSupport.GoldenCorpus;

namespace ArchLucid.ContextIngestion.Tests.GoldenCorpus;

internal sealed class IngestionGoldenCaseFile
{
    [JsonPropertyName("infrastructureDeclaration")]
    public IngestionInfrastructureDeclarationInput? InfrastructureDeclaration
    {
        get;
        set;
    }

    [JsonPropertyName("document")]
    public IngestionDocumentInput? Document
    {
        get;
        set;
    }
}

internal sealed class IngestionInfrastructureDeclarationInput
{
    [JsonPropertyName("name")]
    public string Name
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("format")]
    public string Format
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("declarationId")]
    public string DeclarationId
    {
        get;
        set;
    } = null!;

    /// <summary>Nested Terraform JSON (serialized to string for <see cref="InfrastructureDeclarationReference.Content" />).</summary>
    [JsonPropertyName("terraformDocument")]
    public JsonElement? TerraformDocument
    {
        get;
        set;
    }
}

internal sealed class IngestionDocumentInput
{
    [JsonPropertyName("documentId")]
    public string DocumentId
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("name")]
    public string Name
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("contentType")]
    public string ContentType
    {
        get;
        set;
    } = null!;

    [JsonPropertyName("content")]
    public string Content
    {
        get;
        set;
    } = null!;
}

internal static class IngestionGoldenCaseInputDtos
{
    public static InfrastructureDeclarationReference ToDeclaration(IngestionInfrastructureDeclarationInput i)
    {
        if (i.TerraformDocument is not { } doc)
            throw new InvalidOperationException("terraformDocument is required for infrastructure cases.");

        string content = JsonSerializer.Serialize(doc, GoldenCorpusJson.SerializerOptions);
        return new InfrastructureDeclarationReference
        {
            Name = i.Name,
            Format = i.Format,
            DeclarationId = i.DeclarationId,
            Content = content
        };
    }

    public static ContextDocumentReference ToDocument(IngestionDocumentInput d) =>
        new()
        {
            DocumentId = d.DocumentId,
            Name = d.Name,
            ContentType = d.ContentType,
            Content = d.Content
        };
}
