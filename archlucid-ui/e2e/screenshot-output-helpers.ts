import path from "node:path";

/**
 * Resolves a path under `archlucid-ui/public/...`.
 * Uses `process.cwd()` so Playwright's transform matches Node execution (avoid `import.meta` + bundler tsconfig).
 */
export function publicDirUnderUi(...parts: string[]): string {
  return path.join(process.cwd(), "public", ...parts);
}
