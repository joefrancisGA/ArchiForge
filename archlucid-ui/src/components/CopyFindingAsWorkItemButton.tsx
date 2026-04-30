"use client";

import { Check, ClipboardList } from "lucide-react";
import { useCallback, useState } from "react";

import {
  findingInspectNarrativeFields,
  findingInspectPrimaryLabels,
} from "@/lib/finding-display-from-inspect";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  type WorkItemClipboardFormat,
  buildInspectFindingWorkItemBody,
  buildTraceRowWorkItemBody,
  writeWorkItemBodyToClipboard,
  type FindingWorkItemBuildInput,
} from "@/lib/copy-finding-as-work-item";
import { showError, showSuccess } from "@/lib/toast";
import type { FindingInspectPayload } from "@/types/finding-inspect";
import type { FindingTraceConfidenceDto } from "@/types/explanation";

/** Target system for pasted body text (Markdown family shares the same builders today). */
const FORMAT_ITEMS: readonly { readonly value: WorkItemClipboardFormat; readonly label: string }[] = [
  { value: "markdown", label: "Markdown" },
  { value: "githubMarkdown", label: "GitHub Issues" },
  { value: "azureDevOpsMarkdown", label: "Azure Boards" },
  { value: "jiraWiki", label: "Jira (wiki)" },
] as const;

function evidenceLinesFromInspectPayload(payload: FindingInspectPayload): string[] {
  return payload.evidence.map((e) => {
    const base = e.excerpt?.trim() ?? e.artifactId?.trim() ?? "";
    const lr = e.lineRange?.trim();

    if (base.length === 0 && (lr === undefined || lr.length === 0)) {
      return "Not available";
    }

    if (lr !== undefined && lr.length > 0) {
      return `${base.length > 0 ? base : "(citation)"} (${lr})`;
    }

    return base;
  });
}

function buildFindingWorkItemInput(
  runId: string,
  findingId: string,
  siteOrigin: string,
  payload: FindingInspectPayload,
): FindingWorkItemBuildInput {
  const labels = findingInspectPrimaryLabels(payload);
  const narrative = findingInspectNarrativeFields(payload);

  return {
    runId,
    findingId,
    siteOrigin,
    severityLabel: labels.severityLabel,
    categoryLabel: labels.categoryLabel,
    impactedAreaLabel: labels.impactedAreaLabel,
    title: narrative.title,
    description: narrative.description,
    recommendedAction: labels.recommendedAction,
    decisionRuleId: payload.decisionRuleId,
    decisionRuleName: payload.decisionRuleName,
    evidenceExcerpts: evidenceLinesFromInspectPayload(payload),
  };
}

export type CopyFindingAsWorkItemButtonProps = {
  runId: string;
  findingId: string;
  payload: FindingInspectPayload;
};

/**
 * Copies a structured work-item body for Jira, GitHub, or Azure Boards from the finding inspect payload.
 */
export function CopyFindingAsWorkItemButton({ runId, findingId, payload }: CopyFindingAsWorkItemButtonProps) {
  const [format, setFormat] = useState<WorkItemClipboardFormat>("markdown");
  const [copied, setCopied] = useState(false);

  const onCopy = useCallback(async () => {
    const siteOrigin = typeof window !== "undefined" ? window.location.origin : "";
    const input = buildFindingWorkItemInput(runId, findingId, siteOrigin, payload);
    const text = buildInspectFindingWorkItemBody(format, input);
    const ok = await writeWorkItemBodyToClipboard(text);

    if (!ok) {
      showError("Could not copy to clipboard");

      return;
    }

    showSuccess("Copied work item to clipboard");
    setCopied(true);
    window.setTimeout(() => {
      setCopied(false);
    }, 2_000);
  }, [findingId, format, payload, runId]);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select
        value={format}
        onValueChange={(v) => {
          setFormat(v as WorkItemClipboardFormat);
        }}
      >
        <SelectTrigger className="h-8 w-[11.5rem] text-xs" aria-label="Work item format">
          <SelectValue placeholder="Format" />
        </SelectTrigger>
        <SelectContent>
          {FORMAT_ITEMS.map((item) => (
            <SelectItem key={item.value} value={item.value} className="text-xs">
              {item.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Button
        type="button"
        variant="secondary"
        size="sm"
        className="h-8 gap-1.5 text-xs"
        aria-label="Create remediation ticket — copy formatted text to clipboard"
        onClick={() => {
          void onCopy();
        }}
      >
        {copied ? <Check className="size-3.5 text-emerald-600" aria-hidden /> : <ClipboardList className="size-3.5" aria-hidden />}
        {copied ? "Copied" : "Create remediation ticket"}
      </Button>
    </div>
  );
}

export type CopyTraceRowWorkItemButtonProps = {
  runId: string;
  row: FindingTraceConfidenceDto;
};

/**
 * Minimal copy for [`RunFindingExplainabilityTable`](/components/RunFindingExplainabilityTable) rows (aggregate trace list).
 */
export function CopyTraceRowWorkItemButton({ runId, row }: CopyTraceRowWorkItemButtonProps) {
  const [format, setFormat] = useState<WorkItemClipboardFormat>("markdown");
  const [copied, setCopied] = useState(false);

  const onCopy = useCallback(async () => {
    const siteOrigin = typeof window !== "undefined" ? window.location.origin : "";
    const text = buildTraceRowWorkItemBody(format, {
      runId,
      findingId: row.findingId,
      findingTitle: row.findingTitle ?? null,
      ruleId: row.ruleId ?? null,
      siteOrigin,
    });
    const ok = await writeWorkItemBodyToClipboard(text);

    if (!ok) {
      showError("Could not copy to clipboard");

      return;
    }

    showSuccess("Copied work item stub to clipboard");
    setCopied(true);
    window.setTimeout(() => {
      setCopied(false);
    }, 2_000);
  }, [format, row.findingId, row.findingTitle, row.ruleId, runId]);

  return (
    <div className="flex min-w-0 flex-col gap-1 sm:flex-row sm:items-center">
      <Select
        value={format}
        onValueChange={(v) => {
          setFormat(v as WorkItemClipboardFormat);
        }}
      >
        <SelectTrigger className="h-7 w-[9.5rem] text-[0.65rem]" aria-label="Work item format for this row">
          <SelectValue placeholder="Format" />
        </SelectTrigger>
        <SelectContent>
          {FORMAT_ITEMS.map((item) => (
            <SelectItem key={item.value} value={item.value} className="text-xs">
              {item.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Button
        type="button"
        size="sm"
        variant="outline"
        className="h-7 gap-1 px-2 text-[0.65rem]"
        aria-label="Copy finding as work item to clipboard"
        onClick={() => {
          void onCopy();
        }}
      >
        {copied ? <Check className="size-3 text-emerald-600" aria-hidden /> : <ClipboardList className="size-3" aria-hidden />}
        Copy item
      </Button>
    </div>
  );
}
