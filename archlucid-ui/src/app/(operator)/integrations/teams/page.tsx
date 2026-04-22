"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import {
  deleteTeamsIncomingWebhookConnection,
  getTeamsIncomingWebhookConnection,
  getTeamsNotificationTriggerCatalog,
  upsertTeamsIncomingWebhookConnection,
} from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type {
  TeamsIncomingWebhookConnectionResponse,
  TeamsIncomingWebhookConnectionUpsertRequest,
} from "@/types/teams-incoming-webhook-connection";

// Friendly label and explanatory copy per canonical trigger event type. The keys must mirror
// `ArchLucid.Core.Notifications.Teams.TeamsNotificationTriggerCatalog.All` (server-side source of truth).
const TRIGGER_DESCRIPTIONS: Record<string, { label: string; helpText: string }> = {
  "com.archlucid.authority.run.completed": {
    label: "Run committed",
    helpText: "An architecture run produced a committed manifest (operator UI: run page).",
  },
  "com.archlucid.governance.approval.submitted": {
    label: "Governance approval requested",
    helpText: "A governance approval request was raised and awaits review (operator UI: approvals).",
  },
  "com.archlucid.alert.fired": {
    label: "Alert fired",
    helpText: "An alert rule matched and an alert record was opened (operator UI: alerts).",
  },
  "com.archlucid.compliance.drift.escalated": {
    label: "Compliance drift escalated",
    helpText: "A compliance drift breached its threshold and was escalated (operator UI: compliance).",
  },
  "com.archlucid.advisory.scan.completed": {
    label: "Advisory scan completed",
    helpText: "An advisory finding scan committed a fresh result (operator UI: advisory findings).",
  },
  "com.archlucid.seat.reservation.released": {
    label: "Trial seat released",
    helpText: "A trial seat reservation expired or was released, freeing capacity (operator UI: trial seats).",
  },
};

// Renders the canonical trigger label, falling back to the raw event type for forward-compat
// when the server adds a new trigger before the UI has shipped a friendly label for it.
function describeTrigger(eventType: string): { label: string; helpText: string } {
  return (
    TRIGGER_DESCRIPTIONS[eventType] ?? {
      label: eventType,
      helpText: `Custom or newly added trigger (${eventType}).`,
    }
  );
}

