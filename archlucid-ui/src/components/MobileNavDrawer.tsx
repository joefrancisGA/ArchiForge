"use client";

import { Menu } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { useNavProgressiveDisclosure } from "@/hooks/useNavProgressiveDisclosure";
import { NAV_GROUPS, type NavLinkItem } from "@/lib/nav-config";
import { filterNavLinksByAuthority } from "@/lib/nav-authority";
import { filterNavLinksByTier } from "@/lib/nav-tier";
import { isNavLinkActive } from "@/lib/nav-link-active";
import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";
import { cn } from "@/lib/utils";

/**
 * Hamburger + full-height drawer for small screens (sidebar is hidden below `lg`).
 */
export function MobileNavDrawer() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const { showExtended, showAdvanced } = useNavProgressiveDisclosure();
  const callerAuthorityRank = useNavCallerAuthorityRank();

  return (
    <>
      <Button
        type="button"
        variant="outline"
        size="icon"
        className="shrink-0 lg:hidden"
        aria-label="Open navigation menu"
        onClick={() => {
          setOpen(true);
        }}
      >
        <Menu className="h-5 w-5" aria-hidden />
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="!left-0 !top-0 flex h-full max-h-screen w-[min(100vw,20rem)] max-w-[min(100vw,20rem)] !translate-x-0 !translate-y-0 flex-col gap-0 overflow-y-auto rounded-none border-0 border-r border-neutral-200 p-0 shadow-xl data-[state=closed]:!slide-out-to-left-0 data-[state=open]:!slide-in-from-left-0 dark:border-neutral-700 sm:max-w-[20rem]">
          <DialogHeader className="border-b border-neutral-200 px-4 py-3 text-left dark:border-neutral-700">
            <DialogTitle className="text-base">Operator navigation</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4 px-3 py-3">
            {NAV_GROUPS.map((group) => {
              const visibleLinks: NavLinkItem[] = filterNavLinksByAuthority(
                filterNavLinksByTier(group.links, showExtended, showAdvanced),
                callerAuthorityRank,
              );

              return (
              <div key={group.id}>
                <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                  <span className="block">{group.label}</span>
                  {group.caption ? (
                    <span className="mt-0.5 block text-[10px] font-normal normal-case leading-snug tracking-normal text-neutral-500 dark:text-neutral-400">
                      {group.caption}
                    </span>
                  ) : null}
                </div>
                <nav className="flex flex-col gap-0.5" aria-label={group.label}>
                  {visibleLinks.map((link) => {
                    const active = isNavLinkActive(pathname, link.href);
                    const Icon = link.icon;

                    return (
                      <Link
                        key={link.href}
                        href={link.href}
                        className={cn(
                          "shell-nav-link flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800",
                          active
                            ? "bg-teal-50 font-semibold text-teal-900 dark:bg-teal-900/30 dark:text-teal-200"
                            : "text-neutral-800 dark:text-neutral-200",
                        )}
                        title={link.title}
                        aria-current={active ? "page" : undefined}
                        aria-keyshortcuts={
                          link.keyShortcut ? registryKeyToAriaKeyShortcuts(link.keyShortcut) : undefined
                        }
                        onClick={() => {
                          setOpen(false);
                        }}
                      >
                        {Icon ? <Icon className="h-4 w-4 shrink-0 opacity-90" aria-hidden /> : null}
                        {link.label}
                      </Link>
                    );
                  })}
                </nav>
              </div>
            );
            })}
            <p className="text-xs text-neutral-600 dark:text-neutral-400" aria-keyshortcuts="Shift+?">
              Press Shift+? for help and keyboard shortcuts
            </p>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
