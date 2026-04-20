import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { TooltipProvider } from "@/components/ui/tooltip";
import { GLOSSARY_TERMS } from "@/lib/glossary-terms";

import { GlossaryTerm } from "./GlossaryTerm";

describe("GlossaryTerm", () => {
  it("shows glossary definition on hover", async () => {
    render(
      <TooltipProvider delayDuration={0}>
        <GlossaryTerm termId="governance_workflow">Governance workflow</GlossaryTerm>
      </TooltipProvider>,
    );

    fireEvent.pointerMove(screen.getByText("Governance workflow"));

    expect(
      (await screen.findAllByText(GLOSSARY_TERMS.governance_workflow, { exact: true })).length,
    ).toBeGreaterThan(0);
  });
});
