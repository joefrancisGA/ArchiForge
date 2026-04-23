"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";

import { EnterpriseControlsExecutePageHint } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { useNavSurface } from "@/lib/use-nav-surface";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import { PolicyPackDiffView } from "@/components/PolicyPackDiffView";
import {
  assignPolicyPack,
  createPolicyPack,
  getEffectivePolicyContent,
  getEffectivePolicyPacks,
  listPolicyPackVersions,
  listPolicyPacks,
  publishPolicyPackVersion,
} from "@/lib/api";
import {
  enterpriseMutationControlDisabledTitle,
  policyPacksAssignButtonLabelReaderRank,
  policyPacksCompareVersionsIntroOperator,
  policyPacksCompareVersionsIntroReader,
  policyPacksCompareVersionsReaderSubline,
  policyPacksCurrentPacksHeadingOperator,
  policyPacksCurrentPacksHeadingReader,
  policyPacksCreatePackButtonLabelReaderRank,
  policyPacksHideDiffButtonTitle,
  policyPacksEmptyScopeOperatorLine,
  policyPacksEmptyScopeReaderLine,
  policyPacksLifecycleLeadReaderLine,
  policyPacksPackContentHeadingOperator,
  policyPacksPackContentHeadingReader,
  policyPacksPackSelectReaderTitle,
  policyPacksPageLeadOperator,
  policyPacksPageLeadReader,
  policyPacksPublishedVersionsEmptyOperatorLine,
  policyPacksPublishedVersionsEmptyReaderLine,
  policyPacksPublishButtonLabelReaderRank,
  policyPacksRefreshAssistReaderLine,
  policyPacksShowDiffButtonLabelReaderRank,
  policyPacksShowDiffButtonReaderTitle,
} from "@/lib/enterprise-controls-context-copy";
import { cn } from "@/lib/utils";
import { showSuccess } from "@/lib/toast";
import type {
  EffectivePolicyPackSet,
  PolicyPack,
  PolicyPackContentDocument,
  PolicyPackVersion,
} from "@/types/policy-packs";

const PACK_TYPES = [
  { value: "BuiltIn", label: "Built-in template" },
  { value: "TenantCustom", label: "Tenant custom" },
  { value: "WorkspaceCustom", label: "Workspace custom" },
  { value: "ProjectCustom", label: "Project custom" },
];

const DEFAULT_CONTENT = `{
  "complianceRuleIds": [],
  "complianceRuleKeys": [],
  "alertRuleIds": [],
  "compositeAlertRuleIds": [],
  "advisoryDefaults": {},
  "metadata": {}
}`;

const VERTICAL_POLICY_PACK_IMPORTS: ReadonlyArray<{ slug: string; label: string }> = [
  { slug: "financial-services", label: "Financial services" },
  { slug: "healthcare", label: "Healthcare" },
  { slug: "retail", label: "Retail / PCI" },
  { slug: "saas", label: "SaaS / SOC 2" },
  { slug: "public-sector", label: "Public sector (EU)" },
];

