import type { LucideIcon } from "lucide-react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

export type EmptyStateAction = {
  label: string;
  href: string;
  variant?: "default" | "secondary" | "outline" | "ghost" | "destructive" | "link";
};

export type EmptyStateProps = {
  icon?: LucideIcon;
  title: string;
  description: string;
  actions?: EmptyStateAction[];
  helpTopicPath?: string;
};

/**
 * Centered empty collection / idle state with optional icon, CTAs, and help deep-link.
 */
export function EmptyState({ icon: Icon, title, description, actions, helpTopicPath }: EmptyStateProps) {
  const actionList = actions ?? [];

  return (
    <div role="status" aria-label={title} className="my-4">
      <Card className="border-neutral-200 bg-neutral-50/80 dark:border-neutral-700 dark:bg-neutral-900/40">
        <CardContent className="flex flex-col items-center gap-4 px-6 py-8 text-center">
          {Icon ? (
            <Icon className="h-12 w-12 shrink-0 text-teal-700 dark:text-teal-400" aria-hidden />
          ) : null}
          <h3 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">{title}</h3>
          <p className="max-w-md text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">{description}</p>
          {actionList.length > 0 ? (
            <div className="flex flex-wrap items-center justify-center gap-3">
              {actionList.map((action, index) => {
                const isPrimary = index === 0 && action.variant === undefined;
                const variant = isPrimary ? "default" : (action.variant ?? "outline");

                return (
                  <Button
                    key={`${action.href}-${action.label}`}
                    asChild
                    variant={variant}
                    className={
                      isPrimary
                        ? "bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
                        : undefined
                    }
                  >
                    <Link href={action.href}>{action.label}</Link>
                  </Button>
                );
              })}
            </div>
          ) : null}
          {helpTopicPath ? (
            <Link
              href={`/getting-started#${helpTopicPath}`}
              className="text-sm font-medium text-teal-800 underline dark:text-teal-300"
            >
              Learn more
            </Link>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
