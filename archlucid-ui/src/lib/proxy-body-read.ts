/**
 * When Content-Length is present and parseable, reject before buffering if it exceeds the cap.
 * Returns `false` when absent, invalid, or within limit (caller should stream-read with a limit).
 */
export function declaredPostBodyExceedsLimit(
  contentLengthHeader: string | null,
  maxBytes: number,
): false | { declaredLength: number } {
  if (contentLengthHeader === null || contentLengthHeader.trim() === "") {
    return false;
  }

  const declaredLength = Number(contentLengthHeader);

  if (Number.isNaN(declaredLength)) {
    return false;
  }

  if (declaredLength > maxBytes) {
    return { declaredLength };
  }

  return false;
}

/**
 * Reads a request body stream as UTF-8 text, enforcing a maximum byte size.
 *
 * @returns Joined text, `""` when {@link body} is null/undefined, or `null` if the limit is exceeded.
 */
export async function readRequestBodyWithLimit(
  body: ReadableStream<Uint8Array> | null | undefined,
  maxBytes: number,
): Promise<string | null> {
  if (body == null) {
    return "";
  }

  const reader = body.getReader();
  const decoder = new TextDecoder();
  const chunks: string[] = [];
  let totalBytes = 0;

  for (;;) {
    const { done, value } = await reader.read();

    if (done) {
      break;
    }

    totalBytes += value.byteLength;

    if (totalBytes > maxBytes) {
      await reader.cancel();
      return null;
    }

    chunks.push(decoder.decode(value, { stream: true }));
  }

  chunks.push(decoder.decode());
  return chunks.join("");
}