export default function PolicyPacksPage() {
  const canMutatePacks = useNavSurface("policy-packs").mutationCapability;
  const [packs, setPacks] = useState<PolicyPack[]>([]);
  const [effective, setEffective] = useState<EffectivePolicyPackSet | null>(null);
  const [effectiveContent, setEffectiveContent] = useState<PolicyPackContentDocument | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Baseline governance");
  const [description, setDescription] = useState("");
  const [packType, setPackType] = useState("ProjectCustom");
  const [createJson, setCreateJson] = useState(DEFAULT_CONTENT);

  const [selectedPackId, setSelectedPackId] = useState("");
  const [publishVersion, setPublishVersion] = useState("1.0.0");
  const [publishJson, setPublishJson] = useState(DEFAULT_CONTENT);

  const [assignVersion, setAssignVersion] = useState("1.0.0");
  const [assignScopeLevel, setAssignScopeLevel] = useState("Project");
  const [assignPinned, setAssignPinned] = useState(false);

  const [packVersions, setPackVersions] = useState<PolicyPackVersion[]>([]);
  const [compareLeftId, setCompareLeftId] = useState("");
  const [compareRightId, setCompareRightId] = useState("");
  const [showVersionDiff, setShowVersionDiff] = useState(false);
  const [verticalImportSlug, setVerticalImportSlug] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const [p, eff, doc] = await Promise.all([
        listPolicyPacks(),
        getEffectivePolicyPacks(),
        getEffectivePolicyContent(),
      ]);
      setPacks(p);
      setEffective(eff);
      setEffectiveContent(doc);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (packs.length > 0 && !selectedPackId) {
      setSelectedPackId(packs[0]!.policyPackId);
    }
  }, [packs, selectedPackId]);

  useEffect(() => {
    if (!selectedPackId) {
      setPackVersions([]);
      setCompareLeftId("");
      setCompareRightId("");
      setShowVersionDiff(false);

      return;
    }

    void (async () => {
      try {
        const versions = await listPolicyPackVersions(selectedPackId);
        setPackVersions(versions);
        const latest = versions[0];

        if (latest) {
          setPublishVersion(latest.version);
          setPublishJson(latest.contentJson || DEFAULT_CONTENT);
          setAssignVersion(latest.version);
        }

        if (versions.length >= 2) {
          setCompareLeftId(versions[1]!.policyPackVersionId);
          setCompareRightId(versions[0]!.policyPackVersionId);
        } else if (versions.length === 1) {
          setCompareLeftId(versions[0]!.policyPackVersionId);
          setCompareRightId(versions[0]!.policyPackVersionId);
        } else {
          setCompareLeftId("");
          setCompareRightId("");
        }

        setShowVersionDiff(false);
      } catch {
        setPackVersions([]);
        setCompareLeftId("");
        setCompareRightId("");
        setShowVersionDiff(false);
      }
    })();
  }, [selectedPackId]);

  async function importVerticalPolicyPack(slug: string, label: string) {
    setFailure(null);
    setVerticalImportSlug(slug);
    try {
      const response: Response = await fetch(`/vertical-templates/${slug}/policy-pack.json`);

      if (!response.ok) {
        setFailure(uiFailureFromMessage(`${label}: could not load template (HTTP ${response.status}).`));
        return;
      }

      const bodyText: string = await response.text();
      let parsed: unknown;

      try {
        parsed = JSON.parse(bodyText);
      } catch {
        setFailure(uiFailureFromMessage(`${label}: template JSON is invalid.`));
        return;
      }

      const doc = parsed as PolicyPackContentDocument;

      if (!Array.isArray(doc.complianceRuleKeys) || doc.complianceRuleKeys.length === 0) {
        setFailure(uiFailureFromMessage(`${label}: template is missing complianceRuleKeys.`));
        return;
      }

      setCreateJson(JSON.stringify(parsed, null, 2));
      const verticalKey: string = doc.metadata?.vertical ?? slug;
      setName(`${label} (${verticalKey})`);
      setDescription(`Imported vertical starter policy pack (${slug}). Review JSON before publishing.`);
      showSuccess(`${label} template loaded into the create form.`);
    } catch (e: unknown) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setVerticalImportSlug(null);
    }
  }

  async function onCreate() {
    if (!canMutatePacks) {
      return;
    }

    setFailure(null);
    try {
      JSON.parse(createJson);
    } catch {
      setFailure(uiFailureFromMessage("Create: JSON content is invalid."));
      return;
    }
    setLoading(true);
    try {
      const created: PolicyPack = await createPolicyPack({
        name: name.trim() || "Pack",
        description: description.trim(),
        packType,
        initialContentJson: createJson,
      });
      await load();
      // Do not rely only on useEffect(packs): it only runs when selectedPackId is empty, and E2E/CI can race renders.
      setSelectedPackId(created.policyPackId);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function onPublish() {
    if (!canMutatePacks) {
      return;
    }

    if (!selectedPackId) {
      setFailure(uiFailureFromMessage("Select a pack to publish."));
      return;
    }
    setFailure(null);
    try {
      JSON.parse(publishJson);
    } catch {
      setFailure(uiFailureFromMessage("Publish: JSON content is invalid."));
      return;
    }
    setLoading(true);
    try {
      await publishPolicyPackVersion(selectedPackId, {
        version: publishVersion.trim(),
        contentJson: publishJson,
      });
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  async function onAssign() {
    if (!canMutatePacks) {
      return;
    }

    if (!selectedPackId) {
      setFailure(uiFailureFromMessage("Select a pack to assign."));
      return;
    }
    setFailure(null);
    setLoading(true);
    try {
      await assignPolicyPack(selectedPackId, {
        version: assignVersion.trim(),
        scopeLevel: assignScopeLevel,
        isPinned: assignPinned,
      });
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  const compareLeftVersion = packVersions.find((v) => v.policyPackVersionId === compareLeftId);
  const compareRightVersion = packVersions.find((v) => v.policyPackVersionId === compareRightId);

  return (
    <main style={{ maxWidth: 960 }}>
      <LayerHeader pageKey="policy-packs" />
      <h2 style={{ marginTop: 0 }}>Policy packs</h2>
      <p className="mb-2 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        {canMutatePacks ? policyPacksPageLeadOperator : policyPacksPageLeadReader}{" "}
        <Link href="/governance-resolution" className="font-medium text-teal-800 underline dark:text-teal-300">
          Governance resolution
        </Link>
        .
      </p>
      <EnterpriseControlsExecutePageHint className="mb-3" />

      <div className="mb-3 flex flex-col gap-1 sm:flex-row sm:flex-wrap sm:items-center sm:gap-3">
        <Button type="button" variant="secondary" size="sm" onClick={() => void load()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </Button>
        {!canMutatePacks ? (
          <span className="max-w-prose text-xs text-neutral-500 dark:text-neutral-400">
            {policyPacksRefreshAssistReaderLine}
          </span>
        ) : null}
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

      <div className={cn("flex flex-col gap-8", !canMutatePacks && "flex-col-reverse")}>
      <section style={{ marginBottom: 32 }} aria-labelledby="policy-packs-current-heading">
        <h3 id="policy-packs-current-heading">
          {canMutatePacks ? policyPacksCurrentPacksHeadingOperator : policyPacksCurrentPacksHeadingReader}
        </h3>
        {packs.length === 0 ? (
          <p style={{ color: "#666", maxWidth: "42rem", fontSize: 14 }}>
            {canMutatePacks ? policyPacksEmptyScopeOperatorLine : policyPacksEmptyScopeReaderLine}
          </p>
        ) : (
          <ul>
            {packs.map((p) => (
              <li key={p.policyPackId}>
                <strong>{p.name}</strong> — {p.packType} / {p.status} / current{" "}
                <code>{p.currentVersion}</code>
                <div style={{ fontSize: 13, color: "#555" }}>{p.description}</div>
              </li>
            ))}
          </ul>
        )}

        <label style={{ display: "block", marginTop: 12 }}>
          Selected pack (inspect versions and lifecycle)
          <select
            value={selectedPackId}
            onChange={(e) => setSelectedPackId(e.target.value)}
            title={canMutatePacks ? undefined : policyPacksPackSelectReaderTitle}
            style={{ display: "block", width: "100%", maxWidth: 480, padding: 8, marginTop: 4 }}
          >
            <option value="">—</option>
            {packs.map((p) => (
              <option key={p.policyPackId} value={p.policyPackId}>
                {p.name} ({p.policyPackId.slice(0, 8)}…)
              </option>
            ))}
          </select>
        </label>
      </section>

      <section style={{ marginBottom: 32 }} aria-labelledby="policy-packs-content-heading">
        <h3 id="policy-packs-content-heading">
          {canMutatePacks ? policyPacksPackContentHeadingOperator : policyPacksPackContentHeadingReader}
        </h3>
        <h4 style={{ marginTop: 8, marginBottom: 8 }}>Effective resolved packs</h4>
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            overflow: "auto",
            fontSize: 12,
            maxHeight: 360,
            marginBottom: 20,
          }}
        >
          {effective ? JSON.stringify(effective, null, 2) : "—"}
        </pre>

        <h4 style={{ marginTop: 0, marginBottom: 8 }}>Resolved effective content</h4>
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            overflow: "auto",
            fontSize: 12,
            maxHeight: 360,
            marginBottom: 24,
          }}
        >
          {effectiveContent ? JSON.stringify(effectiveContent, null, 2) : "—"}
        </pre>

        <h4 style={{ marginTop: 0, marginBottom: 8 }}>Published versions</h4>
        {packVersions.length === 0 ? (
          <p style={{ color: "#666", fontSize: 14 }}>
            {selectedPackId
              ? canMutatePacks
                ? policyPacksPublishedVersionsEmptyOperatorLine
                : policyPacksPublishedVersionsEmptyReaderLine
              : "Select a pack to load versions."}
          </p>
        ) : (
          <ul style={{ fontSize: 14, lineHeight: 1.6 }}>
            {packVersions.map((v) => (
              <li key={v.policyPackVersionId}>
                <strong>{v.version}</strong>
                {v.isPublished ? " · published" : " · draft"}
                <span style={{ color: "#64748b" }}> · {v.createdUtc}</span>
              </li>
            ))}
          </ul>
        )}

        <h4 style={{ marginTop: 20, marginBottom: 8 }}>Compare versions</h4>
        {!canMutatePacks ? (
          <p className="mb-1 max-w-prose text-xs text-neutral-500 dark:text-neutral-400" role="note">
            {policyPacksCompareVersionsReaderSubline}
          </p>
        ) : null}
        <p style={{ fontSize: 14, color: "#555", marginTop: 0 }}>
          {canMutatePacks ? policyPacksCompareVersionsIntroOperator : policyPacksCompareVersionsIntroReader}
        </p>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "flex-end", marginBottom: 12 }}>
          <label>
            Left version
            <select
              value={compareLeftId}
              onChange={(e) => {
                setCompareLeftId(e.target.value);
                setShowVersionDiff(false);
              }}
              style={{ display: "block", minWidth: 220, padding: 8, marginTop: 4 }}
            >
              <option value="">—</option>
              {packVersions.map((v) => (
                <option key={`L-${v.policyPackVersionId}`} value={v.policyPackVersionId}>
                  {v.version} ({v.policyPackVersionId.slice(0, 8)}…)
                </option>
              ))}
            </select>
          </label>
          <label>
            Right version
            <select
              value={compareRightId}
              onChange={(e) => {
                setCompareRightId(e.target.value);
                setShowVersionDiff(false);
              }}
              style={{ display: "block", minWidth: 220, padding: 8, marginTop: 4 }}
            >
              <option value="">—</option>
              {packVersions.map((v) => (
                <option key={`R-${v.policyPackVersionId}`} value={v.policyPackVersionId}>
                  {v.version} ({v.policyPackVersionId.slice(0, 8)}…)
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            onClick={() => setShowVersionDiff(true)}
            disabled={!compareLeftId || !compareRightId || compareLeftId === compareRightId}
            title={canMutatePacks ? undefined : policyPacksShowDiffButtonReaderTitle}
          >
            {canMutatePacks ? "Show diff" : policyPacksShowDiffButtonLabelReaderRank}
          </button>
          {showVersionDiff ? (
            <button type="button" onClick={() => setShowVersionDiff(false)} title={policyPacksHideDiffButtonTitle}>
              Hide diff
            </button>
          ) : null}
        </div>
        {showVersionDiff && compareLeftId !== compareRightId && compareLeftVersion && compareRightVersion ? (
          <PolicyPackDiffView leftVersion={compareLeftVersion} rightVersion={compareRightVersion} />
        ) : null}
        {showVersionDiff && compareLeftId !== compareRightId && (!compareLeftVersion || !compareRightVersion) ? (
          <p style={{ color: "#b91c1c" }}>Selected versions are no longer in the list; refresh and try again.</p>
        ) : null}
      </section>
      </div>

      <section style={{ marginBottom: 32 }} aria-labelledby="policy-packs-lifecycle-heading">
        <h3 id="policy-packs-lifecycle-heading">
          {canMutatePacks ? "Lifecycle actions" : "Lifecycle actions (operator writes)"}
        </h3>
        {canMutatePacks ? null : (
          <p style={{ color: "#64748b", fontSize: 12, maxWidth: "48rem", marginTop: 4, marginBottom: 8 }}>
            {policyPacksLifecycleLeadReaderLine}
          </p>
        )}
        <div className={cn(!canMutatePacks && "opacity-90")}>
          <section style={{ marginBottom: 32 }} aria-labelledby="policy-packs-vertical-import-heading">
            <h4 id="policy-packs-vertical-import-heading" style={{ marginTop: 0, marginBottom: 8 }}>
              Import a vertical policy pack
            </h4>
            <p className="mb-3 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
              Loads the starter <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">policy-pack.json</code>{" "}
              shipped under{" "}
              <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">archlucid-ui/public/vertical-templates/</code>{" "}
              (mirrors <code className="rounded bg-neutral-100 px-1 dark:bg-neutral-800">templates/policy-packs/</code> in
              the repo). Fills the create form below — adjust name and JSON, then create and publish.
            </p>
            <div className="mb-2 flex flex-wrap gap-2">
              {VERTICAL_POLICY_PACK_IMPORTS.map((row) => (
                <Button
                  key={row.slug}
                  type="button"
                  size="sm"
                  variant="secondary"
                  disabled={verticalImportSlug !== null || !canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  onClick={() => void importVerticalPolicyPack(row.slug, row.label)}
                >
                  {verticalImportSlug === row.slug ? "Loading…" : row.label}
                </Button>
              ))}
            </div>
          </section>

          <section style={{ marginBottom: 32 }}>
            <h4 style={{ marginTop: 0, marginBottom: 8 }}>Create pack</h4>
            <div style={{ display: "grid", gap: 10, maxWidth: 720 }}>
              <label>
                Name
                <input
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                />
              </label>
              <label>
                Description
                <input
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                />
              </label>
              <label>
                Pack type
                <select
                  value={packType}
                  onChange={(e) => setPackType(e.target.value)}
                  disabled={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                >
                  {PACK_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>
                      {t.label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Initial content (JSON)
                <textarea
                  value={createJson}
                  onChange={(e) => setCreateJson(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  rows={12}
                  style={{ display: "block", width: "100%", fontFamily: "monospace", fontSize: 12, marginTop: 4 }}
                />
              </label>
              <button
                type="button"
                onClick={() => void onCreate()}
                disabled={loading || !canMutatePacks}
                title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                className={cn(
                  !canMutatePacks &&
                    "rounded border border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/50 dark:text-neutral-400",
                )}
              >
                {canMutatePacks ? "Create pack" : policyPacksCreatePackButtonLabelReaderRank}
              </button>
            </div>
          </section>

          <section style={{ marginBottom: 32 }}>
            <h4 style={{ marginTop: 0, marginBottom: 8 }}>Publish version</h4>
            <p style={{ fontSize: 14, color: "#555" }}>
              Creates a published version row and marks the pack Active. Use a new semantic version when content changes.
            </p>
            <div style={{ display: "grid", gap: 10, maxWidth: 720 }}>
              <label>
                Version label
                <input
                  value={publishVersion}
                  onChange={(e) => setPublishVersion(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
                />
              </label>
              <label>
                Content (JSON)
                <textarea
                  value={publishJson}
                  onChange={(e) => setPublishJson(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  rows={12}
                  style={{ display: "block", width: "100%", fontFamily: "monospace", fontSize: 12, marginTop: 4 }}
                />
              </label>
              <button
                type="button"
                onClick={() => void onPublish()}
                disabled={loading || !selectedPackId || !canMutatePacks}
                title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                className={cn(
                  !canMutatePacks &&
                    "rounded border border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/50 dark:text-neutral-400",
                )}
              >
                {canMutatePacks ? "Publish" : policyPacksPublishButtonLabelReaderRank}
              </button>
            </div>
          </section>

          <section style={{ marginBottom: 0 }}>
            <h4 style={{ marginTop: 0, marginBottom: 8 }}>Assign to current scope</h4>
            <p style={{ fontSize: 14, color: "#555" }}>
              Assignment must reference an existing version string for that pack (e.g. the one you published).
            </p>
            <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
              <label>
                Version
                <input
                  value={assignVersion}
                  onChange={(e) => setAssignVersion(e.target.value)}
                  readOnly={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", padding: 8, marginTop: 4, width: 160 }}
                />
              </label>
              <label>
                Scope level
                <select
                  value={assignScopeLevel}
                  onChange={(e) => setAssignScopeLevel(e.target.value)}
                  disabled={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  style={{ display: "block", padding: 8, marginTop: 4, minWidth: 140 }}
                >
                  <option value="Tenant">Tenant</option>
                  <option value="Workspace">Workspace</option>
                  <option value="Project">Project</option>
                </select>
              </label>
              <label style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
                <input
                  type="checkbox"
                  checked={assignPinned}
                  disabled={!canMutatePacks}
                  title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                  onChange={(e) => setAssignPinned(e.target.checked)}
                />
                Pinned
              </label>
              <button
                type="button"
                onClick={() => void onAssign()}
                disabled={loading || !selectedPackId || !canMutatePacks}
                title={canMutatePacks ? undefined : enterpriseMutationControlDisabledTitle}
                className={cn(
                  !canMutatePacks &&
                    "rounded border border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/50 dark:text-neutral-400",
                )}
              >
                {canMutatePacks ? "Assign" : policyPacksAssignButtonLabelReaderRank}
              </button>
            </div>
          </section>
        </div>
      </section>
    </main>
  );
}
