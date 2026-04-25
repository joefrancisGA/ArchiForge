import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";

/** Raw markdown on GitHub (buyer source of record). */
export const TRUST_CENTER_RAW_GITHUB_URL =
  "https://raw.githubusercontent.com/joefrancisGA/ArchLucid/main/docs/trust-center.md";

/** Blob view (same repo path as procurement deep links). */
export const TRUST_CENTER_BLOB_GITHUB_URL =
  "https://github.com/joefrancisGA/ArchLucid/blob/main/docs/trust-center.md";

const LAST_REVIEWED_PATTERN = /<!--\s*TRUST_CENTER_LAST_REVIEWED_UTC:([^>]+)\s*-->/;

/**
 * Reads `docs/trust-center.md` from the monorepo root, or `go-to-market-samples/trust-center.md`
 * inside the Docker UI build (see `archlucid-ui/Dockerfile`).
 */
export function readTrustCenterMarkdown(): string {
  const cwd = process.cwd();
  const dockerPath = join(cwd, "go-to-market-samples", "trust-center.md");

  if (existsSync(dockerPath)) {
    return readFileSync(dockerPath, "utf8").replace(/\r\n/g, "\n");
  }

  const monorepoPath = join(cwd, "..", "docs", "trust-center.md");
  if (existsSync(monorepoPath)) {
    return readFileSync(monorepoPath, "utf8").replace(/\r\n/g, "\n");
  }

  throw new Error(
    "docs/trust-center.md not found. Expected ../docs/trust-center.md (monorepo) or go-to-market-samples/trust-center.md (Docker).",
  );
}

export function parseTrustCenterLastReviewedUtc(markdown: string): string | null {
  const m = markdown.match(LAST_REVIEWED_PATTERN);
  return m ? m[1]!.trim() : null;
}
