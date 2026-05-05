import type { ManifestSummary } from "@/types/authority";

export type GoldenManifestMarkdownOptions = {
  /** Echoed in metadata. */
  runId?: string | null;
  /** When full manifest JSON is missing, format a short summary from API manifest summary instead. */
  manifestSummaryFallback?: ManifestSummary | null;
};

function isRecord(data: unknown): data is Record<string, unknown> {
  return typeof data === "object" && data !== null && !Array.isArray(data);
}

/**
 * Returns true when the run-detail `goldenManifest` payload looks like a real document rather than a placeholder.
 */
export function isUsableGoldenManifestExportJson(data: unknown): boolean {
  if (!isRecord(data)) {
    return false;
  }

  const keys = Object.keys(data);

  if (keys.length === 0) {
    return false;
  }

  if (keys.length === 1 && data.demo === true) {
    return false;
  }

  return true;
}

function normalizeInlineText(value: unknown): string | null {
  if (typeof value !== "string") {
    return null;
  }

  const t = value.replace(/\s+/g, " ").trim();

  if (t.length === 0) {
    return null;
  }

  return t;
}

function pushBulletLines(target: string[], items: unknown, labelForEmpty?: string): void {
  if (!Array.isArray(items) || items.length === 0) {
    if (labelForEmpty) {
      target.push(`- ${labelForEmpty}`);
    }

    return;
  }

  for (const item of items) {
    const line = normalizeInlineText(item);

    if (line) {
      target.push(`- ${line}`);
    }
  }
}

function formatSandboxStyleGoldenManifest(m: Record<string, unknown>): string {
  const lines: string[] = [];
  const title =
    normalizeInlineText(m.systemName) ??
    normalizeInlineText(m.manifestVersion) ??
    "Golden manifest";
  const env = normalizeInlineText(m.environment);
  const cloud = normalizeInlineText(m.cloudProvider);
  const status = normalizeInlineText(m.status);

  lines.push(`# ${title}`);
  lines.push("");

  if (env || cloud || status) {
    lines.push("## Document metadata");
    lines.push("");

    if (env) {
      lines.push(`- **Environment:** ${env}`);
    }

    if (cloud) {
      lines.push(`- **Cloud:** ${cloud}`);
    }

    if (status) {
      lines.push(`- **Status:** ${status}`);
    }

    lines.push("");
  }

  lines.push("## Objectives");
  lines.push("");

  const summary = isRecord(m.summary) ? m.summary : null;

  if (summary) {
    const decisionCount = summary.decisionCount;
    const warningCount = summary.warningCount;
    const unresolved = summary.unresolvedIssueCount;
    const costPosture = normalizeInlineText(summary.costPosture);

    if (typeof decisionCount === "number") {
      lines.push(`- **Decisions recorded:** ${decisionCount}`);
    }

    if (typeof warningCount === "number") {
      lines.push(`- **Warnings:** ${warningCount}`);
    }

    if (typeof unresolved === "number") {
      lines.push(`- **Unresolved issues:** ${unresolved}`);
    }

    if (costPosture) {
      lines.push(`- **Cost posture:** ${costPosture}`);
    }
  } else {
    lines.push("_No summary object was present in the manifest JSON._");
  }

  lines.push("");

  lines.push("## Architecture overview");
  lines.push("");
  lines.push(
    "High-level decisions and patterns captured in this manifest (from embedded highlights).",
  );
  lines.push("");

  lines.push("## Component breakdown");
  lines.push("");

  const highlights = Array.isArray(m.highlights) ? m.highlights : [];

  if (highlights.length === 0) {
    lines.push("_No highlights were present._");
  } else {
    for (const h of highlights) {
      if (!isRecord(h)) {
        continue;
      }

      const hid = normalizeInlineText(h.decisionId);
      const hTitle = normalizeInlineText(h.title);
      const category = normalizeInlineText(h.category);
      const disposition = normalizeInlineText(h.disposition);
      const rationale = normalizeInlineText(h.rationale);

      if (hTitle) {
        lines.push(`### ${hTitle}`);
        lines.push("");
      }

      if (hid) {
        lines.push(`- **Decision id:** \`${hid}\``);
      }

      if (category) {
        lines.push(`- **Category:** ${category}`);
      }

      if (disposition) {
        lines.push(`- **Disposition:** ${disposition}`);
      }

      if (rationale) {
        lines.push("");
        lines.push(rationale);
      }

      lines.push("");
    }
  }

  lines.push("## Security model");
  lines.push("");

  const warnings = Array.isArray(m.warnings) ? m.warnings : [];

  if (warnings.length === 0) {
    lines.push("_No warnings were listed on the manifest._");
    lines.push("");
  } else {
    for (const w of warnings) {
      if (typeof w === "string") {
        const text = normalizeInlineText(w);

        if (text) {
          lines.push(`- ${text}`);
        }

        continue;
      }

      if (!isRecord(w)) {
        continue;
      }

      const code = normalizeInlineText(w.code);
      const message = normalizeInlineText(w.message);

      if (code && message) {
        lines.push(`- **${code}:** ${message}`);
      } else if (message) {
        lines.push(`- ${message}`);
      }
    }

    lines.push("");
  }

  return lines.join("\n").trimEnd() + "\n";
}

