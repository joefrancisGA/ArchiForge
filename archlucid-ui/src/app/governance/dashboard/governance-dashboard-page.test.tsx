import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const getGovernanceDashboardMock = vi.fn();

vi.mock("@/lib/api", () => ({
  getGovernanceDashboard: (...args: unknown[]) => getGovernanceDashboardMock(...args),
}));

const pushMock = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: pushMock }),
}));

import GovernanceDashboardPage from "./page";

describe("GovernanceDashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders pending count badge when > 0", async () => {
    getGovernanceDashboardMock.mockResolvedValue({
      pendingApprovals: [
        {
          approvalRequestId: "a1",
          runId: "run-1",
          manifestVersion: "v1",
          sourceEnvironment: "dev",
          targetEnvironment: "test",
          status: "Submitted",
          requestedBy: "alice",
          reviewedBy: null,
          requestComment: null,
          reviewComment: null,
          requestedUtc: "2026-04-01T12:00:00Z",
          reviewedUtc: null,
        },
      ],
      recentDecisions: [],
      recentChanges: [],
      pendingCount: 2,
    });

    render(<GovernanceDashboardPage />);

    await waitFor(() => {
      expect(screen.getByTestId("governance-dashboard-pending-count-badge")).toHaveTextContent("2 open");
    });
  });

  it("shows empty states when no data", async () => {
    getGovernanceDashboardMock.mockResolvedValue({
      pendingApprovals: [],
      recentDecisions: [],
      recentChanges: [],
      pendingCount: 0,
    });

    render(<GovernanceDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText(/no pending approvals — all clear/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/no recent decisions/i)).toBeInTheDocument();
    expect(screen.getByText(/no policy pack changes recorded/i)).toBeInTheDocument();
  });

  it("renders recent decisions with correct badges", async () => {
    getGovernanceDashboardMock.mockResolvedValue({
      pendingApprovals: [],
      recentDecisions: [
        {
          approvalRequestId: "d1",
          runId: "run-x",
          manifestVersion: "v2",
          sourceEnvironment: "dev",
          targetEnvironment: "prod",
          status: "Approved",
          requestedBy: "bob",
          reviewedBy: "carol",
          requestComment: null,
          reviewComment: "LGTM",
          requestedUtc: "2026-04-01T10:00:00Z",
          reviewedUtc: "2026-04-01T11:00:00Z",
        },
        {
          approvalRequestId: "d2",
          runId: "run-y",
          manifestVersion: "v1",
          sourceEnvironment: "test",
          targetEnvironment: "prod",
          status: "Rejected",
          requestedBy: "bob",
          reviewedBy: "carol",
          requestComment: null,
          reviewComment: "No",
          requestedUtc: "2026-04-01T09:00:00Z",
          reviewedUtc: "2026-04-01T09:30:00Z",
        },
        {
          approvalRequestId: "d3",
          runId: "run-z",
          manifestVersion: "v3",
          sourceEnvironment: "dev",
          targetEnvironment: "test",
          status: "Promoted",
          requestedBy: "bob",
          reviewedBy: "carol",
          requestComment: null,
          reviewComment: null,
          requestedUtc: "2026-04-01T08:00:00Z",
          reviewedUtc: "2026-04-01T08:15:00Z",
        },
      ],
      recentChanges: [],
      pendingCount: 0,
    });

    render(<GovernanceDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText("Approved")).toBeInTheDocument();
    });

    expect(screen.getByText("Rejected")).toBeInTheDocument();
    expect(screen.getByText("Promoted")).toBeInTheDocument();
  });

  it("renders policy pack changes with monospace pack id", async () => {
    getGovernanceDashboardMock.mockResolvedValue({
      pendingApprovals: [],
      recentDecisions: [],
      recentChanges: [
        {
          changeLogId: "11111111-2222-3333-4444-555555555555",
          policyPackId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          tenantId: "tenant-1",
          workspaceId: "ws-1",
          projectId: "proj-1",
          changeType: "Published",
          changedBy: "ops",
          changedUtc: "2026-04-02T00:00:00Z",
          summaryText: "Version 1.0.0 published",
        },
      ],
      pendingCount: 0,
    });

    render(<GovernanceDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText("Published")).toBeInTheDocument();
    });

    expect(screen.getByText("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")).toBeInTheDocument();
    expect(screen.getByText(/version 1\.0\.0 published/i)).toBeInTheDocument();
  });
});
