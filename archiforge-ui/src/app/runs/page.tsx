import type { Metadata } from "next";
import Link from "next/link";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorMalformedCallout,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { coerceRunSummaryList } from "@/lib/operator-response-guards";
import { listRunsByProject } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export const metadata: Metadata = {
  title: "Runs",
};

/** Server-rendered run list page. Fetches runs for a project and validates via coerceRunSummaryList. */
export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; take?: string }>;
}) {
  const resolved = await searchParams;
  const projectId = resolved.projectId ?? "default";
  const take = Number(resolved.take ?? "20");

  let runs: RunSummary[] = [];
  let loadFailure: ApiLoadFailureState | null = null;
  let malformedMessage: string | null = null;

  try {
    const raw: unknown = await listRunsByProject(projectId, take);
    const coerced = coerceRunSummaryList(raw);

    if (!coerced.ok) {
      malformedMessage = coerced.message;
      runs = [];
    } else {
      runs = coerced.items;
    }
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  return (
    <main>
      <h2>Runs</h2>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.5 }}>
        Project <strong>{projectId}</strong>. Open a run for manifest summary, artifact review, compare and
        replay links, and exports.
      </p>
      <p style={{ marginTop: 8 }}>
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/graph">Graph</Link>
        {" · "}
        <Link href="/compare">Compare runs</Link>
      </p>

      {loadFailure && (
        <>
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
          <p style={{ margin: "12px 0 0", fontSize: 14 }}>
            Check that the API is running, <code>ARCHIFORGE_API_BASE_URL</code> / proxy is correct, and
            credentials (if any) are set for server-side fetches.
          </p>
        </>
      )}

      {!loadFailure && malformedMessage && (
        <OperatorMalformedCallout>
          <strong>Runs list response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{malformedMessage}</p>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            The HTTP call may have succeeded, but the JSON did not match the expected run summary
            list. This is distinct from an empty project (zero runs).
          </p>
        </OperatorMalformedCallout>
      )}

      {loadFailure === null && !malformedMessage && runs.length === 0 && (
        <OperatorEmptyState title="No runs in this project">
          <p style={{ margin: 0 }}>
            There are no runs for this project and query yet (valid empty result). Create a run via
            the API or CLI, then refresh. See <code>docs/CLI_USAGE.md</code>.{" "}
            <Link href="/">Back to home</Link>.
          </p>
        </OperatorEmptyState>
      )}

      {!loadFailure && !malformedMessage && runs.length > 0 && (
        <table style={{ borderCollapse: "collapse", width: "100%", marginTop: 16 }}>
          <thead>
            <tr>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ccc", padding: 8 }}>Run ID</th>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ccc", padding: 8 }}>
                Description
              </th>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ccc", padding: 8 }}>Created</th>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ccc", padding: 8 }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {runs.map((run) => (
              <tr key={run.runId}>
                <td style={{ padding: 8, fontFamily: "monospace", fontSize: 13 }}>{run.runId}</td>
                <td style={{ padding: 8 }}>{run.description ?? ""}</td>
                <td style={{ padding: 8 }}>{new Date(run.createdUtc).toLocaleString()}</td>
                <td style={{ padding: 8 }}>
                  <Link href={`/runs/${run.runId}`}>Open run</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
