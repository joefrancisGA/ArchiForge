import { describe, expect, it } from "vitest";

import { decodeJwtPayload, pickDisplayNameFromPayload } from "@/lib/oidc/jwt-payload";

describe("decodeJwtPayload", () => {
  it("decodes a minimal JWT payload", () => {
    const payload = { sub: "u1", name: "Test User" };
    const b64 = Buffer.from(JSON.stringify(payload), "utf8").toString("base64url");
    const jwt = `x.${b64}.y`;
    const decoded = decodeJwtPayload(jwt);

    expect(decoded).toEqual(payload);
  });

  it("returns null for invalid input", () => {
    expect(decodeJwtPayload("not-a-jwt")).toBeNull();
    expect(decodeJwtPayload("")).toBeNull();
  });
});

describe("pickDisplayNameFromPayload", () => {
  it("prefers preferred_username", () => {
    expect(
      pickDisplayNameFromPayload({
        preferred_username: "alice@contoso.com",
        name: "Alice",
      }),
    ).toBe("alice@contoso.com");
  });

  it("falls back to sub", () => {
    expect(pickDisplayNameFromPayload({ sub: "abc-123" })).toBe("abc-123");
  });
});
