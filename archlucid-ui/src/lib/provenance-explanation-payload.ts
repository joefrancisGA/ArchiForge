export type ExplanationPayloadFields = Readonly<{
  message: string | null;
  detail: string | null;
  title: string | null;
}>;

/** Coordinator provenance explanation: 501 stub uses `{ message }`; RFC 7807 uses `detail` / `title`. */
export function parseProvenanceExplanationPayload(raw: unknown): ExplanationPayloadFields {
  if (raw === null || typeof raw !== "object")
    return { message: null, detail: null, title: null };

  const body = raw as Record<string, unknown>;

  return {
    message: typeof body.message === "string" ? body.message : null,
    detail: typeof body.detail === "string" ? body.detail : null,
    title: typeof body.title === "string" ? body.title : null,
  };
}