function formatRequirementItems(items: unknown, heading: string, lines: string[]): void {
  if (!Array.isArray(items) || items.length === 0) {
    return;
  }

  lines.push(`### ${heading}`);
  lines.push("");

  for (const raw of items) {
    if (!isRecord(raw)) {
      continue;
    }

    const name = normalizeInlineText(raw.requirementName);
    const text = normalizeInlineText(raw.requirementText);
    const status = normalizeInlineText(raw.coverageStatus);

    if (name) {
      lines.push(`- **${name}**`);
    }

    if (status) {
      lines.push(`  - Coverage: ${status}`);
    }

    if (text) {
      lines.push(`  - ${text}`);
    }
  }

  lines.push("");
}

function formatPolicySection(policy: Record<string, unknown> | null, lines: string[]): void {
  if (policy === null) {
    return;
  }

  pushBulletLines(lines, policy.notes, undefined);

  const satisfied = policy.satisfiedControls;
  const violations = policy.violations;

  if (Array.isArray(satisfied) && satisfied.length > 0) {
    lines.push("### Policy — satisfied controls");
    lines.push("");

    for (const c of satisfied) {
      if (!isRecord(c)) {
        continue;
      }

      const id = normalizeInlineText(c.controlId);
      const name = normalizeInlineText(c.controlName);
      const label = [id, name].filter(Boolean).join(" — ");

      if (label) {
        lines.push(`- ${label}`);
      }
    }

    lines.push("");
  }

  if (Array.isArray(violations) && violations.length > 0) {
    lines.push("### Policy — violations");
    lines.push("");

    for (const c of violations) {
      if (!isRecord(c)) {
        continue;
      }

      const id = normalizeInlineText(c.controlId);
      const name = normalizeInlineText(c.controlName);
      const label = [id, name].filter(Boolean).join(" — ");

      if (label) {
        lines.push(`- ${label}`);
      }
    }

    lines.push("");
  }
}

