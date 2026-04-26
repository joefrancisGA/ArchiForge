/**
 * Default public GitHub blob base for the `main` branch when optional docs env vars are unset.
 * Override with NEXT_PUBLIC_DOCS_BASE_URL (HelpPanel) or NEXT_PUBLIC_ARCHLUCID_DOCS_BLOB_BASE (ContextualHelp).
 */
const DEFAULT_DOCS_GITHUB_ORG = "joefrancisGA";
const DEFAULT_DOCS_GITHUB_REPO = "ArchLucid";

export const DEFAULT_GITHUB_BLOB_BASE = `https://github.com/${DEFAULT_DOCS_GITHUB_ORG}/${DEFAULT_DOCS_GITHUB_REPO}/blob/main`;
