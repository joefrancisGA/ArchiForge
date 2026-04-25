"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { ContextualHelp } from "@/components/ContextualHelp";
import { useOperatorNavAuthority } from "@/components/OperatorNavAuthorityProvider";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getExecDigestPreferences, saveExecDigestPreferences } from "@/lib/api";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { getEffectiveBrowserProxyScopeHeaders } from "@/lib/operator-scope-storage";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { operateCapabilityFromRank } from "@/lib/operate-capability";
import { showError, showSuccess } from "@/lib/toast";
import type { ExecDigestPreferencesResponse, ExecDigestPreferencesUpsertRequest } from "@/types/exec-digest-preferences";

type TrialStatusPayload = {
  status?: string;
  daysRemaining?: number | null;
};

/**
 * Non-sensitive tenant settings operators touch daily: trial, digest email, and effective request scope
 * (headers; full workspace list remains on the API roadmap).
 */
export default function TenantSettingsPage() {
  const { callerAuthorityRank, currentPrincipal } = useOperatorNavAuthority();
  const canEditDigest = operateCapabilityFromRank(callerAuthorityRank);
  const [digestLoadFailure, setDigestLoadFailure] = useState<string | null>(null);
  const [trial, setTrial] = useState<TrialStatusPayload | null>(null);
  const [digest, setDigest] = useState<ExecDigestPreferencesResponse | null>(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<ExecDigestPreferencesUpsertRequest | null>(null);
  const scope = getEffectiveBrowserProxyScopeHeaders();

  const load = useCallback(async () => {
    setDigestLoadFailure(null);
    try {
      const tRes = await fetch(
        "/api/proxy/v1/tenant/trial-status",
        mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
      );
      if (tRes.ok) {
        setTrial((await tRes.json()) as TrialStatusPayload);
      } else {
        setTrial(null);
      }
    } catch {
      setTrial(null);
    }
    try {
      const d = await getExecDigestPreferences();
      setDigest(d);
      setForm({
        emailEnabled: d.emailEnabled,
        recipientEmails: [...d.recipientEmails],
        ianaTimeZoneId: d.ianaTimeZoneId,
        dayOfWeek: d.dayOfWeek,
        hourOfDay: d.hourOfDay,
      });
    } catch (e) {
      setDigestLoadFailure(toApiLoadFailure(e).message);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function onSaveDigest(e: React.FormEvent): Promise<void> {
    e.preventDefault();
    if (form === null) {
      return;
    }
    if (!canEditDigest) {
      return;
    }
    setSaving(true);
    try {
      const next = await saveExecDigestPreferences(form);
      setDigest(next);
      showSuccess("Notification preferences saved.");
    } catch (e) {
      const msg = e instanceof Error ? e.message : String(e);
      showError("Could not save notification preferences", msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="mx-auto max-w-2xl space-y-6" data-testid="tenant-settings-page">
      <div>
        <div className="flex items-start gap-2">
          <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Tenant settings</h1>
          <ContextualHelp helpKey="tenant-settings-page" />
        </div>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">Workspace defaults and operator-facing preferences for this tenant. Infrastructure and feature-flag controls stay server-side only.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Tenant name</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-300">
            A friendly tenant display name is not available through the public API yet. Principal name from sign-in:{" "}
            <span className="font-medium">{currentPrincipal.name ?? "—"}</span>.{" "}
            <span className="text-neutral-500">(API endpoint not yet available)</span>
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Request scope (workspace / project)</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-neutral-600 dark:text-neutral-300">
          <p className="m-0">These values are what the UI sends on <code className="text-xs">/api/proxy</code> calls. Use the header scope switcher to change the active project.</p>
          <ul className="m-0 list-inside list-disc">
            <li>
              <code className="text-xs">x-tenant-id</code>: {scope["x-tenant-id"]}
            </li>
            <li>
              <code className="text-xs">x-workspace-id</code>: {scope["x-workspace-id"]}
            </li>
            <li>
              <code className="text-xs">x-project-id</code>: {scope["x-project-id"]}
            </li>
          </ul>
          <p className="m-0 text-xs text-neutral-500">Listing workspaces from the API (GET /v1/tenant/workspaces) is optional; the <Link className="text-teal-800 underline dark:text-teal-300" href="/">home</Link> scope control reflects your selection.</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Pilot / trial</CardTitle>
        </CardHeader>
        <CardContent>
          {trial == null ? (
            <p className="m-0 text-sm text-neutral-500">Could not load trial status.</p>
          ) : (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-300">
              <span className="font-medium">Status:</span> {trial.status ?? "—"}
              {typeof trial.daysRemaining === "number" ? (
                <span>
                  {" "}
                  — <span className="font-medium">Days remaining:</span> {trial.daysRemaining}
                </span>
              ) : null}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Executive digest (email)</CardTitle>
        </CardHeader>
        <CardContent>
          {digestLoadFailure !== null ? (
            <p className="m-0 text-sm text-rose-800 dark:text-rose-200" role="alert">
              {digestLoadFailure}
            </p>
          ) : null}
          {digest == null || form == null ? (
            <p className="m-0 text-sm text-neutral-500">No digest preferences loaded.</p>
          ) : (
            <form onSubmit={onSaveDigest} className="space-y-4">
              <div className="flex items-center justify-between gap-2">
                <div>
                  <p className="m-0 text-sm font-medium text-neutral-800 dark:text-neutral-100">Email enabled</p>
                  <p className="m-0 text-xs text-neutral-500">Sends a weekly roll-up to the listed addresses (API-enforced on save).</p>
                </div>
                <label className="inline-flex cursor-pointer items-center gap-2">
                  <input
                    type="checkbox"
                    className="size-4 rounded border border-neutral-300 text-teal-800 focus:ring-2 focus:ring-neutral-400 dark:border-neutral-600"
                    checked={form.emailEnabled}
                    onChange={(e) => {
                      setForm((f) => (f === null ? f : { ...f, emailEnabled: e.target.checked }));
                    }}
                    disabled={!canEditDigest}
                    aria-label="Email enabled"
                  />
                </label>
              </div>
              <div>
                <Label htmlFor="digest-emails">Recipient emails (one per line or comma-separated)</Label>
                <TextareaishEmails
                  id="digest-emails"
                  value={form.recipientEmails}
                  onChange={(emails) => {
                    setForm((f) => (f === null ? f : { ...f, recipientEmails: emails }));
                  }}
                  readOnly={!canEditDigest}
                />
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div>
                  <Label htmlFor="tz">IANA time zone</Label>
                  <Input
                    id="tz"
                    name="ianaTimeZoneId"
                    value={form.ianaTimeZoneId}
                    onChange={(ev) => {
                      setForm((f) => (f === null ? f : { ...f, ianaTimeZoneId: ev.target.value }));
                    }}
                    readOnly={!canEditDigest}
                  />
                </div>
                <div>
                  <Label htmlFor="dow">Day of week (0–6)</Label>
                  <Input
                    id="dow"
                    inputMode="numeric"
                    value={String(form.dayOfWeek)}
                    onChange={(ev) => {
                      setForm((f) => (f === null ? f : { ...f, dayOfWeek: Number.parseInt(ev.target.value, 10) || 0 }));
                    }}
                    readOnly={!canEditDigest}
                  />
                </div>
                <div>
                  <Label htmlFor="hour">Hour of day (0–23)</Label>
                  <Input
                    id="hour"
                    inputMode="numeric"
                    value={String(form.hourOfDay)}
                    onChange={(ev) => {
                      setForm((f) => (f === null ? f : { ...f, hourOfDay: Number.parseInt(ev.target.value, 10) || 0 }));
                    }}
                    readOnly={!canEditDigest}
                  />
                </div>
              </div>
              {!canEditDigest ? <p className="m-0 text-xs text-neutral-500">Editing requires operator rank (Execute) on the API; your session is read-only in the UI for these controls.</p> : null}
              <div>
                <Button type="submit" disabled={!canEditDigest || saving} data-testid="tenant-digest-save">
                  {saving ? "Saving…" : "Save notification preferences"}
                </Button>
              </div>
            </form>
          )}
        </CardContent>
      </Card>
    </main>
  );
}

function TextareaishEmails(props: {
  id: string;
  value: readonly string[];
  onChange: (next: string[]) => void;
  readOnly: boolean;
}): React.ReactNode {
  const v = props.value.join(", ");
  return (
    <Input
      id={props.id}
      value={v}
      onChange={(ev) => {
        const raw = ev.target.value;
        const next = raw
          .split(/[,\n]+/)
          .map((s) => s.trim())
          .filter((s) => s.length > 0);
        props.onChange(next);
      }}
      readOnly={props.readOnly}
    />
  );
}
