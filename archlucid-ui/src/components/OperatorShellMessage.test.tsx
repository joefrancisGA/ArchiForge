import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { OperatorTryNext } from "@/components/OperatorShellMessage";

describe("OperatorTryNext", () => {
  it("renders Try next label and children", () => {
    render(
      <OperatorTryNext>
        Check <code>GET /health/live</code> then reload.
      </OperatorTryNext>,
    );

    expect(screen.getByText("Try next:")).toBeInTheDocument();
    expect(screen.getByText(/GET \/health\/live/)).toBeInTheDocument();
  });
});
