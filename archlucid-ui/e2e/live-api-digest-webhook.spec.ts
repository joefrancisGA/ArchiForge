/**
 * Live API + SQL: digest subscriptions (create → list → toggle) and durable audit types.
 * There is no DELETE on digest subscriptions in the product API; webhook "dry send" is not exposed over HTTP.
 */
import { expect, test } from "@playwright/test";

import {
  createDigestSubscription,
  listDigestSubscriptions,
  liveApiBase,
  searchAudit,
  toggleDigestSubscription,
} from "./helpers/live-api-client";

const forensics: { subscriptionId?: string; subscriptionName?: string } = {};

test.describe("live-api-digest-webhook", () => {
  test.afterAll(() => {
    if (forensics.subscriptionId) {
      console.log(
        `[live-api-digest-webhook] subscriptionId=${forensics.subscriptionId} name=${forensics.subscriptionName ?? ""}`,
      );
    }
  });

  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("digest subscription create → list → toggle with audit DigestSubscriptionCreated and DigestSubscriptionToggled", async ({
    request,
  }) => {
    test.setTimeout(120_000);

    const name = `e2e-digest-${Date.now()}`;
    const created = await createDigestSubscription(request, {
      name,
      channelType: "Email",
      destination: "e2e-digest@example.invalid",
      isEnabled: true,
      metadataJson: JSON.stringify({ liveE2e: true }),
    });

    const subscriptionId = created.subscriptionId;

    if (!subscriptionId) {
      throw new Error("Create digest subscription response missing subscriptionId");
    }

    forensics.subscriptionId = subscriptionId;
    forensics.subscriptionName = name;
    test.info().annotations.push({ type: "e2e-digest-subscription-id", description: subscriptionId });

    const listed = await listDigestSubscriptions(request);
    const hit = listed.find((s) => s.subscriptionId === subscriptionId || s.name === name);

    expect(hit, "GET /v1/digest-subscriptions should include the new subscription").toBeDefined();

    const beforeToggle = await searchAudit(request, { eventType: "DigestSubscriptionCreated", take: "100" });

    expect(
      beforeToggle.some((e) => e.eventType === "DigestSubscriptionCreated"),
      "Expected DigestSubscriptionCreated in audit search",
    ).toBe(true);

    const toggled = await toggleDigestSubscription(request, subscriptionId);

    expect(typeof toggled.isEnabled).toBe("boolean");

    const afterToggle = await searchAudit(request, { eventType: "DigestSubscriptionToggled", take: "100" });

    expect(
      afterToggle.some((e) => e.eventType === "DigestSubscriptionToggled"),
      "Expected DigestSubscriptionToggled after toggle",
    ).toBe(true);
  });

  test("webhook delivery dry-run — not exposed on ArchLucid.Api", () => {
    test.skip(
      true,
      "No HTTP dry-run for digest/webhook delivery in ArchLucid.Api; delivery is driven by AdvisoryScanRunner + IDigestDeliveryDispatcher in the worker.",
    );
  });
});
