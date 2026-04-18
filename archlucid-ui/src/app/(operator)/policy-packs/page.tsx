"use client";

import { useCallback, useEffect, useState } from "react";
import {
  EnterpriseControlsExecutePageHint,
  EnterpriseExecutePlusPageCue,
} from "@/components/EnterpriseControlsContextHints";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
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
import { enterprisePolicyPacksOperatorPlusLine } from "@/lib/enterprise-controls-context-copy";
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

export default function PolicyPacksPage() {
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

  async function onCreate() {
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
      <h2 style={{ marginTop: 0 }}>Policy packs</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Versioned governance bundles (compliance / alert / composite rule IDs + advisory defaults). Assign with a{" "}
        <strong>scope level</strong> (tenant baseline, workspace, or project); effective content is{" "}
        <strong>hierarchically resolved</strong> (see Governance resolution). Alert evaluators use resolved{" "}
        <code>alertRuleIds</code> / <code>compositeAlertRuleIds</code> when non-empty.
      </p>
      <EnterpriseControlsExecutePageHint />
      <EnterpriseExecutePlusPageCue message={enterprisePolicyPacksOperatorPlusLine} />

      <p>
        <button type="button" onClick={() => void load()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </button>
      </p>

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <section style={{ marginBottom: 32 }}>
        <h3>Packs in scope</h3>
        {packs.length === 0 ? (
          <p style={{ color: "#666" }}>No packs yet.</p>
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
          Selected pack (publish / assign)
          <select
            value={selectedPackId}
            onChange={(e) => setSelectedPackId(e.target.value)}
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

      <section style={{ marginBottom: 32 }}>
        <h3>Create pack</h3>
        <div style={{ display: "grid", gap: 10, maxWidth: 720 }}>
          <label>
            Name
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
          <label>
            Description
            <input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
          <label>
            Pack type
            <select
              value={packType}
              onChange={(e) => setPackType(e.target.value)}
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
              rows={12}
              style={{ display: "block", width: "100%", fontFamily: "monospace", fontSize: 12, marginTop: 4 }}
            />
          </label>
          <button type="button" onClick={() => void onCreate()} disabled={loading}>
            Create pack
          </button>
        </div>
      </section>

      <section style={{ marginBottom: 32 }}>
        <h3>Publish version</h3>
        <p style={{ fontSize: 14, color: "#555" }}>
          Creates a published version row and marks the pack Active. Use a new semantic version when content changes.
        </p>
        <div style={{ display: "grid", gap: 10, maxWidth: 720 }}>
          <label>
            Version label
            <input
              value={publishVersion}
              onChange={(e) => setPublishVersion(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
          <label>
            Content (JSON)
            <textarea
              value={publishJson}
              onChange={(e) => setPublishJson(e.target.value)}
              rows={12}
              style={{ display: "block", width: "100%", fontFamily: "monospace", fontSize: 12, marginTop: 4 }}
            />
          </label>
          <button type="button" onClick={() => void onPublish()} disabled={loading || !selectedPackId}>
            Publish
          </button>
        </div>
      </section>

      <section style={{ marginBottom: 32 }}>
        <h3>Published versions</h3>
        {packVersions.length === 0 ? (
          <p style={{ color: "#666", fontSize: 14 }}>
            {selectedPackId ? "No published versions loaded for this pack yet." : "Select a pack to load versions."}
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
        <p style={{ fontSize: 14, color: "#555", marginTop: 0 }}>
          Pick two published content snapshots for the selected pack and diff JSON structure (added / removed / changed
          paths).
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
          >
            Show diff
          </button>
          {showVersionDiff ? (
            <button type="button" onClick={() => setShowVersionDiff(false)}>
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

      <section style={{ marginBottom: 32 }}>
        <h3>Assign to current scope</h3>
        <p style={{ fontSize: 14, color: "#555" }}>
          Assignment must reference an existing version string for that pack (e.g. the one you published).
        </p>
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
          <label>
            Version
            <input
              value={assignVersion}
              onChange={(e) => setAssignVersion(e.target.value)}
              style={{ display: "block", padding: 8, marginTop: 4, width: 160 }}
            />
          </label>
          <label>
            Scope level
            <select
              value={assignScopeLevel}
              onChange={(e) => setAssignScopeLevel(e.target.value)}
              style={{ display: "block", padding: 8, marginTop: 4, minWidth: 140 }}
            >
              <option value="Tenant">Tenant</option>
              <option value="Workspace">Workspace</option>
              <option value="Project">Project</option>
            </select>
          </label>
          <label style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
            <input type="checkbox" checked={assignPinned} onChange={(e) => setAssignPinned(e.target.checked)} />
            Pinned
          </label>
          <button type="button" onClick={() => void onAssign()} disabled={loading || !selectedPackId}>
            Assign
          </button>
        </div>
      </section>

      <section style={{ marginBottom: 32 }}>
        <h3>Effective resolved packs</h3>
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            overflow: "auto",
            fontSize: 12,
            maxHeight: 360,
          }}
        >
          {effective ? JSON.stringify(effective, null, 2) : "—"}
        </pre>
      </section>

      <section>
        <h3>Resolved effective content</h3>
        <pre
          style={{
            background: "#f5f5f5",
            padding: 12,
            overflow: "auto",
            fontSize: 12,
            maxHeight: 360,
          }}
        >
          {effectiveContent ? JSON.stringify(effectiveContent, null, 2) : "—"}
        </pre>
      </section>
    </main>
  );
}
