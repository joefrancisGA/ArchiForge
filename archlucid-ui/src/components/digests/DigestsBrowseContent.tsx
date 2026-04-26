"use client";

import { useEffect, useState } from "react";

import { DocumentLayout } from "@/components/DocumentLayout";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
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
    <main className="mx-auto max-w-5xl">
      <h2 className="m-0 text-xl font-semibold text-neutral-900 dark:text-neutral-100">Architecture digests</h2>
      <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
        Markdown digests from scheduled or manual advisory scans (v1: plain preformatted view).
      </p>

      <div className="mt-4">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => void loadDigests()}
          disabled={loading}
          title={
            canMutateEnterpriseShell
              ? digestsListRefreshButtonTitleOperator
              : digestsListRefreshButtonTitleReader
          }
        >
          {loading ? "Loading…" : "Refresh"}
        </Button>
      </div>

      {failure !== null ? (
        <div className="mt-4" role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-[minmax(16rem,20rem)_1fr]">
        <aside className="rounded-lg border border-neutral-200 bg-white p-3 dark:border-neutral-700 dark:bg-neutral-950">
          <h3 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
            {canMutateEnterpriseShell ? digestsHistoryHeadingOperator : digestsHistoryHeadingReader}
          </h3>
          {digests.length === 0 ? (
            <p className="m-0 mt-2 text-sm text-neutral-500 dark:text-neutral-400">No digests yet.</p>
          ) : (
            <ul className="m-0 mt-2 list-none space-y-2 p-0">
              {digests.map((digest) => (
                <li key={digest.digestId}>
                  <button
                    type="button"
                    onClick={() => void selectDigest(digest)}
                    className="w-full cursor-pointer rounded-md border border-transparent p-1 text-left text-sm text-neutral-900 transition-colors hover:border-neutral-200 hover:bg-neutral-50 dark:text-neutral-100 dark:hover:border-neutral-700 dark:hover:bg-neutral-900/60"
                  >
                    {digest.title}
                    <div className="mt-0.5 text-xs text-neutral-500 dark:text-neutral-400">
                      {new Date(digest.generatedUtc).toLocaleString()}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </aside>

        <section className="min-w-0 rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950">
          {!selected ? <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">Select a digest.</p> : null}

          {selected ? (
            <DocumentLayout
              tocItems={[
                { id: "digest-body", label: "Digest" },
                { id: "digest-delivery", label: "Delivery attempts" },
                { id: "digest-meta", label: "Metadata" },
              ]}
            >
              <h2 id="digest-body" className="m-0 text-xl font-bold text-neutral-900 dark:text-neutral-50">
                {selected.title}
              </h2>
              <p className="m-0 text-base leading-relaxed text-neutral-800 dark:text-neutral-200">{selected.summary}</p>
              <p id="digest-meta" className="doc-meta m-0 text-sm text-neutral-600 dark:text-neutral-400">
                Run: {selected.runId ?? "—"}
                {selected.comparedToRunId ? ` · Compared to: ${selected.comparedToRunId}` : null}
              </p>
              <pre className="whitespace-pre-wrap rounded-md border border-neutral-200 bg-neutral-100 p-3 font-mono text-sm text-neutral-900 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100">
                {selected.contentMarkdown}
              </pre>

              <div id="digest-delivery" className="mt-6 border-t border-neutral-200 pt-4 dark:border-neutral-800">
                <h3 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Delivery attempts</h3>
                {deliveryAttempts.length === 0 ? (
                  <p className="m-0 mt-2 text-sm text-neutral-600 dark:text-neutral-400">
                    No attempts recorded (add subscriptions in the <strong>Subscriptions</strong> tab of this hub).
                  </p>
                ) : (
                  <ul className="m-0 mt-2 list-disc space-y-1 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
                    {deliveryAttempts.map((a) => (
                      <li key={a.attemptId}>
                        <strong>{a.status}</strong> · {a.channelType} · {new Date(a.attemptedUtc).toLocaleString()}
                        {a.errorMessage ? (
                          <span className="text-rose-700 dark:text-rose-300"> — {a.errorMessage}</span>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </DocumentLayout>
          ) : null}
        </section>
      </div>
    </main>
  );
}
