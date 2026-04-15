"use client";

import { Menu } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NAV_GROUPS } from "@/lib/nav-config";
import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";

/**
 * Hamburger + full-height drawer for small screens (sidebar is hidden below `lg`).
 */
export function MobileNavDrawer() {
  const [open, setOpen] = useState(false);

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
            {NAV_GROUPS.map((group) => (
              <div key={group.id}>
                <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
                  {group.label}
                </div>
                <nav className="flex flex-col gap-0.5" aria-label={group.label}>
                  {group.links.map((link) => (
                    <Link
                      key={link.href}
                      href={link.href}
                      className="shell-nav-link rounded-md px-2 py-1.5 text-sm text-neutral-800 hover:bg-neutral-100 dark:text-neutral-200 dark:hover:bg-neutral-800"
                      title={link.title}
                      aria-keyshortcuts={
                        link.keyShortcut ? registryKeyToAriaKeyShortcuts(link.keyShortcut) : undefined
                      }
                      onClick={() => {
                        setOpen(false);
                      }}
                    >
                      {link.label}
                    </Link>
                  ))}
                </nav>
              </div>
            ))}
            <p className="text-xs text-neutral-600 dark:text-neutral-400" aria-keyshortcuts="Shift+?">
              Press Shift+? for keyboard shortcuts
            </p>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
