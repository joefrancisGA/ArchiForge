export type CorePilotStepBase = {
  title: string;
  shortBody: string;
  detail?: string;
  primaryHref: string;
  primaryLabel: string;
};

/**
 * Core Pilot path titles and links — shared between the first-review checklist and diagnostics summary on operator home.
 * No JSX here; `OperatorFirstRunWorkflowPanel` adds rich optional `secondary` for specific steps locally.
 */
export const CORE_PILOT_STEPS: CorePilotStepBase[] = [
  {
    title: "Create an architecture review request",
    shortBody: "Capture system identity, requirements, and constraints for your first review package.",
    detail:
      "The new-request wizard walks you through system identity, requirements, constraints, and advanced inputs — then submits the review pipeline and tracks progress in real time.",
    primaryHref: "/reviews/new",
    primaryLabel: "Start new request",
  },
  {
    title: "Track review progress",
    shortBody: "Watch pipeline progress in the wizard or open the review from the list when it is ready.",
    detail:
      "The coordinator fills snapshots and pipeline steps. You can use the wizard’s last step or open review detail anytime.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Open reviews list",
  },
  {
    title: "Finalize the review package",
    shortBody: "On review detail, finalize when the pipeline is ready — this locks the manifest and unlocks exports.",
    detail:
      "Commit/finalize produces the golden manifest and artifacts. Until then, the manifest summary and artifact table are not available. See docs/OPERATOR_QUICKSTART.md for CLI/API.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Choose review → open detail",
  },
  {
    title: "Review the review package",
    shortBody:
      "After finalization, read the manifest summary and findings; download or share artifacts — that bundle is your review package.",
    detail:
      "Open the manifest link from review detail for the full page; use artifact actions for download and in-shell review.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Open a finalized review",
  },
];
