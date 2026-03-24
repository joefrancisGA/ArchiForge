using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application.Diagrams;

public sealed class MermaidCliDiagramImageRenderer(
    ILogger<MermaidCliDiagramImageRenderer> logger) : IDiagramImageRenderer
{
    public async Task<byte[]?> RenderMermaidPngAsync(
        string mermaidDiagram,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mermaidDiagram))
        {
            return null;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "archiforge-mermaid", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var inputPath = Path.Combine(tempDir, "diagram.mmd");
        var outputPath = Path.Combine(tempDir, "diagram.png");

        await File.WriteAllTextAsync(inputPath, mermaidDiagram, Encoding.UTF8, cancellationToken);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "mmdc",
                Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -b transparent",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = psi;
            process.Start();

            await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Mermaid CLI failed with exit code {process.ExitCode}. STDERR: {stdErr}");
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("Mermaid CLI did not produce an output PNG.");
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temporary Mermaid work directory '{TempDir}'.", tempDir);
            }
        }
    }
}
