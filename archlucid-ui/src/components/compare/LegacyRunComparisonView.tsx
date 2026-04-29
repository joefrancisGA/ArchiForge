import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { sortDiffItems } from "@/lib/compare-display-sort";
import type { RunComparison } from "@/types/authority";

const cellCls = "border border-neutral-200 px-2.5 py-2 text-left align-top dark:border-neutral-700";
const monoCls = "font-mono text-[13px]";

const FIXTURE_MANIFEST_RE = /manifest-(left|right)-fixture/i;
const FIXTURE_HASH_RE = /^sha256:(left|right)$/i;

/** Returns true when a manifest ID looks like a fixture/seed placeholder rather than a real ID. */
function isFixtureManifestId(id: string): boolean {
  return FIXTURE_MANIFEST_RE.test(id);
}

/** Returns true when a hash looks like a placeholder rather than a real digest. */
function isFixtureHash(hash: string): boolean {
  return FIXTURE_HASH_RE.test(hash);
}

/** Maps a potentially fixture-shaped manifest ID to a display label. */
function displayManifestId(id: string, side: "left" | "right"): string {
  if (isFixtureManifestId(id)) {
    return side === "left" ? "Baseline manifest" : "Updated manifest";
  }

  return id;
}

/** Maps a potentially fixture-shaped hash to a display string. */
function displayHash(hash: string): string {
  if (isFixtureHash(hash)) {
    return "(hash unavailable in demo)";
  }

  return hash;
}

/**
 * Run-level and manifest diffs from the comparison endpoint.
 */
export function LegacyRunComparisonView(props: { result: RunComparison }) {
  const { result } = props;
  const runLevelDiffs = sortDiffItems(result.runLevelDiffs);
  const manifestDiffs =
    result.manifestComparison !== undefined && result.manifestComparison !== null
      ? sortDiffItems(result.manifestComparison.diffs)
      : [];

  return (
    <section id="compare-legacy" className="mt-7">
      <h3 className="mb-2">Run-level diff</h3>
      <p className="mt-0 text-sm text-neutral-500 dark:text-neutral-400">
        <strong>Base:</strong> <code className={monoCls}>{result.leftRunId}</code> ·{" "}
        <strong>Updated:</strong> <code className={monoCls}>{result.rightRunId}</code>
        {result.runLevelDiffCount !== undefined && (
          <>
            {" "}
            · <strong>Changes:</strong> {result.runLevelDiffCount}
          </>
        )}
      </p>

      <h4 className="text-[15px]">Run-level diffs</h4>
      {result.runLevelDiffs.length === 0 ? (
        <OperatorEmptyState title="No run-level diffs">
          <p className="m-0 text-sm">
            The endpoint returned zero row-level differences (valid empty result).
          </p>
        </OperatorEmptyState>
      ) : (
        <table className="mt-2 w-full border-collapse text-sm">
          <thead>
            <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
              <th className={cellCls}>Kind</th>
              <th className={cellCls}>Section</th>
              <th className={cellCls}>Key</th>
              <th className={cellCls}>Before</th>
              <th className={cellCls}>After</th>
            </tr>
          </thead>
          <tbody>
            {runLevelDiffs.map((diff, index) => (
              <tr key={`${diff.section}-${diff.key}-${diff.diffKind}-${index}`}>
                <td className={cellCls}>{diff.diffKind}</td>
                <td className={cellCls}>{diff.section}</td>
                <td className={cellCls}>{diff.key}</td>
                <td className={`${cellCls} ${monoCls}`}>{diff.beforeValue ?? "—"}</td>
                <td className={`${cellCls} ${monoCls}`}>{diff.afterValue ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h4 className="mt-6 text-[15px]">Manifest diff</h4>
      {!result.manifestComparison ? (
        <OperatorEmptyState title="No manifest comparison block">
          <p className="m-0 text-sm">
            The API did not include a manifest comparison object for this pair (distinct from "zero
            diffs inside a comparison").
          </p>
        </OperatorEmptyState>
      ) : (
        <>
          <p className="mb-2 text-sm">
            <strong>Changes:</strong> added {result.manifestComparison.addedCount}, removed{" "}
            {result.manifestComparison.removedCount}, changed {result.manifestComparison.changedCount}
          </p>
          <details className="mb-3 rounded-md border border-neutral-200 bg-neutral-50 px-3 py-2 text-sm dark:border-neutral-700 dark:bg-neutral-900/50">
            <summary className="cursor-pointer text-xs font-medium text-neutral-600 dark:text-neutral-400">
              Technical details
            </summary>
            <p className="mb-0 mt-2 text-xs text-neutral-600 dark:text-neutral-400">
              <strong>Manifest IDs:</strong>{" "}
              <code className={monoCls}>
                {displayManifestId(result.manifestComparison.leftManifestId, "left")}
              </code>{" "}
              vs{" "}
              <code className={monoCls}>
                {displayManifestId(result.manifestComparison.rightManifestId, "right")}
              </code>
              <br />
              <strong>Hashes:</strong>{" "}
              <span className={monoCls}>{displayHash(result.manifestComparison.leftManifestHash)}</span> vs{" "}
              <span className={monoCls}>{displayHash(result.manifestComparison.rightManifestHash)}</span>
            </p>
          </details>
          {manifestDiffs.length === 0 ? (
            <OperatorEmptyState title="Manifest comparison has zero line items">
              <p className="m-0 text-sm">Comparison object present but diff list is empty.</p>
            </OperatorEmptyState>
          ) : (
            <table className="mt-2 w-full border-collapse text-sm">
              <thead>
                <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                  <th className={cellCls}>Kind</th>
                  <th className={cellCls}>Section</th>
                  <th className={cellCls}>Key</th>
                  <th className={cellCls}>Before</th>
                  <th className={cellCls}>After</th>
                  <th className={cellCls}>Notes</th>
                </tr>
              </thead>
              <tbody>
                {manifestDiffs.map((diff, index) => (
                  <tr key={`${diff.section}-${diff.key}-${diff.diffKind}-${index}`}>
                    <td className={cellCls}>{diff.diffKind}</td>
                    <td className={cellCls}>{diff.section}</td>
                    <td className={cellCls}>{diff.key}</td>
                    <td className={`${cellCls} ${monoCls}`}>{diff.beforeValue ?? "—"}</td>
                    <td className={`${cellCls} ${monoCls}`}>{diff.afterValue ?? "—"}</td>
                    <td className={cellCls}>{diff.notes ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </section>
  );
}
