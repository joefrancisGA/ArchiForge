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
      "The new-request wizard walks you through system identity, requirements, constraints, and advanced inputs — then submits the run and tracks the pipeline in real time.",
    primaryHref: "/runs/new",
    primaryLabel: "Start new request",
  },
  {
    title: "Track run progress",
    shortBody: "Watch progress in the wizard or open the run from the runs list when ready.",
    detail:
      "The coordinator fills snapshots and pipeline steps. You can use the wizard’s last step or open run detail anytime.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open runs list",
  },
  {
    title: "Finalize the reviewed manifest",
    shortBody: "On run detail, finalize when the run is ready, or use the API/CLI for automation.",
    detail:
      "Until finalization, there is no manifest link or artifact exports. See docs/OPERATOR_QUICKSTART.md in the repo for CLI/API examples.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Choose run → open detail",
  },
  {
    title: "Review manifest and artifacts",
    shortBody: "After finalization, review the manifest summary, artifact table, and export links on run detail.",
    detail:
      "Open the reviewed manifest link from run detail for the full page; use artifact actions for download and review.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open a finalized run",
  },
];
