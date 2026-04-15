"use client";

import { ChevronDown } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { NAV_GROUPS } from "@/lib/nav-config";
import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";
import { cn } from "@/lib/utils";

const STORAGE_PREFIX = "archlucid_sidebar_group_";

/**
 * Collapsible grouped sidebar navigation (desktop). Group open state persists in localStorage.
 */
export function SidebarNav() {
  const [mounted, setMounted] = useState(false);
  const [openByGroup, setOpenByGroup] = useState<Record<string, boolean>>({});

  useEffect(() => {
    const next: Record<string, boolean> = {};

    for (const group of NAV_GROUPS) {
      try {
        if (typeof window !== "undefined") {
          const raw = window.localStorage.getItem(STORAGE_PREFIX + group.id);
          next[group.id] = raw !== "0";
        } else {
          next[group.id] = true;
        }
      } catch {
        next[group.id] = true;
      }
    }

    setOpenByGroup(next);
    setMounted(true);
  }, []);

  function setGroupOpen(groupId: string, value: boolean): void {
    setOpenByGroup((prev) => ({ ...prev, [groupId]: value }));

    try {
      window.localStorage.setItem(STORAGE_PREFIX + groupId, value ? "1" : "0");
    } catch {
      /* private mode */
    }
  }

  return (
    <div className="flex h-full flex-col gap-1 pb-6 pr-1">
      {NAV_GROUPS.map((group) => {
        const isOpen = !mounted || openByGroup[group.id] !== false;

        return (
          <Collapsible
            key={group.id}
            open={isOpen}
            onOpenChange={(next) => {
              setGroupOpen(group.id, next);
            }}
          >
            <CollapsibleTrigger
              className="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-xs font-semibold uppercase tracking-wide text-neutral-500 hover:bg-neutral-100 dark:text-neutral-400 dark:hover:bg-neutral-800"
              type="button"
            >
              <span>{group.label}</span>
              <ChevronDown
                className={cn("h-4 w-4 shrink-0 transition-transform", isOpen ? "rotate-0" : "-rotate-90")}
                aria-hidden
              />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <nav
                className="flex flex-col gap-0.5 border-l border-neutral-200 py-1 pl-2 dark:border-neutral-700"
                aria-label={group.label}
              >
                {group.links.map((link) => (
                  <Link
                    key={link.href}
                    href={link.href}
                    className="shell-nav-link rounded-md px-2 py-1 text-sm text-neutral-800 hover:bg-neutral-100 dark:text-neutral-200 dark:hover:bg-neutral-800"
                    title={link.title}
                    aria-keyshortcuts={
                      link.keyShortcut ? registryKeyToAriaKeyShortcuts(link.keyShortcut) : undefined
                    }
                  >
                    {link.label}
                  </Link>
                ))}
              </nav>
            </CollapsibleContent>
          </Collapsible>
        );
      })}
      <p
        className="mt-3 border-t border-neutral-200 pt-3 text-xs text-neutral-600 dark:border-neutral-700 dark:text-neutral-400"
        aria-keyshortcuts="Shift+?"
      >
        Press Shift+? for keyboard shortcuts
      </p>
    </div>
  );
}
