import { redirect } from "next/navigation";

/**
 * List and lifecycle live on `/policy-packs`. This index exists so `/governance/policy-packs` bookmarks
 * and breadcrumb targets resolve instead of 404 — avoids operators landing on unrelated governance pages.
 */
export default function GovernancePolicyPacksIndexPage() {
  redirect("/policy-packs");
}
