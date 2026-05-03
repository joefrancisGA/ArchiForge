"use client";

import { useCommandState } from "cmdk";
import { usePathname, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/components/ui/command";
import { useNavCallerAuthorityRank, useNavCommittedArchitectureReview } from "@/components/OperatorNavAuthorityProvider";
import { useNavProgressiveDisclosure } from "@/hooks/useNavProgressiveDisclosure";
import { NAV_GROUPS } from "@/lib/nav-config";
import { effectiveNavDisclosureForPathname } from "@/lib/nav-disclosure-for-path";
import { listNavGroupsVisibleInOperatorShell } from "@/lib/nav-shell-visibility";
import { SHORTCUTS } from "@/lib/shortcut-registry";

const RUN_ID_LIKE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function CommandPaletteNavGroups({
  callerAuthorityRank,
  shellShowExtended,
  shellShowAdvanced,
  hasCommittedArchitectureReview,
  onNavigate,
}: {
  callerAuthorityRank: number;
  shellShowExtended: boolean;
  shellShowAdvanced: boolean;
  hasCommittedArchitectureReview: boolean;
  onNavigate: (href: string) => void;
}) {
  const search = useCommandState((state) => state.search);
  const showAdminPalette = search.trim().length > 0;

  const reviewRows = listNavGroupsVisibleInOperatorShell(
    NAV_GROUPS,
    shellShowExtended,
    shellShowAdvanced,
    callerAuthorityRank,
    false,
    "review-workflow",
    hasCommittedArchitectureReview,
  );

  const adminRows = listNavGroupsVisibleInOperatorShell(
    NAV_GROUPS,
    shellShowExtended,
    shellShowAdvanced,
    callerAuthorityRank,
    false,
    "platform-admin",
    hasCommittedArchitectureReview,
  );

  return (
    <>
      {reviewRows.map(({ group, visibleLinks }) => (
        <CommandGroup key={group.id} heading={group.label}>
          {visibleLinks.map((link) => (
            <CommandItem
              key={link.href}
              value={`${link.label} ${link.href}`}
              onSelect={() => {
                onNavigate(link.href);
              }}
            >
              {link.label}
            </CommandItem>
          ))}
        </CommandGroup>
      ))}
      {showAdminPalette
        ? adminRows.map(({ group, visibleLinks }) => (
            <CommandGroup
              key={`palette-${group.id}`}
              heading={group.id === "operator-admin" ? "Administration" : group.label}
            >
              {visibleLinks.map((link) => (
                <CommandItem
                  key={link.href}
                  value={`administration ${link.label} ${link.href}`}
                  onSelect={() => {
                    onNavigate(link.href);
                  }}
                >
                  {link.label}
                </CommandItem>
              ))}
            </CommandGroup>
          ))
        : null}
    </>
  );
}

function RunIdQuickOpen({ onNavigate }: { onNavigate: (href: string) => void }) {
  const search = useCommandState((state) => state.search);
  const trimmed = search.trim();

  if (!RUN_ID_LIKE.test(trimmed)) {
    return null;
  }

  return (
    <CommandGroup heading="Quick open">
      <CommandItem
        value={`open-review-${trimmed}`}
        onSelect={() => {
          onNavigate(`/reviews/${trimmed}`);
        }}
      >
        Open architecture review detail ({trimmed})
      </CommandItem>
    </CommandGroup>
  );
}

/**
 * Ctrl+K / ⌘K command palette: jump to operator pages surfaced in nav config.
 * Uses **`listNavGroupsVisibleInOperatorShell`** (tier → authority, omit empty groups) — same as sidebar and mobile drawer.
 * Optional run UUID quick-open is unchanged.
 */
export function CommandPalette() {
  const [open, setOpen] = useState(false);
  const router = useRouter();
  const pathname = usePathname();
  const { showExtended, showAdvanced } = useNavProgressiveDisclosure();
  const callerAuthorityRank = useNavCallerAuthorityRank();
  const hasCommittedArchitectureReview = useNavCommittedArchitectureReview();
  const { showExtended: shellShowExtended, showAdvanced: shellShowAdvanced } = effectiveNavDisclosureForPathname(
    pathname,
    showExtended,
    showAdvanced,
  );

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setOpen((previous) => !previous);
      }
    };

    window.addEventListener("keydown", onKeyDown);

    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, []);

  const navigate = useCallback(
    (href: string) => {
      setOpen(false);
      router.push(href);
    },
    [router],
  );

  return (
    <>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-8 gap-1.5 border-dashed border-neutral-400 bg-neutral-50/90 px-2.5 font-mono text-xs font-semibold tracking-tight text-neutral-800 shadow-sm hover:bg-neutral-100 dark:border-neutral-600 dark:bg-neutral-900/80 dark:text-neutral-100 dark:hover:bg-neutral-800"
        aria-label="Open command palette"
        onClick={() => {
          setOpen(true);
        }}
      >
        <span className="rounded border border-neutral-300 bg-white px-1 py-0.5 text-[10px] font-semibold text-neutral-600 dark:border-neutral-600 dark:bg-neutral-950 dark:text-neutral-400">
          ⌘K
        </span>
        <span>Jump…</span>
      </Button>
      <CommandDialog open={open} onOpenChange={setOpen}>
        <CommandInput placeholder="Search pages or paste a review ID…" />
        <CommandList>
          <RunIdQuickOpen onNavigate={navigate} />
          <CommandEmpty>No matching pages. Try another search or paste a review ID.</CommandEmpty>
          <CommandPaletteNavGroups
            callerAuthorityRank={callerAuthorityRank}
            shellShowExtended={shellShowExtended}
            shellShowAdvanced={shellShowAdvanced}
            hasCommittedArchitectureReview={hasCommittedArchitectureReview}
            onNavigate={navigate}
          />
          <CommandSeparator />
          <CommandGroup heading="Keyboard shortcuts (navigation)">
            {SHORTCUTS.filter((entry) => entry.route !== undefined && entry.route !== "").map((entry) => (
              <CommandItem
                key={entry.key}
                value={`${entry.label} ${entry.key}`}
                onSelect={() => {
                  if (entry.route) {
                    navigate(entry.route);
                  }
                }}
              >
                {entry.label}{" "}
                <span className="ml-1 text-xs text-neutral-500">({entry.key})</span>
              </CommandItem>
            ))}
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </>
  );
}
