import { redirect } from "next/navigation";

/**
 * Legacy route: executive digest schedule lives on the **Digests** hub **Schedule** tab.
 */
export default function ExecDigestSettingsRedirect() {
  redirect("/digests?tab=schedule");
}
