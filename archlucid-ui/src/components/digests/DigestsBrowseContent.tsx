"use client";

import { useEffect, useState } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import {
  digestsHistoryHeadingOperator,
  digestsHistoryHeadingReader,
  digestsListRefreshButtonTitleOperator,
  digestsListRefreshButtonTitleReader,
} from "@/lib/enterprise-controls-context-copy";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  getArchitectureDigest,
  listArchitectureDigests,
  listDigestDeliveryAttempts,
} from "@/lib/api";
import type { ArchitectureDigest } from "@/types/advisory-scheduling";
import type { DigestDeliveryAttempt } from "@/types/digest-subscriptions";

/**
 * Browse tab: architecture digest list and detail (former standalone `/digests` page body).
 */
export function DigestsBrowseContent() {
  const canMutateEnterpriseShell = useEnterpriseMutationCapability();
  const [digests, setDigests] = useState<ArchitectureDigest[]>([]);
  const [selected, setSelected] = useState<ArchitectureDigest | null>(null);
  const [deliveryAttempts, setDeliveryAttempts] = useState<DigestDeliveryAttempt[]>([]);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  useEffect(() => {
    void loadDigests();
  }, []);

  async function loadDigests() {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listArchitectureDigests(40);
      setDigests(data);
      setSelected(null);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function selectDigest(d: ArchitectureDigest) {
    setFailure(null);
    try {
      const full = await getArchitectureDigest(d.digestId);
      setSelected(full);
      const attempts = await listDigestDeliveryAttempts(d.digestId);
      setDeliveryAttempts(attempts);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  return (
    <main style={{ maxWidth: 1100 }}>
      <h2 style={{ marginTop: 0 }}>Architecture digests</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Markdown digests from scheduled or manual advisory scans (v1: plain preformatted view).
      </p>

      <div style={{ marginBottom: 16 }}>
        <button
          type="button"
          onClick={() => void loadDigests()}
          disabled={loading}
          title={
            canMutateEnterpriseShell
              ? digestsListRefreshButtonTitleOperator
              : digestsListRefreshButtonTitleReader
          }
        >
          {loading ? "Loading…" : "Refresh"}
        </button>
      </div>

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <div style={{ display: "grid", gridTemplateColumns: "minmax(280px, 320px) 1fr", gap: 16 }}>
        <aside style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fff" }}>
          <h3 style={{ marginTop: 0, fontSize: 16 }}>
            {canMutateEnterpriseShell ? digestsHistoryHeadingOperator : digestsHistoryHeadingReader}
          </h3>
          {digests.length === 0 ? (
            <p style={{ color: "#666", fontSize: 14 }}>No digests yet.</p>
          ) : (
            <ul style={{ paddingLeft: 16, margin: 0 }}>
              {digests.map((digest) => (
                <li key={digest.digestId} style={{ marginBottom: 8 }}>
                  <button
                    type="button"
                    onClick={() => void selectDigest(digest)}
                    style={{ textAlign: "left", background: "none", border: "none", cursor: "pointer", padding: 0 }}
                  >
                    {digest.title}
                    <div style={{ fontSize: 12, color: "#666" }}>
                      {new Date(digest.generatedUtc).toLocaleString()}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </aside>

        <section style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16, background: "#fff" }}>
          {!selected && <p style={{ color: "#666" }}>Select a digest.</p>}

          {selected ? (
            <>
              <h3 style={{ marginTop: 0 }}>{selected.title}</h3>
              <p>{selected.summary}</p>
              <p style={{ fontSize: 13, color: "#555" }}>
                Run: {selected.runId ?? "—"}
                {selected.comparedToRunId ? ` · Compared to: ${selected.comparedToRunId}` : null}
              </p>
              <pre
                style={{
                  whiteSpace: "pre-wrap",
                  fontFamily: "ui-monospace, monospace",
                  fontSize: 13,
                  background: "#f8f8f8",
                  padding: 12,
                  borderRadius: 6,
                }}
              >
                {selected.contentMarkdown}
              </pre>

              <div style={{ marginTop: 20, paddingTop: 16, borderTop: "1px solid #eee" }}>
                <h4 style={{ marginTop: 0, fontSize: 15 }}>Delivery attempts</h4>
                {deliveryAttempts.length === 0 ? (
                  <p style={{ color: "#666", fontSize: 14, margin: 0 }}>
                    No attempts recorded (add subscriptions in the <strong>Subscriptions</strong> tab of this hub).
                  </p>
                ) : (
                  <ul style={{ fontSize: 13, paddingLeft: 20, margin: 0 }}>
                    {deliveryAttempts.map((a) => (
                      <li key={a.attemptId} style={{ marginBottom: 6 }}>
                        <strong>{a.status}</strong> · {a.channelType} · {new Date(a.attemptedUtc).toLocaleString()}
                        {a.errorMessage ? (
                          <span style={{ color: "crimson" }}> — {a.errorMessage}</span>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          ) : null}
        </section>
      </div>
    </main>
  );
}
