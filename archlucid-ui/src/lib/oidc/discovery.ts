export type OidcDiscoveryDocument = {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  end_session_endpoint?: string;
};

const discoveryPromises = new Map<string, Promise<OidcDiscoveryDocument>>();

function discoveryUrlForAuthority(authority: string): string {
  const base = authority.replace(/\/+$/, "");

  return `${base}/.well-known/openid-configuration`;
}

export function loadDiscoveryDocument(authority: string): Promise<OidcDiscoveryDocument> {
  const url = discoveryUrlForAuthority(authority);
  const cached = discoveryPromises.get(url);

  if (cached) {
    return cached;
  }

  const promise = fetch(url, { cache: "no-store" }).then(async (response) => {
    if (!response.ok) {
      throw new Error(`OIDC discovery failed (${response.status}): ${url}`);
    }

    return (await response.json()) as OidcDiscoveryDocument;
  });

  discoveryPromises.set(url, promise);

  return promise;
}
