import { AdvisoryHubClient } from "@/components/advisory/AdvisoryHubClient";
import { advisoryHubTabFromSearchParam } from "@/lib/advisory-hub-tab";

type PageProps = {
  searchParams: Promise<{ tab?: string }>;
};

/**
 * Server resolves `?tab=` so the client hub mounts without a long Suspense wait on `useSearchParams`.
 */
export default async function AdvisoryPage(props: PageProps) {
  const p = await props.searchParams;
  const initialTab = advisoryHubTabFromSearchParam(p.tab ?? null);

  return <AdvisoryHubClient initialTab={initialTab} />;
}
