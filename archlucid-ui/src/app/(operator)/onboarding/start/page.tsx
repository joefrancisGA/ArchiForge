import { redirect } from "next/navigation";

type OnboardingStartRedirectPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

function buildDestinationQuery(searchParams: Record<string, string | string[] | undefined>): string {
  const u = new URL("http://local");
  u.pathname = "/getting-started";

  for (const [key, value] of Object.entries(searchParams)) {
    if (value === undefined) continue;

    if (Array.isArray(value)) for (const v of value) u.searchParams.append(key, v);
    else u.searchParams.set(key, value);
  }

  return `${u.pathname}${u.search}`;
}

/**
 * Preserves query (e.g. `source=registration` from handoff) while moving to the canonical getting-started page.
 * @deprecated Bookmarks to `/onboarding/start` still work.
 */
export default async function OnboardingStartRedirectPage({ searchParams }: OnboardingStartRedirectPageProps) {
  const resolved = await searchParams;
  const dest = buildDestinationQuery(resolved);

  redirect(dest);
}
