using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Serves static developer-facing HTML documentation pages (excluded from OpenAPI/Swagger).
/// </summary>
/// <remarks>
/// Intentionally hidden from the API explorer via <c>IgnoreApi = true</c>.
/// Marked <see cref="AllowAnonymousAttribute"/> because these are read-only recipe pages
/// that do not expose sensitive data or mutate state.
/// </remarks>
[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class DocsController : ControllerBase
{
    /// <summary>Returns an HTML page with step-by-step comparison replay recipes.</summary>
    [HttpGet("replay-recipes")]
    [Produces("text/html")]
    public IActionResult ReplayRecipes()
    {
        string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        string swaggerUrl = $"{baseUrl}/swagger";
        string html = """
                      <!DOCTYPE html>
                      <html lang="en">
                      <head>
                        <meta charset="utf-8" />
                        <meta name="viewport" content="width=device-width, initial-scale=1" />
                        <title>ArchiForge comparison replay recipes</title>
                        <style>
                          body {{ font-family: sans-serif; max-width: 800px; margin: 1rem auto; padding: 0 1rem; line-height: 1.5; }}
                          h1 {{ font-size: 1.5rem; }}
                          h2 {{ font-size: 1.2rem; margin-top: 1.5rem; }}
                          code {{ background: #f0f0f0; padding: 0.2em 0.4em; border-radius: 3px; }}
                          pre {{ background: #f5f5f5; padding: 1rem; overflow-x: auto; border-radius: 4px; }}
                          a {{ color: #0066cc; }}
                          ol {{ padding-left: 1.5rem; }}
                          li {{ margin-bottom: 0.5rem; }}
                        </style>
                      </head>
                      <body>
                        <h1>ArchiForge comparison replay recipes</h1>
                        <p>Step-by-step flow to list comparisons, replay as a file, and export a drift report. Use <a href="{{SWAGGER_URL}}">Swagger UI</a> to try the API interactively.</p>

                        <h2>1. List comparison records</h2>
                        <p><strong>GET</strong> <code>/v1/architecture/comparisons</code> with optional query params: <code>comparisonType</code>, <code>leftRunId</code>, <code>rightRunId</code>, <code>limit</code>.</p>
                        <pre>curl -s "{{BASE_URL}}/v1/architecture/comparisons?limit=10"</pre>

                        <h2>2. Get a single comparison record (metadata)</h2>
                        <p><strong>GET</strong> <code>/v1/architecture/comparisons/{{comparisonRecordId}}</code></p>
                        <pre>curl -s "{{BASE_URL}}/v1/architecture/comparisons/YOUR_RECORD_ID"</pre>

                        <h2>3. Replay as file (Markdown, DOCX, etc.)</h2>
                        <p><strong>POST</strong> <code>/v1/architecture/comparisons/{{comparisonRecordId}}/replay</code> with JSON body: <code>format</code>, <code>replayMode</code> (artifact | regenerate | verify), <code>profile</code>, <code>persistReplay</code>.</p>
                        <pre>curl -X POST "{{BASE_URL}}/v1/architecture/comparisons/YOUR_RECORD_ID/replay" \\
                        -H "Content-Type: application/json" \\
                        -o comparison.md \\
                        -d '{{"format":"markdown","replayMode":"artifact","persistReplay":false}}'</pre>

                        <h2>4. Replay metadata only (no file body)</h2>
                        <p><strong>POST</strong> <code>/v1/architecture/comparisons/{{comparisonRecordId}}/replay/metadata</code> — same body as replay; returns JSON with recordId, type, format, fileName.</p>

                        <h2>5. Export drift report (verify-style diff)</h2>
                        <p><strong>GET</strong> <code>/v1/architecture/comparisons/{{comparisonRecordId}}/drift-report?format=markdown|html|docx</code> — compares stored vs regenerated and returns a report file.</p>
                        <pre>curl -s "{{BASE_URL}}/v1/architecture/comparisons/YOUR_RECORD_ID/drift-report?format=markdown" -o drift.md</pre>

                        <p><a href="{{SWAGGER_URL}}">Open Swagger UI</a></p>
                      </body>
                      </html>
                      """;
        html = html.Replace("{{BASE_URL}}", baseUrl).Replace("{{SWAGGER_URL}}", swaggerUrl);
        return Content(html, "text/html");
    }
}
