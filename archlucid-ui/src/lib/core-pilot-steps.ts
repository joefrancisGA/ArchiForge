export type CorePilotStepBase = {
  title: string;
  shortBody: string;
  detail?: string;
  primaryHref: string;
  primaryLabel: string;
};

/**
 * Core Pilot path titles and links — shared between the First Manifest checklist and diagnostics summary on operator home.
 * No JSX here; `OperatorFirstRunWorkflowPanel` adds rich optional `secondary` for specific steps locally.
 */
export const CORE_PILOT_STEPS: CorePilotStepBase[] = [
  {
    title: "Create an architecture request",
    shortBody: "Capture system identity, requirements, and constraints.",
    detail:
      "The new-request wizard walks you through system identity, requirements, constraints, and advanced inputs — then submits the review pipeline and tracks progress in real time.",
    primaryHref: "/reviews/new",
    primaryLabel: "Start new request",
  },
  {
    title: "Track review progress",
    shortBody: "Watch progress in the wizard or open the review from the reviews list when ready.",
    detail:
      "The coordinator fills snapshots and pipeline steps. You can use the wizard’s last step or open review detail anytime.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Open reviews list",
  },
  {
    title: "Finalize the reviewed manifest",
    shortBody: "On review detail, finalize when the pipeline is ready, or use the API/CLI for automation.",
    detail:
      "Until finalization, there is no manifest link or artifact exports. See docs/OPERATOR_QUICKSTART.md in the repo for CLI/API examples.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Choose review → open detail",
  },
  {
    title: "Review manifest and artifacts",
    shortBody: "After finalization, review the manifest summary, artifact table, and export links on review detail.",
    detail:
      "Open the reviewed manifest link from review detail for the full page; use artifact actions for download and review.",
    primaryHref: "/reviews?projectId=default",
    primaryLabel: "Open a finalized review",
  },
];
