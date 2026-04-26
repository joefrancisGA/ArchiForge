import Link from "next/link";

import { ArtifactListTable } from "@/components/ArtifactListTable";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorErrorCallout,
  OperatorMalformedCallout,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { coerceArtifactDescriptorList, coerceManifestSummary } from "@/lib/operator-response-guards";
import { getBundleDownloadUrl, getManifestSummary, listArtifacts } from "@/lib/api";
import type { ArtifactDescriptor, ManifestSummary } from "@/types/authority";

/** Server-rendered manifest detail page. Shows manifest summary, artifacts table, and download links. */
export default async function ManifestDetailPage({
  params,
}: {
  params: Promise<{ manifestId: string }>;
}) {
  const { manifestId } = await params;

  let summary: ManifestSummary | null = null;
  let artifacts: ArtifactDescriptor[] = [];
  let summaryFailure: ApiLoadFailureState | null = null;
  let artifactsFailure: ApiLoadFailureState | null = null;
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
    summaryFailure = toApiLoadFailure(e);
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
    artifactsFailure = toApiLoadFailure(e);
  }

  if (summaryFailure) {
    return (
      <main>
        <h2>Manifest</h2>
        <p className="mb-2 text-sm font-semibold">
          Manifest summary could not be loaded.
        </p>
        <OperatorApiProblem
          problem={summaryFailure.problem}
          fallbackMessage={summaryFailure.message}
          correlationId={summaryFailure.correlationId}
        />
        <OperatorTryNext>
          Typical causes: unknown manifest ID (404), auth, or API unavailability—this is not an empty artifact list.
          Re-open the manifest link from <Link href="/runs?projectId=default">Runs</Link> → run detail, or confirm the
          ID in the URL matches a finalized manifest in scope.
        </OperatorTryNext>
        <p className="text-sm">
          <Link href="/">Home</Link>
          {" · "}
          <Link href="/runs?projectId=default">Runs</Link>
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
          <p className="mt-2">{summaryMalformed}</p>
        </OperatorMalformedCallout>
        <OperatorTryNext>
          Align API and UI versions (<code>GET /version</code>). If you followed a stale bookmark, open the manifest
          again from <Link href="/runs?projectId=default">Runs</Link>.
        </OperatorTryNext>
        <p className="text-sm">
          <Link href="/">Home</Link>
          {" · "}
          <Link href="/runs?projectId=default">Runs</Link>
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
          <p className="mt-2">
            The API returned no summary object and no parseable error payload—an unexpected empty success path. Retry
            once; if it persists, capture <code>GET /version</code> and request logs for the manifest ID in the URL.
          </p>
        </OperatorErrorCallout>
        <OperatorTryNext>
          Hard-refresh, confirm proxy scope headers match the manifest&apos;s tenant/project, then navigate from run
          detail instead of a pasted ID.
        </OperatorTryNext>
        <p className="text-sm">
          <Link href="/">Home</Link>
          {" · "}
          <Link href="/runs?projectId=default">Runs</Link>
        </p>
      </main>
    );
  }

  return (
    <main>
      <h2>Manifest</h2>
      <p className="text-sm">
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs?projectId=default">Runs</Link>
        {" · "}
        <Link href={`/runs/${summary.runId}`}>Run detail</Link>
      </p>
      <p className="text-sm text-neutral-500 dark:text-neutral-400 max-w-3xl">
        Artifact rows link here for review. Use bundle download for the full ZIP.
      </p>
      <p>
        <strong>Manifest ID:</strong> {summary.manifestId}
      </p>
      <p>
        <strong>Status:</strong> {summary.status}
      </p>
      {summary.operatorSummary && (
        <p className="text-sm text-neutral-600 dark:text-neutral-400 leading-normal">{summary.operatorSummary}</p>
      )}
      <p>
        <strong>Rule set:</strong> {summary.ruleSetId} {summary.ruleSetVersion}
      </p>
      <p>
        <strong>Manifest hash:</strong>{" "}
        <span className="font-mono text-[13px]">{summary.manifestHash}</span>
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

      <section className="mt-6">
        <h3>Artifacts</h3>

        <p className="mb-3">
          <a href={getBundleDownloadUrl(manifestId)}>Download bundle (ZIP)</a>
        </p>

        {artifactsFailure && (
          <>
            <p className="mb-2 text-sm font-semibold">
              Artifact list could not be loaded.
            </p>
            <OperatorApiProblem
              problem={artifactsFailure.problem}
              fallbackMessage={artifactsFailure.message}
              correlationId={artifactsFailure.correlationId}
              variant="warning"
            />
            <OperatorTryNext>
              The artifacts request failed (network, 404, or server error)—distinct from an empty list or malformed
              JSON. Summary above may still be valid; retry the page or open <strong>Download bundle (ZIP)</strong> if
              the list endpoint alone fails.
            </OperatorTryNext>
          </>
        )}

        {!artifactsFailure && artifactsMalformed && (
          <>
            <OperatorMalformedCallout>
              <strong>Artifact list response was not usable.</strong>
              <p className="mt-2">{artifactsMalformed}</p>
            </OperatorMalformedCallout>
            <OperatorTryNext>
              Compare API/UI versions. You can still use bundle download when the list contract drifts but storage is
              intact—watch for 404 vs manifest-not-found in ProblemDetails.
            </OperatorTryNext>
          </>
        )}

        {!artifactsFailure && !artifactsMalformed && artifacts.length === 0 && (
          <OperatorEmptyState title="No artifacts listed for this manifest">
            <p className="m-0">
              The summary loaded, but the artifact descriptor list is empty (valid empty result).
              Bundle ZIP may return 404 when there is no bundle.
            </p>
          </OperatorEmptyState>
        )}

        {!artifactsFailure && !artifactsMalformed && artifacts.length > 0 && (
          <ArtifactListTable manifestId={manifestId} artifacts={artifacts} />
        )}
      </section>
    </main>
  );
}
