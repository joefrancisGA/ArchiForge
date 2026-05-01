import { fireEvent, render, screen, waitFor } from "@testing-library/react";

import { describe, expect, it, vi } from "vitest";



import { CopyFindingAsWorkItemButton } from "@/components/CopyFindingAsWorkItemButton";

import type { FindingInspectPayload } from "@/types/finding-inspect";



describe("CopyFindingAsWorkItemButton", () => {

  it("calls clipboard.writeText with Markdown work item body", async () => {

    const writeText = vi.fn().mockResolvedValue(undefined);



    Object.defineProperty(navigator, "clipboard", {

      configurable: true,

      value: { writeText },

    });



    const payload: FindingInspectPayload = {

      findingId: "fid-1",

      typedPayload: {

        severity: "High",

        category: "Cost",

        title: "Over-provisioned",

        description: "Too many SKU.",

      },

      decisionRuleId: "r1",

      decisionRuleName: "SKU check",

      evidence: [{ artifactId: "a", lineRange: "1-2", excerpt: "log" }],

      auditRowId: null,

      runId: "run-42",

      manifestVersion: "v9",

    };



    render(<CopyFindingAsWorkItemButton findingId="fid-1" payload={payload} runId="run-42" />);



    fireEvent.click(
      screen.getByRole("button", { name: /create remediation ticket — copy formatted text to clipboard/i }),
    );



    await waitFor(() => {

      expect(writeText).toHaveBeenCalled();

    });



    const body = writeText.mock.calls[0]?.[0] ?? "";

    expect(body).toContain("## Finding: Cost — Over-provisioned");

    expect(body).toContain("`fid-1`");

    expect(body).toContain("/reviews/run-42");

  });

});


