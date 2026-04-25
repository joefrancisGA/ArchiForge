import { redirect } from "next/navigation";

/** @deprecated Use `/getting-started` — kept for bookmarks. */
export default function OnboardingRedirectPage() {
  redirect("/getting-started");
}
