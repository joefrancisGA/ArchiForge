using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI prints static explainer text only.")]
internal static class ExplainOperatorModelCommand
{
    public static Task<int> RunAsync()
    {
        const string text = """
            ArchLucid operator model (plain language)
            ------------------------------------------

            • **Run** — one architecture review effort from structured request through execution to (optional) commit.
            • **Golden manifest** — the committed, authoritative snapshot of decisions and findings you can export and govern.
            • **Authority pipeline** — backend stages that orchestrate agent work (simulator or real mode per configuration).
            • **Audit log** — append-only record of important actions (exports, governance, alerts); supports CSV/CEF export for SIEMs.
            • **Governance gates** — optional rules that can block commit when severity thresholds are exceeded.

            Happy path: configure SQL + auth → create run → execute → commit → review artifacts in the operator UI.

            For scope boundaries (V1 vs deferred V1.1/V2 items), see docs/library/V1_SCOPE.md and docs/library/V1_DEFERRED.md.

            _From `archlucid explain-operator-model`._
            """;

        Console.WriteLine(text);

        return Task.FromResult(CliExitCode.Success);
    }
}
