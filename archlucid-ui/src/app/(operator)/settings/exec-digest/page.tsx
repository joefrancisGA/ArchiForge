"use client";

import { useCallback, useEffect, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { getExecDigestPreferences, saveExecDigestPreferences } from "@/lib/api";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { cn } from "@/lib/utils";
import type { ExecDigestPreferencesResponse } from "@/types/exec-digest-preferences";

const dayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export default function ExecDigestSettingsPage() {
  const canMutate = useEnterpriseMutationCapability();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [prefs, setPrefs] = useState<ExecDigestPreferencesResponse | null>(null);

  const [emailEnabled, setEmailEnabled] = useState(false);
  const [recipients, setRecipients] = useState("");
  const [timeZone, setTimeZone] = useState("UTC");
  const [dayOfWeek, setDayOfWeek] = useState(1);
  const [hourOfDay, setHourOfDay] = useState(8);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await getExecDigestPreferences();
      setPrefs(data);
      setEmailEnabled(data.emailEnabled);
      setRecipients(data.recipientEmails.join("; "));
      setTimeZone(data.ianaTimeZoneId);
      setDayOfWeek(data.dayOfWeek);
      setHourOfDay(data.hourOfDay);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function onSave() {
    if (!canMutate) {
      return;
    }

    setSaving(true);
    setFailure(null);
    try {
      const list = recipients
        .split(/[;,]/g)
        .map((s) => s.trim())
        .filter((s) => s.length > 0);
      const saved = await saveExecDigestPreferences({
        emailEnabled,
        recipientEmails: list,
        ianaTimeZoneId: timeZone.trim() || "UTC",
        dayOfWeek,
        hourOfDay,
      });
      setPrefs(saved);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Weekly executive digest</h1>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
          Schedule a weekly email for sponsors with compliance drift, committed manifest highlights, and dashboard links.
          Delivery uses the same outbound email transport as trial lifecycle messages.
        </p>
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

      {loading || !prefs ? (
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading preferences…</p>
      ) : (
        <div className="space-y-4 rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
          <div className="flex items-center gap-2">
            <input
              id="exec-digest-enabled"
              type="checkbox"
              className="h-4 w-4"
              checked={emailEnabled}
              disabled={!canMutate}
              onChange={(e) => setEmailEnabled(e.target.checked)}
            />
            <Label htmlFor="exec-digest-enabled" className="text-sm font-medium">
              Enable weekly digest email
            </Label>
          </div>

          <div className="space-y-1">
            <Label htmlFor="exec-digest-recipients">Recipient emails (semicolon-separated)</Label>
            <Input
              id="exec-digest-recipients"
              value={recipients}
              disabled={!canMutate}
              onChange={(e) => setRecipients(e.target.value)}
              placeholder="ops@example.com; sponsor@example.com"
            />
            <p className="text-xs text-neutral-500 dark:text-neutral-400">
              When empty, the trial admin mailbox is used if one exists.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1">
              <Label htmlFor="exec-digest-tz">IANA timezone</Label>
              <Input
                id="exec-digest-tz"
                value={timeZone}
                disabled={!canMutate}
                onChange={(e) => setTimeZone(e.target.value)}
                placeholder="America/New_York"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="exec-digest-hour">Hour (0–23, local)</Label>
              <Input
                id="exec-digest-hour"
                type="number"
                min={0}
                max={23}
                value={hourOfDay}
                disabled={!canMutate}
                onChange={(e) => setHourOfDay(Number.parseInt(e.target.value || "0", 10))}
              />
            </div>
          </div>

          <div className="space-y-1">
            <Label htmlFor="exec-digest-dow">Day of week</Label>
            <select
              id="exec-digest-dow"
              className={cn(
                "h-9 w-full rounded-md border border-neutral-300 bg-white px-2 text-sm",
                "dark:border-neutral-700 dark:bg-neutral-900",
              )}
              value={dayOfWeek}
              disabled={!canMutate}
              onChange={(e) => setDayOfWeek(Number.parseInt(e.target.value, 10))}
            >
              {dayNames.map((label, idx) => (
                <option key={label} value={idx}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2">
            <Button type="button" disabled={!canMutate || saving} onClick={() => void onSave()}>
              {saving ? "Saving…" : "Save preferences"}
            </Button>
            {!prefs.isConfigured ? (
              <span className="text-xs text-neutral-500 dark:text-neutral-400">Not yet saved to the database.</span>
            ) : null}
          </div>
        </div>
      )}
    </div>
  );
}
