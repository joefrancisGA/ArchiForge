"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";

import { DemoUnavailableNotice } from "@/components/DemoUnavailableNotice";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { showError, showSuccess } from "@/lib/toast";

type TenantBaselineGet = {
  manualPrepHoursPerReview: number | null;
  peoplePerReview: number | null;
  capturedUtc: string | null;
};

function parseNumberOrNull(raw: string): number | null {
  const t = raw.trim();

  if (t.length === 0) {
    return null;
  }

  const n = Number(t);

  if (!Number.isFinite(n)) {
    return Number.NaN;
  }

  return n;
}

/** Client UI for ROI baseline measurement fields (loads/saves `/v1/tenant/baseline` via proxy). */
export function BaselineSettingsClient() {
  const [loadFailure, setLoadFailure] = useState<ApiLoadFailureState | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [manualPrep, setManualPrep] = useState("");
  const [people, setPeople] = useState("");
  const demoMode = isNextPublicDemoMode();

  const load = useCallback(async () => {
    if (demoMode) {
      return;
    }

    setLoading(true);
    setLoadFailure(null);

    try {
      const res = await fetch("/api/proxy/v1/tenant/baseline", {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "include",
      });

      if (!res.ok) {
        const t = await res.text();

        throw { status: res.status, body: t };
      }

      const data = (await res.json()) as TenantBaselineGet;

      setManualPrep(
        data.manualPrepHoursPerReview != null && Number.isFinite(data.manualPrepHoursPerReview)
          ? String(data.manualPrepHoursPerReview)
          : "",
      );
      setPeople(
        data.peoplePerReview != null && Number.isFinite(data.peoplePerReview) ? String(data.peoplePerReview) : "",
      );
    } catch (e) {
      setLoadFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, [demoMode]);

  useEffect(() => {
    if (demoMode) {
      setLoading(false);

      return;
    }

    void load();
  }, [demoMode, load]);

  async function onSave(e: FormEvent): Promise<void> {
    e.preventDefault();

    if (demoMode) {
      return;
    }

    setSaving(true);

    try {
      const prepN = parseNumberOrNull(manualPrep);

      if (Number.isNaN(prepN)) {
        showError("Baseline", "Manual preparation hours must be a number (or leave blank).");

        return;
      }

      if (prepN != null && (prepN <= 0 || prepN > 10_000)) {
        showError("Baseline", "Manual preparation hours must be between 0 and 10,000 (exclusive of zero) when set.");

        return;
      }

      const peopleN = parseNumberOrNull(people);

      if (Number.isNaN(peopleN)) {
        showError("Baseline", "People per review must be a number (or leave blank).");

        return;
      }

      if (peopleN != null && (peopleN <= 0 || peopleN > 10_000)) {
        showError("Baseline", "People per review must be between 1 and 10,000 when set.");

        return;
      }

      const res = await fetch("/api/proxy/v1/tenant/baseline", {
        method: "PUT",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        credentials: "include",
        body: JSON.stringify({
          manualPrepHoursPerReview: manualPrep.trim() === "" ? null : prepN,
          peoplePerReview: people.trim() === "" ? null : peopleN,
        }),
      });

      if (!res.ok) {
        const t = await res.text();
        let detail = t;

        try {
          const p = JSON.parse(t) as { detail?: string };

          if (typeof p.detail === "string") {
            detail = p.detail;
          }
        } catch {
          /* ignore */
        }

        showError("Baseline", detail || `Request failed (${res.status})`);

        return;
      }

      showSuccess("Baseline settings saved.");
      await load();
    } catch (err) {
      showError("Baseline", err instanceof Error ? err.message : "Request failed.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Baseline settings — ROI measurement</h1>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
          These fields tighten the &quot;before&quot; anchor for your value reports. If you skip them, we use conservative
          model defaults. You can update them at any time.
        </p>
      </div>
      {demoMode ? (
        <DemoUnavailableNotice
          title="Baseline settings"
          description="ROI baseline measurement requires a connected deployment and tenant API access."
        />
      ) : null}
      {!demoMode && loadFailure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
        </div>
      ) : null}
      {!demoMode && loading ? (
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading…</p>
      ) : !demoMode ? (
        <form onSubmit={onSave} className="space-y-4">
          <div>
            <Label htmlFor="baseline-prep">Manual preparation hours per review (optional)</Label>
            <Input
              id="baseline-prep"
              type="number"
              min={0}
              step="any"
              className="mt-1"
              data-testid="baseline-manual-prep"
              value={manualPrep}
              onChange={(x) => setManualPrep(x.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="baseline-people">People involved per review (optional)</Label>
            <Input
              id="baseline-people"
              type="number"
              min={0}
              step="1"
              className="mt-1"
              data-testid="baseline-people"
              value={people}
              onChange={(x) => setPeople(x.target.value)}
            />
          </div>
          <div>
            <Button type="submit" disabled={saving} variant="primary" data-testid="baseline-save">
              {saving ? "Saving…" : "Save"}
            </Button>
          </div>
        </form>
      ) : null}
    </div>
  );
}
