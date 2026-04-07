/**
 * No shell POST payload (ask, compare, alert rules, etc.) should approach this.
 * Protects the Node event loop from oversized bodies that would block `request.text()`.
 */
export const PROXY_MAX_BODY_BYTES = 1_048_576; // 1 MB
