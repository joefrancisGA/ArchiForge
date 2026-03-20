import Link from "next/link";
import { listRunsByProject } from "@/lib/api";

export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; take?: string }>;
}) {
  const resolved = await searchParams;
  const projectId = resolved.projectId ?? "default";
  const take = Number(resolved.take ?? "20");

  let runs: Awaited<ReturnType<typeof listRunsByProject>> = [];
  let loadError: string | null = null;
  try {
    runs = await listRunsByProject(projectId, take);
  } catch (e) {
    loadError = e instanceof Error ? e.message : "Failed to load runs.";
  }

  return (
    <main>
      <h2>Runs</h2>
      <p>Project: {projectId}</p>

      {loadError && (
        <p style={{ color: "crimson" }}>
          {loadError} — check <code>ARCHIFORGE_API_BASE_URL</code> and that the API is running.
        </p>
      )}

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
                <Link href={`/runs/${run.runId}`}>Open</Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {!loadError && runs.length === 0 && <p>No runs found.</p>}
    </main>
  );
}
