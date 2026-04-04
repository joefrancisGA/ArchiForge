function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";

  for (let i = 0; i < bytes.length; i++) {
    binary += String.fromCharCode(bytes[i]!);
  }

  const base64 = btoa(binary);

  return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/u, "");
}

/**
 * RFC 7636: random 32-byte verifier, S256 challenge for authorization code + public client.
 */
export async function createPkcePair(): Promise<{ verifier: string; challenge: string }> {
  const verifierBytes = new Uint8Array(32);

  crypto.getRandomValues(verifierBytes);
  const verifier = base64UrlEncode(verifierBytes);
  const encoder = new TextEncoder();
  const digest = await crypto.subtle.digest("SHA-256", encoder.encode(verifier));

  return {
    verifier,
    challenge: base64UrlEncode(new Uint8Array(digest)),
  };
}

export function randomOpaqueState(): string {
  const bytes = new Uint8Array(16);

  crypto.getRandomValues(bytes);

  return base64UrlEncode(bytes);
}
