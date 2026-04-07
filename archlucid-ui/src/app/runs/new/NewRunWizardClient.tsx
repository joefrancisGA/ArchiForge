"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";

import {
  createArchitectureRun,
  type CreateArchitectureRunRequestPayload,
  getRunSummary,
} from "@/lib/api";
import type { RunSummary } from "@/types/authority";

type WizardStep = 1 | 2 | 3 | 4;

const defaultPayload = (): CreateArchitectureRunRequestPayload => ({
  requestId: typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID().replace(/-/g, "") : `ui-${Date.now()}`,
  description: "",
  systemName: "",
  environment: "staging",
  cloudProvider: "Azure",
  constraints: [],
  requiredCapabilities: [],
  assumptions: [],
});

/** Multi-step client wizard: collect request fields, create run, poll status with live region + toast. */
export function NewRunWizardClient() {
  const [step, setStep] = useState<WizardStep>(1);
  const [payload, setPayload] = useState<CreateArchitectureRunRequestPayload>(defaultPayload);
  const [submitting, setSubmitting] = useState(false);
  const [runId, setRunId] = useState<string | null>(null);
  const [pollSummary, setPollSummary] = useState<RunSummary | null>(null);
  const [toast, setToast] = useState<{ kind: "ok" | "err"; message: string } | null>(null);
  const liveRef = useRef<HTMLDivElement>(null);

  const showToast = useCallback((kind: "ok" | "err", message: string) => {
    setToast({ kind, message });
    window.setTimeout(() => setToast(null), 6000);
  }, []);

  useEffect(() => {
    if (runId === null) {
      return;
    }

    let cancelled = false;
    const started = Date.now();
    const maxMs = 120_000;
    const intervalMs = 3000;

    const tick = async () => {
      try {
        const s = await getRunSummary(runId);

        if (!cancelled) {
          setPollSummary(s);
        }
      } catch {
        /* keep polling until timeout */
      }
    };

    void tick();
    const id = window.setInterval(async () => {
      if (cancelled || Date.now() - started > maxMs) {
        window.clearInterval(id);

        return;
      }

      await tick();
    }, intervalMs);

    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [runId]);

  const goNext = () => setStep((s) => (s < 4 ? ((s + 1) as WizardStep) : s));

  const goBack = () => setStep((s) => (s > 1 ? ((s - 1) as WizardStep) : s));

  const submit = async () => {
    if (payload.description.trim().length < 10) {
      showToast("err", "Description must be at least 10 characters.");

      return;
    }

    if (!payload.systemName.trim()) {
      showToast("err", "System name is required.");

      return;
    }

    setSubmitting(true);

    try {
      const res = await createArchitectureRun({
        ...payload,
        description: payload.description.trim(),
        systemName: payload.systemName.trim(),
      });
      const id = res.run?.runId ?? null;

      if (!id) {
        showToast("err", "API returned no run id.");

        return;
      }

      setRunId(id);
      setStep(4);
      showToast("ok", "Run created. Tracking status below.");
    } catch (e: unknown) {
      const message =
        e && typeof e === "object" && "message" in e
          ? String((e as { message?: string }).message)
          : "Request failed.";
      showToast("err", message);
    } finally {
      setSubmitting(false);
    }
  };

  const liveMessage =
    runId === null
      ? "No run yet."
      : pollSummary
        ? `Run ${runId} polled: context ${pollSummary.hasContextSnapshot ? "ready" : "pending"}, golden manifest ${pollSummary.hasGoldenManifest ? "ready" : "pending"}.`
        : `Run ${runId} created; loading summary.`;

  return (
    <div style={{ maxWidth: 640 }}>
      <p style={{ marginTop: 0, color: "#475569" }}>
        Step {step} of 4 — guided create for <code>/v1/architecture/request</code>.
      </p>

      <div
        ref={liveRef}
        aria-live="polite"
        aria-atomic="true"
        style={{
          position: "absolute",
          width: 1,
          height: 1,
          padding: 0,
          margin: -1,
          overflow: "hidden",
          clip: "rect(0,0,0,0)",
          whiteSpace: "nowrap",
          border: 0,
        }}
      >
        {liveMessage}
      </div>

      {step === 1 && (
        <label style={{ display: "block", marginBottom: 16 }}>
          <span style={{ display: "block", fontWeight: 600, marginBottom: 6 }}>System name</span>
          <input
            type="text"
            value={payload.systemName}
            onChange={(e) => setPayload((p) => ({ ...p, systemName: e.target.value }))}
            style={{ width: "100%", padding: 8, boxSizing: "border-box" }}
            autoComplete="off"
          />
        </label>
      )}

      {step === 2 && (
        <label style={{ display: "block", marginBottom: 16 }}>
          <span style={{ display: "block", fontWeight: 600, marginBottom: 6 }}>
            Description (min 10 characters)
          </span>
          <textarea
            value={payload.description}
            onChange={(e) => setPayload((p) => ({ ...p, description: e.target.value }))}
            rows={6}
            style={{ width: "100%", padding: 8, boxSizing: "border-box" }}
          />
        </label>
      )}

      {step === 3 && (
        <label style={{ display: "block", marginBottom: 16 }}>
          <span style={{ display: "block", fontWeight: 600, marginBottom: 6 }}>Environment label</span>
          <input
            type="text"
            value={payload.environment}
            onChange={(e) => setPayload((p) => ({ ...p, environment: e.target.value }))}
            style={{ width: "100%", padding: 8, boxSizing: "border-box" }}
            autoComplete="off"
          />
        </label>
      )}

      {step === 4 && runId && (
        <div style={{ marginBottom: 16 }}>
          <p style={{ margin: "0 0 8px" }}>
            <strong>Run id:</strong>{" "}
            <code style={{ fontSize: 13 }}>{runId}</code>
          </p>
          {pollSummary && (
            <p style={{ margin: 0, color: "#334155" }}>
              <strong>Pipeline signals:</strong> context {pollSummary.hasContextSnapshot ? "✓" : "…"}, graph{" "}
              {pollSummary.hasGraphSnapshot ? "✓" : "…"}, findings {pollSummary.hasFindingsSnapshot ? "✓" : "…"},
              manifest {pollSummary.hasGoldenManifest ? "✓" : "…"}
              {pollSummary.description ? (
                <>
                  <br />
                  <span style={{ fontSize: 14 }}>{pollSummary.description}</span>
                </>
              ) : null}
            </p>
          )}
          <p style={{ margin: "12px 0 0" }}>
            <Link href={`/runs/${runId}`}>Open run detail</Link>
            {" · "}
            <Link href="/runs">Back to runs list</Link>
          </p>
        </div>
      )}

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginTop: 16 }}>
        {step > 1 && step < 4 && (
          <button type="button" onClick={goBack}>
            Back
          </button>
        )}
        {step < 3 && (
          <button type="button" onClick={goNext}>
            Next
          </button>
        )}
        {step === 3 && (
          <button type="button" onClick={submit} disabled={submitting}>
            {submitting ? "Creating…" : "Create run"}
          </button>
        )}
      </div>

      {toast && (
        <div
          role="status"
          style={{
            position: "fixed",
            bottom: 24,
            right: 24,
            maxWidth: 360,
            padding: "12px 16px",
            borderRadius: 8,
            background: toast.kind === "ok" ? "#ecfdf5" : "#fef2f2",
            color: toast.kind === "ok" ? "#065f46" : "#991b1b",
            boxShadow: "0 4px 12px rgba(0,0,0,0.12)",
            zIndex: 50,
          }}
        >
          {toast.message}
        </div>
      )}
    </div>
  );
}
