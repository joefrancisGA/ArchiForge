"use client";

import { useState, type ReactNode } from "react";

import { ArchLucidWordmarkLink } from "@/components/ArchLucidWordmarkLink";
import { AppToaster } from "@/components/AppToaster";
import { AuthPanel } from "@/components/AuthPanel";
import { Breadcrumbs } from "@/components/Breadcrumbs";
import { ColorModeToggle } from "@/components/ColorModeToggle";
import { CommandPalette } from "@/components/CommandPalette";
import { HelpPanel } from "@/components/HelpPanel";
import { KeyboardShortcutProvider } from "@/components/KeyboardShortcutProvider";
import { LayerContextFromRoute } from "@/components/LayerContextFromRoute";
import { MobileNavDrawer } from "@/components/MobileNavDrawer";
import { ScopeSwitcher } from "@/components/ScopeSwitcher";
import { OperatorNavAuthorityProvider } from "@/components/OperatorNavAuthorityProvider";
import { RouteAnnouncer } from "@/components/RouteAnnouncer";
import { SidebarNav } from "@/components/SidebarNav";
import { TrialBanner } from "@/components/TrialBanner";
import { Button } from "@/components/ui/button";
import { TooltipProvider } from "@/components/ui/tooltip";
import { useRouteChangeFocus } from "@/hooks/useRouteChangeFocus";

type AppShellClientProps = {
  children: ReactNode;
};

/**
 * Operator shell: sticky header (logo, command palette, theme), breadcrumbs, collapsible sidebar (lg+),
 * mobile drawer, auth strip, keyboard shortcuts, main landmark.
 */
export function AppShellClient({ children }: AppShellClientProps) {
  const [helpOpen, setHelpOpen] = useState(false);
  useRouteChangeFocus("main-content");

  return (
    <OperatorNavAuthorityProvider>
      <TooltipProvider delayDuration={200}>
        <a href="#main-content" className="skip-to-main">
          Skip to main content
        </a>
        <div className="flex min-h-screen flex-col bg-neutral-50 dark:bg-neutral-950">
          <header data-testid="app-shell-topbar" className="sticky top-0 z-30 border-b border-neutral-200 bg-neutral-50/95 backdrop-blur print:hidden dark:border-neutral-700 dark:bg-neutral-950/95">
            <div className="mx-auto flex max-w-[1600px] items-center gap-3 px-4 py-2.5 lg:px-6">
              <div className="flex min-w-0 flex-1 items-center gap-3">
                <MobileNavDrawer />
                <h1 className="m-0">
                  <Button variant="ghost" className="h-auto p-0" asChild>
                    <ArchLucidWordmarkLink href="/" aria-label="ArchLucid — go to operator home" variant="operator" />
                  </Button>
                </h1>
                <div className="hidden min-w-0 flex-1 items-center gap-1.5 pl-2 lg:flex">
                  <Breadcrumbs />
                </div>
              </div>
              <div className="flex shrink-0 items-center gap-1.5">
                <ScopeSwitcher />
                <CommandPalette />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="hidden h-8 w-8 p-0 sm:inline-flex"
                  aria-label="Open help"
                  onClick={() => {
                    setHelpOpen(true);
                  }}
                >
                  <span className="text-xs">?</span>
                </Button>
                <ColorModeToggle />
              </div>
            </div>
          </header>
          <LayerContextFromRoute />
          <div className="mx-auto flex w-full max-w-[1600px] flex-1">
            <aside data-testid="sidebar-nav" className="hidden w-[15.5rem] shrink-0 overflow-y-auto border-r border-neutral-200 bg-neutral-50/80 px-2 py-4 print:!hidden dark:border-neutral-800 dark:bg-neutral-950/80 lg:block">
              <SidebarNav />
            </aside>
            <div data-testid="app-shell-main" className="min-w-0 flex-1 px-4 py-4 print:px-0 lg:px-6 lg:py-6">
              <AuthPanel />
              <TrialBanner />
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
        <RouteAnnouncer />
        <HelpPanel open={helpOpen} onOpenChange={setHelpOpen} />
      </TooltipProvider>
    </OperatorNavAuthorityProvider>
  );
}
