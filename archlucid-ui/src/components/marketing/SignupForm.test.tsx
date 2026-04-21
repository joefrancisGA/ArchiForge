import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const pushMock = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: pushMock }),
}));

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

import { SignupForm } from "./SignupForm";

describe("SignupForm", () => {
  it("blocks submit when validation fails", async () => {
    render(<SignupForm />);

    fireEvent.click(screen.getByRole("button", { name: /Create trial workspace/i }));

    expect(await screen.findByText(/Enter a valid email/i)).toBeInTheDocument();
    expect(pushMock).not.toHaveBeenCalled();
  });

  it("submits valid payload to the same-origin proxy", async () => {
    const fetchMock = vi.fn(async () => {
      return new Response(
        JSON.stringify({
          tenantId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          defaultWorkspaceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          defaultProjectId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          wasAlreadyProvisioned: false,
        }),
        { status: 201, headers: { "Content-Type": "application/json" } },
      );
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<SignupForm />);

    fireEvent.change(screen.getByLabelText(/Work email/i), { target: { value: "ops@example.com" } });
    fireEvent.change(screen.getByLabelText(/Full name/i), { target: { value: "Ops User" } });
    fireEvent.change(screen.getByLabelText(/Organization name/i), { target: { value: "Contoso Trial Org" } });
    fireEvent.click(screen.getByRole("button", { name: /Create trial workspace/i }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled();
    });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/proxy/v1/register");
    expect(init.method).toBe("POST");
    const body = JSON.parse(String(init.body)) as Record<string, unknown>;
    expect(body.adminEmail).toBe("ops@example.com");
    expect(body.organizationName).toBe("Contoso Trial Org");

    await waitFor(() => {
      expect(pushMock).toHaveBeenCalledWith(expect.stringContaining("/signup/verify?email="));
    });

    expect(body.baselineReviewCycleHours).toBeUndefined();
    expect(body.baselineReviewCycleSource).toBeUndefined();

    vi.unstubAllGlobals();
  });

  it("forwards baseline hours when custom path is selected", async () => {
    const fetchMock = vi.fn(async () => {
      return new Response(
        JSON.stringify({
          tenantId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          defaultWorkspaceId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          defaultProjectId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          wasAlreadyProvisioned: false,
        }),
        { status: 201, headers: { "Content-Type": "application/json" } },
      );
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<SignupForm />);

    fireEvent.change(screen.getByLabelText(/Work email/i), { target: { value: "ops@example.com" } });
    fireEvent.change(screen.getByLabelText(/Full name/i), { target: { value: "Ops User" } });
    fireEvent.change(screen.getByLabelText(/Organization name/i), { target: { value: "Contoso Trial Org" } });

    fireEvent.click(screen.getByTestId("signup-baseline-choice-custom"));

    fireEvent.change(screen.getByTestId("signup-baseline-hours"), { target: { value: "16" } });
    fireEvent.change(screen.getByTestId("signup-baseline-source"), { target: { value: "team estimate" } });

    fireEvent.click(screen.getByRole("button", { name: /Create trial workspace/i }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled();
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const body = JSON.parse(String(init.body)) as Record<string, unknown>;
    expect(body.baselineReviewCycleHours).toBe(16);
    expect(body.baselineReviewCycleSource).toBe("team estimate");

    vi.unstubAllGlobals();
  });
});
