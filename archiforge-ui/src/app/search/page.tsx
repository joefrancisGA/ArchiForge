"use client";

import { useState } from "react";
import { apiGet } from "@/lib/api";

type RetrievalHit = {
  chunkId: string;
  documentId: string;
  sourceType: string;
  sourceId: string;
  title: string;
  text: string;
  score: number;
};

export default function SearchPage() {
  const [query, setQuery] = useState("");
  const [runId, setRunId] = useState("");
  const [results, setResults] = useState<RetrievalHit[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSearch() {
    const q = query.trim();
    if (!q) return;

    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams();
      params.set("q", q);
      if (runId.trim()) params.set("runId", runId.trim());
      const data = await apiGet<RetrievalHit[]>(`/api/retrieval/search?${params.toString()}`);
      setResults(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Search failed");
      setResults([]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main style={{ maxWidth: 900 }}>
      <h2 style={{ marginTop: 0 }}>Semantic Search</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Scoped to your workspace. Uses the same embedding + index as Ask ArchiForge (in-memory + fake vectors by
        default).
      </p>

      <div style={{ display: "grid", gap: 12, marginBottom: 24 }}>
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search architecture knowledge..."
          style={{ padding: 8 }}
        />
        <input
          value={runId}
          onChange={(e) => setRunId(e.target.value)}
          placeholder="Optional Run ID filter"
          style={{ padding: 8, fontFamily: "monospace" }}
        />
        <button type="button" onClick={() => void onSearch()} disabled={loading || !query.trim()}>
          {loading ? "Searching…" : "Search"}
        </button>
      </div>

      {error ? (
        <p style={{ color: "crimson" }} role="alert">
          {error}
        </p>
      ) : null}

      <div style={{ display: "grid", gap: 12 }}>
        {results.map((hit) => (
          <div
            key={hit.chunkId}
            style={{
              border: "1px solid #ddd",
              borderRadius: 8,
              padding: 12,
              background: "#fff",
            }}
          >
            <strong>{hit.title}</strong>
            <div style={{ fontSize: 13, color: "#555" }}>{hit.sourceType}</div>
            <div style={{ fontSize: 13 }}>Score: {hit.score.toFixed(3)}</div>
            <p style={{ whiteSpace: "pre-wrap", marginBottom: 0 }}>{hit.text}</p>
          </div>
        ))}
      </div>
    </main>
  );
}
