import Link from "next/link";

import { ArtifactListTable } from "@/components/ArtifactListTable";
import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorMalformedCallout,
  OperatorWarningCallout,
} from "@/components/OperatorShellMessage";
import { coerceArtifactDescriptorList, coerceManifestSummary } from "@/lib/operator-response-guards";
import { getBundleDownloadUrl, getManifestSummary, listArtifacts } from "@/lib/api";
import type { ArtifactDescriptor, ManifestSummary } from "@/types/authority";

export default async function ManifestDetailPage({
  params,
}: {
  params: Promise<{ manifestId: string }>;
}) {
  const { manifestId } = await params;

  let summary: ManifestSummary | null = null;
  let artifacts: ArtifactDescriptor[] = [];
  let summaryError: string | null = null;
  let artifactsError: string | null = null;
  let summaryMalformed: string | null = null;
  let artifactsMalformed: string | null = null;

  try {
    const rawSummary: unknown = await getManifestSummary(manifestId);
    const coercedSummary = coerceManifestSummary(rawSummary);

    if (!coercedSummary.ok) {
      summaryMalformed = coercedSummary.message;
    } else {
      summary = coercedSummary.value;
    }
  } catch (e) {
    summaryError = e instanceof Error ? e.message : "Failed to load manifest summary.";
  }

  try {
    const rawArtifacts: unknown = await listArtifacts(manifestId);
    const coercedArtifacts = coerceArtifactDescriptorList(rawArtifacts);

    if (!coercedArtifacts.ok) {
      artifacts = [];
      artifactsMalformed = coercedArtifacts.message;
    } else {
      artifacts = coercedArtifacts.items;
    }
  } catch (e) {
    artifactsError = e instanceof Error ? e.message : "Failed to load artifact list.";
  }

  if (summaryError) {
    return (
      <main>
        <h2>Manifest</h2>
        <OperatorErrorCallout>
          <strong>Manifest summary could not be loaded.</strong>
          <p style={{ margin: "8px 0 0" }}>{summaryError}</p>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Typical causes: unknown manifest ID (404), auth, or API unavailability. This is not an
            empty artifact list.
          </p>
        </OperatorErrorCallout>
        <p>
          <Link href="/runs?projectId=default">← Runs</Link>
        </p>
      </main>
    );
  }

  if (summaryMalformed) {
    return (
      <main>
        <h2>Manifest</h2>
        <OperatorMalformedCallout>
          <strong>Manifest summary response was not usable.</strong>
          <p style={{ margin: "8px 0 0" }}>{summaryMalformed}</p>
        </OperatorMalformedCallout>
        <p>
          <Link href="/runs?projectId=default">← Runs</Link>
        </p>
      </main>
    );
  }

  if (!summary) {
    return (
      <main>
        <h2>Manifest</h2>
        <OperatorErrorCallout>
          <strong>Manifest summary missing.</strong>
          <p style={{ margin: "8px 0 0" }}>
            No summary was returned without an explicit error. Retry or verify API behavior.
          </p>
        </OperatorErrorCallout>
        <p>
          <Link href="/runs?projectId=default">← Runs</Link>
        </p>
      </main>
    );
  }

  return (
    <main>
      <h2>Manifest</h2>
      <p>
        <Link href={`/runs/${summary.runId}`}>← Run {summary.runId}</Link>
      </p>
      <p>
        <strong>Manifest ID:</strong> {summary.manifestId}
      </p>
      <p>
        <strong>Status:</strong> {summary.status}
      </p>
      {summary.operatorSummary && (
        <p style={{ fontSize: 14, color: "#475569", lineHeight: 1.5 }}>{summary.operatorSummary}</p>
      )}
      <p>
        <strong>Rule set:</strong> {summary.ruleSetId} {summary.ruleSetVersion}
      </p>
      <p>
        <strong>Manifest hash:</strong>{" "}
        <span style={{ fontFamily: "monospace", fontSize: 13 }}>{summary.manifestHash}</span>
      </p>
      <p>
        <strong>Decisions:</strong> {summary.decisionCount}
      </p>
      <p>
        <strong>Warnings:</strong> {summary.warningCount}
      </p>
      <p>
        <strong>Unresolved issues:</strong> {summary.unresolvedIssueCount}
      </p>

      <section style={{ marginTop: 24 }}>
        <h3>Artifacts</h3>

        <p style={{ marginBottom: 12 }}>
          <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
        </p>

        {artifactsError && (
          <OperatorWarningCallout>
            <strong>Artifact list could not be loaded.</strong>
            <p style={{ margin: "8px 0 0" }}>{artifactsError}</p>
          </OperatorWarningCallout>
        )}

        {!artifactsError && artifactsMalformed && (
          <OperatorMalformedCallout>
            <strong>Artifact list response was not usable.</strong>
            <p style={{ margin: "8px 0 0" }}>{artifactsMalformed}</p>
          </OperatorMalformedCallout>
        )}

        {!artifactsError && !artifactsMalformed && artifacts.length === 0 && (
          <OperatorEmptyState title="No artifacts listed for this manifest">
            <p style={{ margin: 0 }}>
              The summary loaded, but the artifact descriptor list is empty (valid empty result).
              Bundle download may still work if a bundle exists server-side.
            </p>
          </OperatorEmptyState>
        )}

        {!artifactsError && !artifactsMalformed && artifacts.length > 0 && (
          <ArtifactListTable manifestId={manifestId} artifacts={artifacts} />
        )}
      </section>
    </main>
  );
}
