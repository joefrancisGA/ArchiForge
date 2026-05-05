/**
 * Progressive disclosure labels (sidebar + inline help). Tier model unchanged; copy only.
 * @see `docs/library/operator-shell.md`
 */
export const NAV_DISCLOSURE = {
  extended: {
    /** Collapsed: reveal extended-tier links (analysis / investigation). */
    show: "Show analysis & investigation tools",
    /** Expanded: hide extended-tier links. */
    hide: "Hide analysis & investigation tools",
    /** `title` on the settings checkbox for extended-tier links. */
    title:
      "Compare reviews, replay authority chains, advisory scans, and similar investigation tools.",
  },
  /**
   * Maps to **`showAdvanced`** (localStorage `archlucid_nav_show_advanced`) together with Navigation settings →
   * “Show governance, audit & admin controls”.
   */
  advancedOperationsSidebar: {
    show: "Show Advanced Operations",
    hide: "Hide Advanced Operations",
    /** Announced when collapsed (not the visible button text on small breakpoints). */
    assistiveCollapsed:
      "Reveals Governance, Alerts, and Audit log destinations in operator navigation.",
  },
  advanced: {
    /** Settings checkbox: reveal advanced Enterprise Controls links. */
    show: "Show governance, audit & admin controls",
    title:
      "Alert configuration, audit log, governance workflow, planning, and admin-level controls.",
  },
} as const;
