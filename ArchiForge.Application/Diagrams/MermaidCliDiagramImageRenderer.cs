using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Extensions.Logging;

namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Renders a Mermaid diagram to a PNG image by invoking the <c>mmdc</c> Mermaid CLI tool.
/// Writes the diagram to a temporary file, runs <c>mmdc</c>, reads the output PNG, and cleans up.
/// Register <see cref="NullDiagramImageRenderer"/> in environments where <c>mmdc</c> is not installed.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires external mmdc CLI tool installed on the host; tested manually.")]
public sealed class MermaidCliDiagramImageRenderer(
    ILogger<MermaidCliDiagramImageRenderer> logger) : IDiagramImageRenderer
{
    /// <summary>Maximum time to wait for the <c>mmdc</c> process before cancelling and throwing.</summary>
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<byte[]?> RenderMermaidPngAsync(
        string mermaidDiagram,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mermaidDiagram))
        {
            return null;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "archiforge-mermaid", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        string inputPath = Path.Combine(tempDir, "diagram.mmd");
        string outputPath = Path.Combine(tempDir, "diagram.png");

        await File.WriteAllTextAsync(inputPath, mermaidDiagram, Encoding.UTF8, cancellationToken);

        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "mmdc",
                Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -b transparent",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using CancellationTokenSource timeoutCts = new(ProcessTimeout);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using Process process = new();
            process.StartInfo = psi;
            process.Start();

            await process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            string stdErr = await process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

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
