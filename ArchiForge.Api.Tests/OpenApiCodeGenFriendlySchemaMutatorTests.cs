using ArchiForge.Api.OpenApi;

using FluentAssertions;

using Microsoft.OpenApi;

namespace ArchiForge.Api.Tests;

[Trait("Suite", "Core")]
public sealed class OpenApiCodeGenFriendlySchemaMutatorTests
{
    [Fact]
    public void Collapses_integer_and_string_union_to_integer_when_format_is_int32()
    {
        OpenApiDocument document = new()
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["Sample"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer | JsonSchemaType.String,
                        Format = "int32",
                        Pattern = "^-?(?:0|[1-9]\\d*)$"
                    }
                }
            }
        };

        OpenApiCodeGenFriendlySchemaMutator.Apply(document);

        OpenApiSchema schema = (OpenApiSchema)document.Components!.Schemas["Sample"];

        schema.Type.Should().Be(JsonSchemaType.Integer);
        schema.Pattern.Should().BeNull();
    }

    [Fact]
    public void Preserves_null_when_collapsing_integer_string_union()
    {
        OpenApiDocument document = new()
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["Sample"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer | JsonSchemaType.String | JsonSchemaType.Null,
                        Format = "int32",
                        Pattern = "^-?(?:0|[1-9]\\d*)$"
                    }
                }
            }
        };

        OpenApiCodeGenFriendlySchemaMutator.Apply(document);

        OpenApiSchema schema = (OpenApiSchema)document.Components!.Schemas["Sample"];

        schema.Type.Should().Be(JsonSchemaType.Integer | JsonSchemaType.Null);
    }
}
