using Microsoft.OpenApi;

namespace ArchLucid.Api.OpenApi;

/// <summary>
/// Normalizes JSON Schema shapes that confuse common C# OpenAPI generators (e.g. NJsonSchema/NSwag):
/// <c>type: [integer, string]</c> with <c>format: int32</c> becomes plain integer. The ASP.NET Core
/// OpenAPI stack can emit integer|string unions for some CLR numeric shapes; clients should treat
/// these as JSON numbers only.
/// </summary>
internal static class OpenApiCodeGenFriendlySchemaMutator
{
    internal static void Apply(OpenApiDocument document)
    {
        if (document.Components?.Schemas is null)
            return;


        foreach (IOpenApiSchema root in document.Components.Schemas.Values)

            Visit(root);

    }

    private static void Visit(IOpenApiSchema? schema)
    {
        if (schema is null)
            return;


        CollapseIntegerStringUnion(schema);

        if (schema.Properties is not null)

            foreach (IOpenApiSchema propertySchema in schema.Properties.Values)

                Visit(propertySchema);



        Visit(schema.Items);
        VisitList(schema.AllOf);
        VisitList(schema.OneOf);
        VisitList(schema.AnyOf);
        Visit(schema.Not);
        Visit(schema.AdditionalProperties);
    }

    private static void VisitList(IList<IOpenApiSchema>? list)
    {
        if (list is null)
            return;


        foreach (IOpenApiSchema item in list)

            Visit(item);

    }

    private static void CollapseIntegerStringUnion(IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema mutable)
            return;


        if (!mutable.Type.HasValue)
            return;


        JsonSchemaType value = mutable.Type.Value;
        JsonSchemaType withoutNull = value & ~JsonSchemaType.Null;

        bool hasInteger = withoutNull.HasFlag(JsonSchemaType.Integer);
        bool hasString = withoutNull.HasFlag(JsonSchemaType.String);

        if (!hasInteger || !hasString)
            return;


        if (!string.Equals(mutable.Format, "int32", StringComparison.OrdinalIgnoreCase))
            return;


        JsonSchemaType next = JsonSchemaType.Integer;

        if (value.HasFlag(JsonSchemaType.Null))

            next |= JsonSchemaType.Null;


        mutable.Type = next;
        mutable.Pattern = null;
    }
}
