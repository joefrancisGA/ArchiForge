using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
public sealed class ManifestValidatorTests
{
    [Fact]
    public void ValidateOrThrow_when_schema_missing_throws_FileNotFoundException()
    {
        using TempPaths temp = new();
        string manifest = Path.Combine(temp.Dir, "m.json");
        File.WriteAllText(manifest, "{}");

        Action act = () => ManifestValidator.ValidateOrThrow(Path.Combine(temp.Path, "missing.schema.json"), manifest);

        act.Should().Throw<FileNotFoundException>().WithMessage("*Schema file not found*");
    }

    [Fact]
    public void ValidateOrThrow_when_manifest_missing_throws_FileNotFoundException()
    {
        using TempDirectory temp = new();
        string schema = Path.Combine(temp.Path, "schema.json");
        File.WriteAllText(schema, MinimalObjectSchemaJson);

        Action act = () => ManifestValidator.ValidateOrThrow(schema, Path.Combine(temp.Path, "missing.manifest.json"));

        act.Should().Throw<FileNotFoundException>().WithMessage("*Manifest file not found*");
    }

    [Fact]
    public void ValidateOrThrow_when_valid_does_not_throw()
    {
        using TempDirectory temp = new();
        string schema = Path.Combine(temp.Path, "schema.json");
        File.WriteAllText(schema, MinimalObjectSchemaJson);
        string manifest = Path.Combine(temp.Path, "archlucid.json");
        File.WriteAllText(manifest, """{"id":"a"}""");

        Action act = () => ManifestValidator.ValidateOrThrow(schema, manifest);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrThrow_when_invalid_throws_InvalidDataException_with_detail()
    {
        using TempDirectory temp = new();
        string schema = Path.Combine(temp.Path, "schema.json");
        File.WriteAllText(schema, MinimalObjectSchemaJson);
        string manifest = Path.Combine(temp.Path, "archlucid.json");
        File.WriteAllText(manifest, "{}");

        Action act = () => ManifestValidator.ValidateOrThrow(schema, manifest);

        act.Should().Throw<InvalidDataException>().WithMessage("*Manifest validation failed*");
    }

    [Fact]
    public void TryValidate_when_valid_returns_true_and_empty_errors()
    {
        using TempDirectory temp = new();
        string schema = Path.Combine(temp.Path, "schema.json");
        File.WriteAllText(schema, MinimalObjectSchemaJson);

        bool ok = ManifestValidator.TryValidate(schema, """{"id":"x"}""", out string errorsJson);

        ok.Should().BeTrue();
        errorsJson.Should().Be("");
    }

    [Fact]
    public void TryValidate_when_invalid_returns_false_and_errors_json()
    {
        using TempPaths temp = new();
        string schema = Path.Combine(temp.Dir, "schema.json");
        File.WriteAllText(schema, MinimalObjectSchemaJson);

        bool ok = ManifestValidator.TryValidate(schema, "{}", out string errorsJson);

        ok.Should().BeFalse();
        errorsJson.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Minimal object schema requiring string property <c>id</c> (JsonSchema.Net).</summary>
    private const string MinimalObjectSchemaJson =
        """
        {
          "type": "object",
          "required": [ "id" ],
          "properties": {
            "id": { "type": "string" }
          }
        }
        """;

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ArchLucid.Cli.Tests." + Guid.NewGuid().ToString("N")[..8]);

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class TempPaths : IDisposable
    {
        private readonly TempDirectory _inner = new();

        public string Dir => _inner.Path;

        public void Dispose() => _inner.Dispose();
    }
}