function formatManifestDocumentShape(m: Record<string, unknown>): string {
  const lines: string[] = [];
  const meta = isRecord(m.metadata) ? m.metadata : null;
  const topology = isRecord(m.topology) ? m.topology : null;
  const security = isRecord(m.security) ? m.security : null;
  const requirements = isRecord(m.requirements) ? m.requirements : null;
  const constraints = isRecord(m.constraints) ? m.constraints : null;
  const cost = isRecord(m.cost) ? m.cost : null;
  const compliance = isRecord(m.compliance) ? m.compliance : null;
  const policy = isRecord(m.policy) ? m.policy : null;

  const manifestId = normalizeInlineText(m.manifestId);
  const runId = normalizeInlineText(m.runId);
  const ruleSetId = normalizeInlineText(m.ruleSetId);
  const ruleSetVersion = normalizeInlineText(m.ruleSetVersion);
  const manifestHash = normalizeInlineText(m.manifestHash);
  const changeDescription = meta ? normalizeInlineText(meta.changeDescription) : null;
  const manifestVersion = meta ? normalizeInlineText(meta.manifestVersion) : null;

  const titleBase = changeDescription ?? manifestVersion ?? "Architecture manifest";

  lines.push(`# ${titleBase}`);
  lines.push("");

  lines.push("## Document metadata");
  lines.push("");

  if (manifestId) {
    lines.push(`- **Manifest id:** \`${manifestId}\``);
  }

  if (runId) {
    lines.push(`- **Run id:** \`${runId}\``);
  }

  if (ruleSetId && ruleSetVersion) {
    lines.push(`- **Policy pack:** ${ruleSetId} @ ${ruleSetVersion}`);
  } else if (ruleSetId) {
    lines.push(`- **Policy pack:** ${ruleSetId}`);
  }

  if (manifestHash) {
    lines.push(`- **Manifest hash:** \`${manifestHash}\``);
  }

  if (manifestVersion) {
    lines.push(`- **Manifest version:** ${manifestVersion}`);
  }

  lines.push("");

  lines.push("## Objectives");
  lines.push("");

  if (changeDescription) {
    lines.push(changeDescription);
    lines.push("");
  }

  if (requirements !== null) {
    formatRequirementItems(requirements.covered, "Covered requirements", lines);
    formatRequirementItems(requirements.uncovered, "Uncovered requirements", lines);
  }

  formatPolicySection(policy, lines);

  if (
    !changeDescription &&
    requirements === null &&
    policy === null
  ) {
    lines.push("_No explicit objectives were present on the manifest document._");
    lines.push("");
  }

  lines.push("## Architecture overview");
  lines.push("");

  pushBulletLines(lines, m.assumptions, "_No assumptions listed._");

  lines.push("");

  if (constraints !== null) {
    lines.push("### Constraints");
    lines.push("");
    pushBulletLines(lines, constraints.mandatoryConstraints, "_No mandatory constraints._");
    pushBulletLines(lines, constraints.preferences, undefined);
    lines.push("");
  }

  if (topology !== null) {
    lines.push("### Topology");
    lines.push("");
    pushBulletLines(lines, topology.selectedPatterns, undefined);
    pushBulletLines(lines, topology.resources, undefined);
    pushBulletLines(lines, topology.gaps, undefined);
    lines.push("");
  }

  if (cost !== null) {
    lines.push("### Cost");
    lines.push("");
    pushBulletLines(lines, cost.notes, undefined);
    pushBulletLines(lines, cost.costRisks, undefined);

    if (typeof cost.maxMonthlyCost === "number") {
      lines.push(`- **Max monthly cost (estimate):** ${cost.maxMonthlyCost}`);
    }

    lines.push("");
  }

  if (compliance !== null) {
    lines.push("### Compliance");
    lines.push("");
    const controls = compliance.controls;

    if (Array.isArray(controls)) {
      for (const c of controls) {
        if (!isRecord(c)) {
          continue;
        }

        const name = normalizeInlineText(c.controlName);
        const status = normalizeInlineText(c.status);

        if (name && status) {
          lines.push(`- **${name}:** ${status}`);
        } else if (name) {
          lines.push(`- ${name}`);
        }
      }
    }

    pushBulletLines(lines, compliance.gaps, undefined);
    lines.push("");
  }

  lines.push("## Component breakdown");
  lines.push("");

  const decisions = Array.isArray(m.decisions) ? m.decisions : [];
  const services = topology !== null && Array.isArray(topology.services) ? topology.services : [];
  const datastores =
    topology !== null && Array.isArray(topology.datastores) ? topology.datastores : [];
  const relationships =
    topology !== null && Array.isArray(topology.relationships) ? topology.relationships : [];

  if (services.length > 0) {
    lines.push("### Services");
    lines.push("");

    for (const s of services) {
      if (!isRecord(s)) {
        continue;
      }

      const name = normalizeInlineText(s.serviceName);
      const sid = normalizeInlineText(s.serviceId);
      const purpose = normalizeInlineText(s.purpose);

      if (name) {
        lines.push(`- **${name}**${sid ? ` (\`${sid}\`)` : ""}`);
      }

      if (purpose) {
        lines.push(`  - ${purpose}`);
      }
    }

    lines.push("");
  }

  if (datastores.length > 0) {
    lines.push("### Datastores");
    lines.push("");

    for (const ds of datastores) {
      if (!isRecord(ds)) {
        continue;
      }

      const name = normalizeInlineText(ds.name);
      const did = normalizeInlineText(ds.datastoreId);

      if (name) {
        lines.push(`- **${name}**${did ? ` (\`${did}\`)` : ""}`);
      }
    }

    lines.push("");
  }

  if (relationships.length > 0) {
    lines.push("### Relationships");
    lines.push("");

    for (const r of relationships) {
      if (!isRecord(r)) {
        continue;
      }

      const desc = normalizeInlineText(r.description);
      const relId = normalizeInlineText(r.relationshipId);
      const src = normalizeInlineText(r.sourceId);
      const tgt = normalizeInlineText(r.targetId);
      const parts = [src, tgt].filter(Boolean).join(" → ");

      if (desc) {
        lines.push(`- ${desc}${relId ? ` (\`${relId}\`)` : ""}`);
      } else if (parts) {
        lines.push(`- ${parts}${relId ? ` (\`${relId}\`)` : ""}`);
      }
    }

    lines.push("");
  }

  if (decisions.length > 0) {
    lines.push("### Architecture decisions");
    lines.push("");

    for (const d of decisions) {
      if (!isRecord(d)) {
        continue;
      }

      const dTitle = normalizeInlineText(d.title);
      const did = normalizeInlineText(d.decisionId);
      const category = normalizeInlineText(d.category);
      const rationale = normalizeInlineText(d.rationale);
      const option = normalizeInlineText(d.selectedOption);

      if (dTitle) {
        lines.push(`#### ${dTitle}`);
        lines.push("");
      }

      if (did) {
        lines.push(`- **Decision id:** \`${did}\``);
      }

      if (category) {
        lines.push(`- **Category:** ${category}`);
      }

      if (option) {
        lines.push(`- **Selected option:** ${option}`);
      }

      if (rationale) {
        lines.push("");
        lines.push(rationale);
      }

      lines.push("");
    }
  }

  if (
    services.length === 0 &&
    datastores.length === 0 &&
    relationships.length === 0 &&
    decisions.length === 0
  ) {
    lines.push("_No services, datastores, relationships, or decisions were present._");
    lines.push("");
  }

  lines.push("## Security model");
  lines.push("");

  if (security !== null) {
    const controls = Array.isArray(security.controls) ? security.controls : [];

    if (controls.length > 0) {
      lines.push("### Controls");
      lines.push("");

      for (const c of controls) {
        if (!isRecord(c)) {
          continue;
        }

        const cname = normalizeInlineText(c.controlName);
        const status = normalizeInlineText(c.status);
        const impact = normalizeInlineText(c.impact);

        if (cname) {
          lines.push(`- **${cname}**${status ? ` — ${status}` : ""}${impact ? ` (${impact})` : ""}`);
        }
      }

      lines.push("");
    }

    pushBulletLines(lines, security.gaps, undefined);
  }

  pushBulletLines(lines, m.warnings, undefined);

  return lines.join("\n").trimEnd() + "\n";
}

function formatManifestSummaryFallback(summary: ManifestSummary, runId?: string | null): string {
  const lines: string[] = [];

  lines.push(`# Architecture manifest summary`);
  lines.push("");

  lines.push("## Document metadata");
  lines.push("");

  if (runId) {
    lines.push(`- **Run id:** \`${runId}\``);
  }

  lines.push(`- **Manifest id:** \`${summary.manifestId}\``);
  lines.push(`- **Status:** ${summary.status}`);
  lines.push(`- **Policy pack:** ${summary.ruleSetId} @ ${summary.ruleSetVersion}`);
  lines.push(`- **Manifest hash:** \`${summary.manifestHash}\``);
  lines.push("");

  lines.push("## Objectives");
  lines.push("");

  if (summary.operatorSummary) {
    lines.push(summary.operatorSummary);
  } else {
    lines.push("_No operator summary was returned by the API._");
  }

  lines.push("");

  lines.push("## Architecture overview");
  lines.push("");
  lines.push(
    "Full manifest JSON was not available in the browser session; this export contains summary counts only.",
  );
  lines.push("");
  lines.push(`- **Decisions:** ${summary.decisionCount}`);
  lines.push(`- **Warnings:** ${summary.warningCount}`);
  lines.push(`- **Unresolved issues:** ${summary.unresolvedIssueCount}`);
  lines.push("");

  lines.push("## Component breakdown");
  lines.push("");
  lines.push("_Unavailable without full manifest JSON._");
  lines.push("");

  lines.push("## Security model");
  lines.push("");
  lines.push("_Unavailable without full manifest JSON._");
  lines.push("");

  return lines.join("\n");
}

function looksLikeSandboxGoldenManifest(m: Record<string, unknown>): boolean {
  return (
    Array.isArray(m.highlights) &&
    isRecord(m.summary) &&
    typeof m.schemaVersion === "string"
  );
}

/**
 * Renders a readable Markdown summary from a golden manifest JSON value (ManifestDocument or legacy sandbox shapes).
 */
export function formatGoldenManifestMarkdown(
  goldenManifestJson: unknown,
  options?: GoldenManifestMarkdownOptions,
): string {
  if (isUsableGoldenManifestExportJson(goldenManifestJson)) {
    const m = goldenManifestJson as Record<string, unknown>;

    if (looksLikeSandboxGoldenManifest(m)) {
      return formatSandboxStyleGoldenManifest(m);
    }

    return formatManifestDocumentShape(m);
  }

  if (options?.manifestSummaryFallback) {
    return formatManifestSummaryFallback(options.manifestSummaryFallback, options.runId ?? null);
  }

  return (
    `# Golden manifest export\n\n` +
    `Manifest JSON was not available and no summary fallback was provided.\n`
  );
}

export function buildGoldenManifestMarkdownFilename(runId: string, manifestId?: string | null): string {
  const safe = (s: string): string =>
    s
      .trim()
      .replace(/[^a-zA-Z0-9._-]+/g, "-")
      .replace(/-+/g, "-")
      .replace(/^-|-$/g, "")
      .slice(0, 120);

  const primary = safe(runId.length > 0 ? runId : manifestId ?? "manifest");

  return `golden-manifest-${primary || "export"}.md`;
}

/**
 * Triggers a one-shot download of Markdown content in the browser.
 */
export function triggerGoldenManifestMarkdownDownload(markdown: string, filename: string): void {
  const blob = new Blob([markdown], { type: "text/markdown;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = filename;
  anchor.rel = "noopener";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
