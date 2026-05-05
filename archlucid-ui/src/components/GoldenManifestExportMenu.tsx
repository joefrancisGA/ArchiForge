"use client";

import { useState } from "react";

import {
  buildGoldenManifestMarkdownFilename,
  formatGoldenManifestMarkdown,
  isUsableGoldenManifestExportJson,
  triggerGoldenManifestMarkdownDownload,
} from "@/lib/export-markdown";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { ManifestSummary } from "@/types/authority";

export type GoldenManifestExportMenuProps = {
  runId: string;
  manifestId: string;
  goldenManifestJson: unknown | null;
  manifestSummary: ManifestSummary | null;
};

/**
 * Export menu for reviewed (golden) manifest artifacts on run detail — Markdown is generated entirely in the browser.
 */
export function GoldenManifestExportMenu(props: GoldenManifestExportMenuProps) {
  const { runId, manifestId, goldenManifestJson, manifestSummary } = props;
  const [exportMenuKey, setExportMenuKey] = useState(0);

  const canExport: boolean =
    isUsableGoldenManifestExportJson(goldenManifestJson) || manifestSummary !== null;

  if (!canExport) {
    return null;
  }

  return (
    <Select
      key={exportMenuKey}
      onValueChange={(value: string) => {
        if (value !== "markdown-summary") {
          return;
        }

        const markdown: string = formatGoldenManifestMarkdown(goldenManifestJson, {
          runId,
          manifestSummaryFallback: manifestSummary,
        });

        const filename: string = buildGoldenManifestMarkdownFilename(runId, manifestId);

        triggerGoldenManifestMarkdownDownload(markdown, filename);
        setExportMenuKey((k: number) => k + 1);
      }}
    >
      <SelectTrigger className="h-9 w-[10rem]" aria-label="Export reviewed manifest">
        <SelectValue placeholder="Export" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="markdown-summary">Markdown summary</SelectItem>
      </SelectContent>
    </Select>
  );
}
