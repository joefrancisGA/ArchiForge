import { describe, expect, it } from "vitest";

import { replayModeLabel, sortReplayNotes } from "./replay-display";

describe("replayModeLabel", () => {
  it("returns known descriptions", () => {
    expect(replayModeLabel("ReconstructOnly")).toContain("Reconstruct only");
    expect(replayModeLabel("RebuildManifest")).toContain("Rebuild manifest");
  });

  it("falls back to raw mode", () => {
    expect(replayModeLabel("UnknownMode")).toBe("UnknownMode");
  });
});

describe("sortReplayNotes", () => {
  it("sorts lines with en locale", () => {
    expect(sortReplayNotes(["b", "a", "a2"])).toEqual(["a", "a2", "b"]);
  });
});