export default function TeamsNotificationsIntegrationPage() {
  const canMutate = useEnterpriseMutationCapability();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [conn, setConn] = useState<TeamsIncomingWebhookConnectionResponse | null>(null);
  const [secretName, setSecretName] = useState("");
  const [label, setLabel] = useState("");
  const [catalog, setCatalog] = useState<string[]>([]);
  const [enabledTriggers, setEnabledTriggers] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const [data, triggers] = await Promise.all([
        getTeamsIncomingWebhookConnection(),
        getTeamsNotificationTriggerCatalog(),
      ]);
      setConn(data);
      setSecretName(data.keyVaultSecretName ?? "");
      setLabel(data.label ?? "");
      setCatalog(triggers);
      setEnabledTriggers(new Set(data.enabledTriggers ?? triggers));
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function toggleTrigger(eventType: string, checked: boolean) {
    setEnabledTriggers((prev) => {
      const next = new Set(prev);
      if (checked) {
        next.add(eventType);
      } else {
        next.delete(eventType);
      }
      return next;
    });
  }

  async function onSave() {
    if (!canMutate) {
      return;
    }

    setSaving(true);
    setFailure(null);
    try {
      // Preserve the catalog ordering when sending so the diff in the audit log is deterministic.
      const orderedTriggers = catalog.filter((t) => enabledTriggers.has(t));
      const body: TeamsIncomingWebhookConnectionUpsertRequest = {
        keyVaultSecretName: secretName.trim(),
        label: label.trim().length > 0 ? label.trim() : null,
        enabledTriggers: orderedTriggers,
      };
      const saved = await upsertTeamsIncomingWebhookConnection(body);
      setConn(saved);
      setEnabledTriggers(new Set(saved.enabledTriggers));
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSaving(false);
    }
  }

  async function onRemove() {
    if (!canMutate) {
      return;
    }

    setSaving(true);
    setFailure(null);
    try {
      await deleteTeamsIncomingWebhookConnection();
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <LayerHeader pageKey="teams-notifications" />

      <div>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Microsoft Teams</h1>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
          Register the{" "}
          <strong>Key Vault secret name</strong> that holds your Teams incoming webhook URL. ArchLucid never stores the
          webhook URL in SQL — Logic Apps or workers resolve the secret at delivery time. See{" "}
          <Link
            className="text-blue-700 underline dark:text-blue-300"
            href="https://github.com/joefrancisGA/ArchLucid/blob/main/docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md"
          >
            MICROSOFT_TEAMS_NOTIFICATIONS.md
          </Link>
          .
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

      {loading || !conn ? (
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading…</p>
      ) : (
        <div className="space-y-4 rounded-lg border border-neutral-200 p-4 dark:border-neutral-800">
          <p className="text-sm text-neutral-700 dark:text-neutral-300">
            Status:{" "}
            <span className="font-medium">{conn.isConfigured ? "Configured (Key Vault reference)" : "Not configured"}</span>
            {conn.isConfigured ? (
              <span className="text-neutral-500 dark:text-neutral-400">
                {" "}
                — updated {new Date(conn.updatedUtc).toLocaleString()}
              </span>
            ) : null}
          </p>

          <div className="space-y-2">
            <Label htmlFor="kv-secret">Key Vault secret name</Label>
            <Input
              id="kv-secret"
              name="keyVaultSecretName"
              value={secretName}
              onChange={(e) => setSecretName(e.target.value)}
              disabled={!canMutate || saving}
              autoComplete="off"
              placeholder="e.g. teams-incoming-webhook-prod"
            />
            <p className="text-xs text-neutral-500 dark:text-neutral-400">
              Must not be a raw URL (entries containing :// are rejected by the API).
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="teams-label">Label (optional)</Label>
            <Input
              id="teams-label"
              name="label"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              disabled={!canMutate || saving}
              autoComplete="off"
              placeholder="Channel or team name"
            />
          </div>

          <fieldset className="space-y-2">
            <legend className="text-sm font-medium text-neutral-900 dark:text-neutral-100">
              Notification triggers
            </legend>
            <p className="text-xs text-neutral-500 dark:text-neutral-400">
              Select which integration events fan out to this Teams channel. The Logic Apps workflow filters server-side
              before delivery, so disabled triggers cannot reach the channel even if upstream routing misbehaves.
            </p>

            <ul className="space-y-2">
              {catalog.map((eventType) => {
                const description = describeTrigger(eventType);
                const checkboxId = `trigger-${eventType.replace(/\./g, "-")}`;
                const checked = enabledTriggers.has(eventType);
                return (
                  <li key={eventType} className="flex items-start gap-2">
                    <input
                      id={checkboxId}
                      type="checkbox"
                      checked={checked}
                      onChange={(e) => toggleTrigger(eventType, e.target.checked)}
                      disabled={!canMutate || saving}
                      className="mt-1 h-4 w-4 rounded border-neutral-300 text-blue-700 focus:ring-blue-500 dark:border-neutral-700"
                      aria-describedby={`${checkboxId}-help`}
                    />
                    <div className="flex-1">
                      <Label htmlFor={checkboxId} className="font-medium">
                        {description.label}
                      </Label>
                      <p id={`${checkboxId}-help`} className="text-xs text-neutral-500 dark:text-neutral-400">
                        {description.helpText}
                      </p>
                      <p className="font-mono text-[10px] text-neutral-400 dark:text-neutral-500">{eventType}</p>
                    </div>
                  </li>
                );
              })}
            </ul>
          </fieldset>

          <div className="flex flex-wrap gap-2">
            <Button type="button" onClick={() => void onSave()} disabled={!canMutate || saving || secretName.trim() === ""}>
              Save reference
            </Button>
            <Button type="button" variant="outline" onClick={() => void onRemove()} disabled={!canMutate || saving || !conn.isConfigured}>
              Remove reference
            </Button>
          </div>

          {!canMutate ? (
            <p className="text-xs text-neutral-600 dark:text-neutral-400">
              Your role can view this page; saving requires Execute authority (same floor as other Enterprise mutation
              surfaces).
            </p>
          ) : null}
        </div>
      )}
    </div>
  );
}
