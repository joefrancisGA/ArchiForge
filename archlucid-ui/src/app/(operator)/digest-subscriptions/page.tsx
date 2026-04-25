import { redirect } from "next/navigation";

/**
 * Legacy route: digest delivery config lives on the **Digests** hub **Subscriptions** tab.
 */
export default function DigestSubscriptionsRedirect() {
  redirect("/digests?tab=subscriptions");
}
