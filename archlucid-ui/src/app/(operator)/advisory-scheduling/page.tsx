import { redirect } from "next/navigation";

/**
 * Legacy route: scan schedules live on the **Advisory** hub **Schedules** tab.
 */
export default function AdvisorySchedulingRedirect() {
  redirect("/advisory?tab=schedules");
}
