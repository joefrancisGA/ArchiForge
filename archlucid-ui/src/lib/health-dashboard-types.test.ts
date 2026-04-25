import { describe, expect, it } from "vitest";

import { findCircuitBreakersEntry, parseCircuitGatesFromHealthEntry } from "./health-dashboard-types";

describe("parseCircuitGatesFromHealthEntry", () => {
  it("maps gates from circuit_breakers data payload", () => {
    const rows = parseCircuitGatesFromHealthEntry({
      gates: [
        {
          name: "openai completion",
          state: "Closed",
          breakDurationSeconds: 30,
        },
      ],
    });
    expect(rows).toHaveLength(1);
    expect(rows[0]?.name).toBe("openai completion");
    expect(rows[0]?.state).toBe("Closed");
    expect(rows[0]?.breakDurationSeconds).toBe(30);
  });
});

describe("findCircuitBreakersEntry", () => {
  it("returns the named entry", () => {
    const e = findCircuitBreakersEntry([
      { name: "database", status: "Healthy" },
      { name: "circuit_breakers", status: "Healthy", data: { gates: [] } },
    ]);
    expect(e?.name).toBe("circuit_breakers");
  });
});
