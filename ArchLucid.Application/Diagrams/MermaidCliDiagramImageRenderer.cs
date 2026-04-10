using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using ArchLucid.Core.Diagrams;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Diagrams;

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

        string tempDir = Path.Combine(Path.GetTempPath(), "archlucid-mermaid", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDir);

            string inputPath = Path.Combine(tempDir, "diagram.mmd");
            string outputPath = Path.Combine(tempDir, "diagram.png");

            await File.WriteAllTextAsync(inputPath, mermaidDiagram, Encoding.UTF8, cancellationToken);

            ProcessStartInfo psi = new()
            {
                FileName = "mmdc",
                Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" -b transparent",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using CancellationTokenSource timeoutCts = new(ProcessTimeout);
            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using Process process = new();
            process.StartInfo = psi;
            process.Start();

            await process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            string stdErr = await process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            if (process.ExitCode != 0)
            {
                logger.LogWarning(
                    "Mermaid CLI exited with code {ExitCode}. STDERR: {StdErr}",
                    process.ExitCode,
                    stdErr);

                return null;
            }

            if (!File.Exists(outputPath))
            {
                logger.LogWarning("Mermaid CLI reported success but output PNG was missing at {OutputPath}.", outputPath);

                return null;
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Mermaid CLI diagram render failed; callers should fall back to Mermaid source text.");

            return null;
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
