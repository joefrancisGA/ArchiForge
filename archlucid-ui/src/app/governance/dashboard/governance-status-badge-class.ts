/** Tailwind classes for governance workflow status badges (dashboard + tables). */
export function governanceStatusBadgeClass(status: string): string {
  switch (status) {
    case "Submitted":
      return "border-transparent bg-blue-600 text-white hover:bg-blue-600/90 dark:bg-blue-600 dark:hover:bg-blue-600/90";
    case "Approved":
      return "border-transparent bg-emerald-600 text-white hover:bg-emerald-600/90 dark:bg-emerald-600 dark:hover:bg-emerald-600/90";
    case "Rejected":
      return "border-transparent bg-red-600 text-white hover:bg-red-600/90 dark:bg-red-600 dark:hover:bg-red-600/90";
    case "Promoted":
      return "border-transparent bg-violet-600 text-white hover:bg-violet-600/90 dark:bg-violet-600 dark:hover:bg-violet-600/90";
    case "Activated":
      return "border-transparent bg-teal-600 text-white hover:bg-teal-600/90 dark:bg-teal-600 dark:hover:bg-teal-600/90";
    case "Draft":
    default:
      return "border-oklch(0.922 0 0) bg-oklch(0.97 0 0) text-oklch(0.205 0 0) dark:border-oklch(1 0 0 / 10%) dark:bg-oklch(0.269 0 0) dark:text-oklch(0.985 0 0)";
  }
}
