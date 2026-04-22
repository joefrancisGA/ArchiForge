import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { FindingExplainPanel } from "@/components/FindingExplainPanel";
import * as api from "@/lib/api";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import type { FindingLlmAudit } from "@/types/explanation";

vi.mock("@/components/OperatorNavAuthorityProvider", () => ({
  useNavCallerAuthorityRank: (): number => AUTHORITY_RANK.ExecuteAuthority,
}));

describe("FindingExplainPanel", () => {
  it("loads redacted LLM audit text", async () => {
    const sample: FindingLlmAudit = {
      traceId: "abc",
      agentType: "Topology",
      systemPromptRedacted: "sys",
      userPromptRedacted: "user",
      rawResponseRedacted: "resp",
      modelDeploymentName: "sim",
      modelVersion: "1",
      redactionCountsByCategory: {},
    };

    const spy = vi.spyOn(api, "getFindingLlmAudit").mockResolvedValue(sample);
    const chainSpy = vi.spyOn(api, "getFindingEvidenceChain").mockResolvedValue({
      runId: "run-a",
      findingId: "f-1",
      manifestVersion: "v1-run",
      findingsSnapshotId: null,
      contextSnapshotId: null,
      graphSnapshotId: null,
      decisionTraceId: null,
      goldenManifestId: null,
      relatedGraphNodeIds: ["n1"],
      agentExecutionTraceIds: [],
    });
    const postSpy = vi.spyOn(api, "postFindingFeedback").mockResolvedValue(undefined);

    render(<FindingExplainPanel runId="run-a" findingId="f-1" />);

    await waitFor(() => {
      expect(screen.getByText(/sys/)).toBeInTheDocument();
    });

    expect(screen.getByText(/user/)).toBeInTheDocument();
    expect(screen.getByText(/resp/)).toBeInTheDocument();
    expect(screen.getByText(/Evidence chain/)).toBeInTheDocument();
    expect(screen.getByText("v1-run")).toBeInTheDocument();
    expect(spy).toHaveBeenCalledWith("run-a", "f-1");
    expect(chainSpy).toHaveBeenCalledWith("run-a", "f-1");
    expect(postSpy).not.toHaveBeenCalled();
    spy.mockRestore();
    chainSpy.mockRestore();
    postSpy.mockRestore();
  });

  it("posts thumbs feedback when Execute rank", async () => {
    const sample: FindingLlmAudit = {
      traceId: "abc",
      agentType: "Topology",
      systemPromptRedacted: "s",
      userPromptRedacted: "u",
      rawResponseRedacted: "r",
      redactionCountsByCategory: {},
    };

    vi.spyOn(api, "getFindingLlmAudit").mockResolvedValue(sample);
    vi.spyOn(api, "getFindingEvidenceChain").mockResolvedValue({
      runId: "run-a",
      findingId: "f-1",
      manifestVersion: null,
      findingsSnapshotId: null,
      contextSnapshotId: null,
      graphSnapshotId: null,
      decisionTraceId: null,
      goldenManifestId: null,
      relatedGraphNodeIds: [],
      agentExecutionTraceIds: [],
    });
    const postSpy = vi.spyOn(api, "postFindingFeedback").mockResolvedValue(undefined);

    render(<FindingExplainPanel runId="run-a" findingId="f-1" />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /thumbs up/i })).toBeEnabled();
    });

    fireEvent.click(screen.getByRole("button", { name: /thumbs up/i }));

    await waitFor(() => {
      expect(postSpy).toHaveBeenCalledWith("run-a", "f-1", 1);
    });

    postSpy.mockRestore();
  });
});
