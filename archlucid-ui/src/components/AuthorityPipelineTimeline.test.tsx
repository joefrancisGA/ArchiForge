import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { AuthorityPipelineTimeline } from "@/components/AuthorityPipelineTimeline";
import type { PipelineTimelineItem } from "@/types/authority";

describe("AuthorityPipelineTimeline", () => {
  it("renders ordered audit rows", () => {
    const items: PipelineTimelineItem[] = [
      {
        eventId: "11111111-1111-1111-1111-111111111111",
        occurredUtc: "2026-04-01T12:00:00.000Z",
        eventType: "RunStarted",
        actorUserName: "system",
        correlationId: "c1",
      },
      {
        eventId: "22222222-2222-2222-2222-222222222222",
        occurredUtc: "2026-04-01T13:00:00.000Z",
        eventType: "RunCompleted",
        actorUserName: "system",
        correlationId: null,
      },
    ];

    render(<AuthorityPipelineTimeline items={items} />);

    expect(screen.getByText("Run started")).toBeInTheDocument();
    expect(screen.getByText("Run completed")).toBeInTheDocument();
    expect(screen.getAllByText(/RunStarted/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/RunCompleted/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/c1/)).toBeInTheDocument();
    expect(screen.getByText(/11111111-1111-1111-1111-111111111111/)).toBeInTheDocument();
  });

  it("shows empty guidance when there are no events", () => {
    render(<AuthorityPipelineTimeline items={[]} />);

    expect(screen.getByText(/No events recorded/)).toBeInTheDocument();
  });

  it("shows load error message when provided", () => {
    render(
      <AuthorityPipelineTimeline items={null} loadErrorMessage="unauthorized" />,
    );

    expect(screen.getByText(/Pipeline timeline could not be loaded/)).toBeInTheDocument();
    expect(screen.getByText(/unauthorized/)).toBeInTheDocument();
  });
});
