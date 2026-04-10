import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorMalformedCallout,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { listRunsByProjectPaged } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export const metadata: Metadata = {
  title: "Runs list",
};

/** Server-rendered run list page. Fetches a page of runs and validates via coerceRunSummaryPaged. */
export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; page?: string; pageSize?: string; take?: string }>;
}) {
  const resolved = await searchParams;
  const projectId = resolved.projectId ?? "default";
  const page = Math.max(1, Number.parseInt(resolved.page ?? "1", 10) || 1);
  const sizeRaw = resolved.pageSize ?? resolved.take ?? "20";
  const pageSize = Math.min(200, Math.max(1, Number.parseInt(sizeRaw, 10) || 20));
  const totalPages = (total: number) => Math.max(1, Math.ceil(total / pageSize));

  let runs: RunSummary[] = [];
  let totalCount = 0;
  let loadFailure: ApiLoadFailureState | null = null;
  let malformedMessage: string | null = null;

  try {
    const raw: unknown = await listRunsByProjectPaged(projectId, page, pageSize);
    const coerced = coerceRunSummaryPaged(raw);

    if (!coerced.ok) {
      malformedMessage = coerced.message;
      runs = [];
      totalCount = 0;
    } else {
      runs = coerced.value.items;
      totalCount = coerced.value.totalCount;
    }
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure === null && malformedMessage === null && totalCount > 0) {
    const pages = Math.max(1, Math.ceil(totalCount / pageSize));

    if (page > pages) {
      redirect(`/runs?projectId=${encodeURIComponent(projectId)}&page=${pages}&pageSize=${pageSize}`);
    }
  }

  return (
    <main aria-labelledby="runs-page-heading">
      <h2 id="runs-page-heading">
        Runs{" "}
        <span style={{ fontSize: "0.92em", fontWeight: 400, color: "#475569" }}>— project {projectId}</span>
      </h2>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.5 }}>
        Open a run for manifest summary, artifact review, compare and replay links, and exports. Results are paged
        server-side (default 20 per page; use <code>?page=</code> and <code>?pageSize=</code>, or legacy{" "}
        <code>?take=</code> as page size).
      </p>
      <p style={{ marginTop: 8 }}>
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs/new">New run (wizard)</Link>
        {" · "}
        <Link href="/graph">Graph</Link>
        {" · "}
        <Link href="/compare">Compare two runs</Link>
      </p>

      {loadFailure && (
        <>
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
          <OperatorTryNext>
            Confirm the API is up (<code>GET /health/live</code>), <code>.env.local</code> has{" "}
            <code>ARCHLUCID_API_BASE_URL</code> (and API key if required), then reload. If you use a non-default
            project, add <code>?projectId=…</code> to the URL. Use the correlation ID above in API logs if support
            asks.
          </OperatorTryNext>
        </>
      )}

      {!loadFailure && malformedMessage && (
        <>
          <OperatorMalformedCallout>
            <strong>Runs list response was not usable.</strong>
            <p style={{ margin: "8px 0 0" }}>{malformedMessage}</p>
            <p style={{ margin: "8px 0 0", fontSize: 14 }}>
              The HTTP call may have succeeded, but the JSON did not match the expected paged run summary
              shape. This is distinct from an empty project (zero runs).
            </p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Deployed UI and API versions may be out of sync—compare release tags. Open <code>GET /version</code> on
            the API and the operator shell build you are running.
          </OperatorTryNext>
        </>
      )}

      {loadFailure === null && !malformedMessage && totalCount === 0 && (
        <OperatorEmptyState title="No runs in this project yet">
          <p style={{ margin: 0 }}>
            This is a valid empty list — start with a guided request, or create runs via API/CLI and refresh.
          </p>
          <p style={{ margin: "14px 0 0" }}>
            <Link
              href="/runs/new"
              style={{
                display: "inline-block",
                padding: "10px 18px",
                background: "#0f766e",
                color: "#fff",
                borderRadius: 8,
                fontWeight: 600,
                textDecoration: "none",
                fontSize: 14,
              }}
            >
              Create your first run (wizard)
            </Link>
          </p>
          <p style={{ margin: "12px 0 0", fontSize: 14, color: "#64748b" }}>
            CLI/API: <code>docs/CLI_USAGE.md</code> · <Link href="/">Home workflow</Link> ·{" "}
            <Link href="/onboarding">Onboarding</Link>
          </p>
        </OperatorEmptyState>
      )}

      {!loadFailure && !malformedMessage && totalCount > 0 && (
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

      {!loadFailure && !malformedMessage && totalCount > 0 ? (
        <nav
          style={{ marginTop: 20, display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap" }}
          aria-label="Runs pagination"
        >
          <span style={{ color: "#475569", fontSize: 14 }}>
            Page {page} of {totalPages(totalCount)} · {totalCount} run{totalCount === 1 ? "" : "s"} total
          </span>
          {page > 1 ? (
            <Link
              href={`/runs?projectId=${encodeURIComponent(projectId)}&page=${page - 1}&pageSize=${pageSize}`}
              style={{ fontWeight: 600 }}
            >
              Previous
            </Link>
          ) : (
            <span style={{ color: "#94a3b8" }}>Previous</span>
          )}
          {page < totalPages(totalCount) ? (
            <Link
              href={`/runs?projectId=${encodeURIComponent(projectId)}&page=${page + 1}&pageSize=${pageSize}`}
              style={{ fontWeight: 600 }}
            >
              Next
            </Link>
          ) : (
            <span style={{ color: "#94a3b8" }}>Next</span>
          )}
        </nav>
      ) : null}
    </main>
  );
}
