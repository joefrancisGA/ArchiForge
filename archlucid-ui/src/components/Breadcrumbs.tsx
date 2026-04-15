"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { getBreadcrumbs } from "@/lib/breadcrumb-map";

/**
 * Location-aware breadcrumb trail (client — uses `usePathname`). Hidden on home only.
 */
export function Breadcrumbs() {
  const pathname = usePathname() ?? "/";
  const items = getBreadcrumbs(pathname);

  if (items.length <= 1) {
    return null;
  }

  return (
    <nav aria-label="Breadcrumb" className="text-sm text-neutral-600 dark:text-neutral-400">
      <ol className="m-0 flex flex-wrap items-center gap-1 p-0 list-none">
        {items.map((item, index) => (
          <li key={`${item.label}-${index}`} className="flex items-center gap-1">
            {index > 0 ? (
              <span className="text-neutral-400 dark:text-neutral-500" aria-hidden>
                /
              </span>
            ) : null}
            {item.href ? (
              <Link
                href={item.href}
                className="text-teal-800 underline decoration-teal-700/40 underline-offset-2 hover:text-teal-950 dark:text-teal-300 dark:hover:text-teal-100"
              >
                {item.label}
              </Link>
            ) : (
              <span
                className="font-medium text-neutral-900 dark:text-neutral-100"
                aria-current="page"
              >
                {item.label}
              </span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}
