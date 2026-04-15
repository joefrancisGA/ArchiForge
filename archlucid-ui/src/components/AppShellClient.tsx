"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";

import { AppToaster } from "@/components/AppToaster";
import { AuthPanel } from "@/components/AuthPanel";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { ColorModeToggle } from "@/components/ColorModeToggle";
import { CommandPalette } from "@/components/CommandPalette";
import { HelpPanel } from "@/components/HelpPanel";
import { KeyboardShortcutProvider } from "@/components/KeyboardShortcutProvider";
import { MobileNavDrawer } from "@/components/MobileNavDrawer";
import { SidebarNav } from "@/components/SidebarNav";
import { Button } from "@/components/ui/button";

type AppShellClientProps = {
  children: ReactNode;
};

/**
 * Operator shell: sticky header (logo, command palette, theme), breadcrumbs, collapsible sidebar (lg+),
 * mobile drawer, auth strip, keyboard shortcuts, main landmark.
 */
export function AppShellClient({ children }: AppShellClientProps) {
  const [helpOpen, setHelpOpen] = useState(false);

  return (
    <>
      <a href="#main-content" className="skip-to-main">
        Skip to main content
      </a>
      <div className="flex min-h-screen flex-col bg-neutral-50 dark:bg-neutral-950">
        <header className="sticky top-0 z-30 border-b border-neutral-200 bg-neutral-50/95 backdrop-blur dark:border-neutral-700 dark:bg-neutral-950/95">
          <div className="mx-auto flex max-w-[1600px] flex-wrap items-center gap-3 px-4 py-3 lg:px-6">
            <div className="flex min-w-0 flex-1 items-center gap-3">
              <MobileNavDrawer />
              <h1 className="text-2xl font-semibold tracking-tight">
                <Button variant="ghost" className="h-auto p-0 text-2xl font-semibold" asChild>
                  <Link href="/" aria-label="ArchLucid — go to operator home">
                    ArchLucid
                  </Link>
                </Button>
              </h1>
            </div>
            <div className="flex flex-wrap items-center justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="hidden sm:inline-flex"
                aria-label="Open help"
                onClick={() => {
                  setHelpOpen(true);
                }}
              >
                Help
              </Button>
              <CommandPalette />
              <kbd
                className="hidden rounded border border-neutral-300 bg-white px-1.5 py-0.5 font-mono text-[0.7rem] text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-400 sm:inline-block"
                aria-hidden
              >
                Ctrl+K
              </kbd>
              <ColorModeToggle />
            </div>
          </div>
          <div className="mx-auto max-w-[1600px] border-t border-neutral-100 px-4 pb-2 pt-1 dark:border-neutral-800 lg:px-6">
            <Breadcrumbs />
          </div>
        </header>
        <div className="mx-auto flex w-full max-w-[1600px] flex-1">
          <aside className="hidden w-[15.5rem] shrink-0 overflow-y-auto border-r border-neutral-200 bg-neutral-50/80 px-2 py-4 dark:border-neutral-800 dark:bg-neutral-950/80 lg:block">
            <SidebarNav />
          </aside>
          <div className="min-w-0 flex-1 px-4 py-4 lg:px-6 lg:py-6">
            <AuthPanel />
            <KeyboardShortcutProvider
              onHelpRequested={() => {
                setHelpOpen(true);
              }}
            >
              <div
                id="main-content"
                tabIndex={-1}
                className="outline-none focus:outline-none focus-visible:ring-2 focus-visible:ring-neutral-400 focus-visible:ring-offset-2 dark:focus-visible:ring-neutral-600"
              >
                {children}
              </div>
            </KeyboardShortcutProvider>
          </div>
        </div>
      </div>
      <AppToaster />
      <HelpPanel open={helpOpen} onOpenChange={setHelpOpen} />
    </>
  );
}
