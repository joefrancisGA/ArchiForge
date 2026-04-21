/**
 * Canonical JSON bodies for Azure DevOps Git PR threads + statuses (REST 7.1).
 * Must stay byte-identical to `AzureDevOpsPullRequestWireFormat` in C# (ADR 0024).
 */
export function buildThreadCreateJson(markdown) {
  return JSON.stringify({
    comments: [{ parentCommentId: 0, content: markdown ?? "", commentType: 1 }],
    status: 1,
  });
}

export function buildStatusCreateJson(description, targetUrl) {
  let desc = description ?? "";
  if (desc.length > 512) desc = desc.slice(0, 512);

  const o = {
    state: "succeeded",
    description: desc,
    context: { name: "archlucid-manifest", genre: "archlucid" },
  };

  const t = targetUrl && String(targetUrl).trim();
  if (t) o.targetUrl = t;

  return JSON.stringify(o);
}
