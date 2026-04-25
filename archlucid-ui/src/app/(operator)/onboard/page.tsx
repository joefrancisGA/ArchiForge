import { redirect } from "next/navigation";

/** @deprecated The four-step first-session flow lives in New run and run detail; canonical checklist is `/getting-started`. */
export default function OnboardRedirectPage() {
  redirect("/getting-started");
}
