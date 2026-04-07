import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
  }: {
    href: string;
    children: import("react").ReactNode;
  }) => <a href={href}>{children}</a>,
}));

import AdvisoryPage from "./advisory/page";
import AdvisorySchedulingPage from "./advisory-scheduling/page";
import AlertRoutingPage from "./alert-routing/page";
import AlertRulesPage from "./alert-rules/page";
import AlertSimulationPage from "./alert-simulation/page";
import AlertsPage from "./alerts/page";
import AlertTuningPage from "./alert-tuning/page";
import AskPage from "./ask/page";
import CompositeAlertRulesPage from "./composite-alert-rules/page";
import DigestSubscriptionsPage from "./digest-subscriptions/page";
import DigestsPage from "./digests/page";
import GovernanceResolutionPage from "./governance-resolution/page";
import PolicyPacksPage from "./policy-packs/page";
import EvolutionReviewPage from "./evolution-review/page";
import PlanningPage from "./planning/page";
import ProductLearningPage from "./product-learning/page";
import RecommendationLearningPage from "./recommendation-learning/page";
import SearchPage from "./search/page";

/**
 * Render-gate: first paint + import chain for client-only operator pages that had no tests.
 * Does not assert API outcomes (useEffect may run after; fetch errors are caught in-page).
 */
describe("operator client pages — render gate", () => {
  it("AlertsPage renders primary heading", () => {
    render(<AlertsPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Alerts" })).toBeInTheDocument();
  });

  it("AlertRulesPage renders primary heading", () => {
    render(<AlertRulesPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Alert rules" })).toBeInTheDocument();
  });

  it("AlertRoutingPage renders primary heading", () => {
    render(<AlertRoutingPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Alert routing" })).toBeInTheDocument();
  });

  it("AlertSimulationPage renders primary heading", () => {
    render(<AlertSimulationPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Alert rule simulation" })).toBeInTheDocument();
  });

  it("AlertTuningPage renders primary heading", () => {
    render(<AlertTuningPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Alert tuning" })).toBeInTheDocument();
  });

  it("CompositeAlertRulesPage renders primary heading", () => {
    render(<CompositeAlertRulesPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Composite alert rules" })).toBeInTheDocument();
  });

  it("AdvisoryPage renders primary heading", () => {
    render(<AdvisoryPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Improvement Advisor" })).toBeInTheDocument();
  });

  it("AdvisorySchedulingPage renders primary heading", () => {
    render(<AdvisorySchedulingPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Advisory schedules" })).toBeInTheDocument();
  });

  it("RecommendationLearningPage renders primary heading", () => {
    render(<RecommendationLearningPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Recommendation learning" })).toBeInTheDocument();
  });

  it("ProductLearningPage renders primary heading", () => {
    render(<ProductLearningPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Pilot feedback" })).toBeInTheDocument();
  });

  it("PlanningPage renders primary heading", () => {
    render(<PlanningPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Planning" })).toBeInTheDocument();
  });

  it("EvolutionReviewPage renders primary heading", () => {
    render(<EvolutionReviewPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Simulation review" })).toBeInTheDocument();
  });

  it("DigestsPage renders primary heading", () => {
    render(<DigestsPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Architecture digests" })).toBeInTheDocument();
  });

  it("DigestSubscriptionsPage renders primary heading", () => {
    render(<DigestSubscriptionsPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Digest subscriptions" })).toBeInTheDocument();
  });

  it("PolicyPacksPage renders primary heading", () => {
    render(<PolicyPacksPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Policy packs" })).toBeInTheDocument();
  });

  it("GovernanceResolutionPage renders primary heading", () => {
    render(<GovernanceResolutionPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Governance resolution" })).toBeInTheDocument();
  });

  it("SearchPage renders primary heading", () => {
    render(<SearchPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Semantic Search" })).toBeInTheDocument();
  });

  it("AskPage renders primary heading", () => {
    render(<AskPage />);
    expect(screen.getByRole("heading", { level: 2, name: "Ask ArchiForge" })).toBeInTheDocument();
  });
});
