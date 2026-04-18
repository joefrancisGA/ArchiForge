"use client";

import { ChevronDown, Settings2 } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { Label } from "@/components/ui/label";
import { EnterpriseControlsNavGroupHint } from "@/components/EnterpriseControlsContextHints";
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { useNavProgressiveDisclosure } from "@/hooks/useNavProgressiveDisclosure";
import { NAV_GROUPS, type NavLinkItem } from "@/lib/nav-config";
import { filterNavLinksForOperatorShell } from "@/lib/nav-shell-visibility";
import { isNavLinkActive } from "@/lib/nav-link-active";
import { registryKeyToAriaKeyShortcuts } from "@/lib/shortcut-registry";
import { cn } from "@/lib/utils";

const STORAGE_PREFIX = "archlucid_sidebar_group_";

/** Alerts & governance is collapsed by default until the user explicitly opens it (localStorage "1"). */
function readGroupOpenFromStorage(groupId: string, raw: string | null): boolean {
  if (groupId === "alerts-governance") {
    return raw === "1";
  }

  return raw !== "0";
}

/**
 * Collapsible grouped sidebar navigation (desktop). Group open state persists in localStorage.
 * Progressive disclosure: essential links always; extended/advanced via toggles.
 */
export function SidebarNav() {
  const pathname = usePathname();
  const [mounted, setMounted] = useState(false);
  const [openByGroup, setOpenByGroup] = useState<Record<string, boolean>>({});
  const { showExtended, showAdvanced, setShowExtended, setShowAdvanced } = useNavProgressiveDisclosure();
  const callerAuthorityRank = useNavCallerAuthorityRank();
  const [settingsOpen, setSettingsOpen] = useState(false);

  useEffect(() => {
    const next: Record<string, boolean> = {};

    for (const group of NAV_GROUPS) {
      try {
        if (typeof window !== "undefined") {
          const raw = window.localStorage.getItem(STORAGE_PREFIX + group.id);
          next[group.id] = readGroupOpenFromStorage(group.id, raw);
        } else {
          next[group.id] = group.id !== "alerts-governance";
        }
      } catch {
        next[group.id] = group.id !== "alerts-governance";
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
        const visibleLinks: NavLinkItem[] = filterNavLinksForOperatorShell(
          group.links,
          showExtended,
          showAdvanced,
          callerAuthorityRank,
        );

        if (visibleLinks.length === 0) {
          return null;
        }

        return (
          <Collapsible
            key={group.id}
            open={isOpen}
            onOpenChange={(next) => {
              setGroupOpen(group.id, next);
            }}
          >
            <CollapsibleTrigger
              aria-label={group.label}
              className="flex w-full items-start justify-between gap-2 rounded-md px-2 py-1.5 text-left text-xs font-semibold uppercase tracking-wide text-neutral-500 hover:bg-neutral-100 dark:text-neutral-400 dark:hover:bg-neutral-800"
              type="button"
            >
              <span className="flex min-w-0 flex-1 flex-col items-start gap-0.5">
                <span>{group.label}</span>
                {group.caption ? (
                  <span className="max-w-[14rem] text-[10px] font-normal normal-case leading-snug tracking-normal text-neutral-500 dark:text-neutral-400">
                    {group.caption}
                  </span>
                ) : null}
                {group.id === "alerts-governance" ? <EnterpriseControlsNavGroupHint /> : null}
              </span>
              <ChevronDown
                className={cn("mt-0.5 h-4 w-4 shrink-0 transition-transform", isOpen ? "rotate-0" : "-rotate-90")}
                aria-hidden
              />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <nav
                className="flex flex-col gap-0.5 border-l border-neutral-200 py-1 pl-2 dark:border-neutral-700"
                aria-label={group.label}
              >
                {visibleLinks.map((link) => {
                  const active = isNavLinkActive(pathname, link.href);
                  const Icon = link.icon;

                  return (
                    <Link
                      key={link.href}
                      href={link.href}
                      className={cn(
                        "shell-nav-link flex items-center gap-2 rounded-md px-2 py-1 text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800",
                        active
                          ? "bg-teal-50 font-semibold text-teal-900 dark:bg-teal-900/30 dark:text-teal-200"
                          : "text-neutral-800 dark:text-neutral-200",
                      )}
                      title={link.title}
                      aria-current={active ? "page" : undefined}
                      aria-keyshortcuts={
                        link.keyShortcut ? registryKeyToAriaKeyShortcuts(link.keyShortcut) : undefined
                      }
                    >
                      {Icon ? <Icon className="h-4 w-4 shrink-0 opacity-90" aria-hidden /> : null}
                      {link.label}
                    </Link>
                  );
                })}
              </nav>
            </CollapsibleContent>
          </Collapsible>
        );
      })}

      <div className="mt-2 space-y-2 border-t border-neutral-200 pt-3 dark:border-neutral-700">
        <Button
          type="button"
          variant={showExtended ? "secondary" : "outline"}
          size="sm"
          className="w-full justify-center text-xs"
          onClick={() => {
            setShowExtended(!showExtended);
          }}
        >
          {showExtended ? "Show fewer links" : "Show more links"}
        </Button>

        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="w-full justify-start gap-2 text-xs text-neutral-600 dark:text-neutral-400"
          onClick={() => {
            setSettingsOpen(true);
          }}
        >
          <Settings2 className="h-3.5 w-3.5 shrink-0" aria-hidden />
          Navigation settings
        </Button>
      </div>

      <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Navigation settings</DialogTitle>
            <DialogDescription>
              Control which sidebar links appear by progressive disclosure tier. The same destination list also
              respects optional minimum API authority hints (Read / Execute / Admin) when the shell can resolve your
              principal via <code className="text-xs">GET /api/auth/me</code>; the command palette (Ctrl+K) uses the
              same tier + authority composition (see <code className="text-xs">nav-shell-visibility.ts</code>).
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-0.5">
                <Label htmlFor="nav-extended">Show extended links</Label>
                <p className="text-xs text-neutral-500 dark:text-neutral-400">
                  <strong>Advanced Analysis:</strong> compare, replay, graph, advisory, pilot feedback,
                  recommendation learning.{" "}
                  <strong>Enterprise Controls:</strong> policy packs, governance dashboard, governance resolution.
                </p>
              </div>
              <input
                id="nav-extended"
                type="checkbox"
                className="mt-1 h-4 w-4 rounded border-neutral-300 text-teal-700 focus:ring-teal-600 dark:border-neutral-600"
                checked={showExtended}
                onChange={(e) => {
                  setShowExtended(e.target.checked);
                }}
              />
            </div>
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-0.5">
                <Label htmlFor="nav-advanced">Show advanced links</Label>
                <p className="text-xs text-neutral-500 dark:text-neutral-400">
                  <strong>Enterprise Controls:</strong> audit log, alert rules, alert routing, alert tuning,
                  governance workflow, schedules. Requires extended links to be on.
                </p>
              </div>
              <input
                id="nav-advanced"
                type="checkbox"
                className="mt-1 h-4 w-4 rounded border-neutral-300 text-teal-700 focus:ring-teal-600 disabled:opacity-50 dark:border-neutral-600"
                checked={showAdvanced}
                disabled={!showExtended}
                onChange={(e) => {
                  setShowAdvanced(e.target.checked);
                }}
              />
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <p
        className="mt-1 text-xs text-neutral-600 dark:text-neutral-400"
        aria-keyshortcuts="Shift+?"
      >
        Press Shift+? for help and keyboard shortcuts
      </p>
    </div>
  );
}
