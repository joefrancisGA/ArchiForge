"use client";

import { useState } from "react";
import { askArchiForge } from "@/lib/api";
import type { AskResponse } from "@/types/ask";

export default function AskPage() {
  const [runId, setRunId] = useState("");
  const [baseRunId, setBaseRunId] = useState("");
  const [targetRunId, setTargetRunId] = useState("");
  const [question, setQuestion] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<AskResponse | null>(null);

  async function ask() {
    setError(null);
    setResult(null);
    const rid = runId.trim();
    if (!rid) {
      setError("Enter a run ID.");
      return;
    }
    if (!question.trim()) {
      setError("Enter a question.");
      return;
    }

    const base = baseRunId.trim();
    const target = targetRunId.trim();
    const useCompare = base.length > 0 && target.length > 0;

    setLoading(true);
    try {
      const data = await askArchiForge({
        runId: rid,
        question: question.trim(),
        baseRunId: useCompare ? base : undefined,
        targetRunId: useCompare ? target : undefined,
      });
      setResult(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Request failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main style={{ maxWidth: 720 }}>
      <h2 style={{ marginTop: 0 }}>Ask ArchiForge</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Answers are grounded in the run&apos;s GoldenManifest, provenance graph, and optional base → target
        comparison.
      </p>

      <div style={{ display: "flex", flexDirection: "column", gap: 12, marginBottom: 16 }}>
        <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 14 }}>
          Primary run ID (required)
          <input
            value={runId}
            onChange={(e) => setRunId(e.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
            style={{ padding: 8, fontFamily: "monospace" }}
          />
        </label>
        <details style={{ fontSize: 14 }}>
          <summary style={{ cursor: "pointer" }}>Optional: compare two runs</summary>
          <div style={{ display: "flex", flexDirection: "column", gap: 12, marginTop: 12 }}>
            <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              Base run ID
              <input
                value={baseRunId}
                onChange={(e) => setBaseRunId(e.target.value)}
                style={{ padding: 8, fontFamily: "monospace" }}
              />
            </label>
            <label style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              Target run ID
              <input
                value={targetRunId}
                onChange={(e) => setTargetRunId(e.target.value)}
                style={{ padding: 8, fontFamily: "monospace" }}
              />
            </label>
          </div>
        </details>
        <label style={{ display: "flex", flexDirection: "column", gap: 4, fontSize: 14 }}>
          Question
          <textarea
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="e.g. Why did we choose X over Y? What changed between runs?"
            rows={4}
            style={{ padding: 8, fontFamily: "inherit" }}
          />
        </label>
      </div>

      <button type="button" onClick={ask} disabled={loading} style={{ padding: "8px 16px" }}>
        {loading ? "Asking…" : "Ask"}
      </button>

      {error ? (
        <p style={{ color: "crimson", marginTop: 16 }} role="alert">
          {error}
        </p>
      ) : null}

      {result ? (
        <div style={{ marginTop: 24 }}>
          <h3 style={{ marginBottom: 8 }}>Answer</h3>
          <pre
            style={{
              whiteSpace: "pre-wrap",
              background: "#f6f8fa",
              padding: 12,
              borderRadius: 8,
              fontSize: 14,
            }}
          >
            {result.answer}
          </pre>
          {(result.referencedDecisions?.length ?? 0) > 0 ? (
            <section style={{ marginTop: 16 }}>
              <h4 style={{ margin: "0 0 8px" }}>Referenced decisions</h4>
              <ul style={{ margin: 0 }}>
                {result.referencedDecisions.map((d) => (
                  <li key={d}>{d}</li>
                ))}
              </ul>
            </section>
          ) : null}
          {(result.referencedFindings?.length ?? 0) > 0 ? (
            <section style={{ marginTop: 16 }}>
              <h4 style={{ margin: "0 0 8px" }}>Referenced findings</h4>
              <ul style={{ margin: 0 }}>
                {result.referencedFindings.map((f) => (
                  <li key={f} style={{ fontFamily: "monospace", fontSize: 13 }}>
                    {f}
                  </li>
                ))}
              </ul>
            </section>
          ) : null}
          {(result.referencedArtifacts?.length ?? 0) > 0 ? (
            <section style={{ marginTop: 16 }}>
              <h4 style={{ margin: "0 0 8px" }}>Referenced artifacts</h4>
              <ul style={{ margin: 0 }}>
                {result.referencedArtifacts.map((a) => (
                  <li key={a}>{a}</li>
                ))}
              </ul>
            </section>
          ) : null}
        </div>
      ) : null}
    </main>
  );
}
