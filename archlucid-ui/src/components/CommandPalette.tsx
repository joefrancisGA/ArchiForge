"use client";

import { useCommandState } from "cmdk";
import { useRouter } from "next/navigation";
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
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { NAV_GROUPS } from "@/lib/nav-config";
import { filterNavLinksByAuthority } from "@/lib/nav-authority";
import { SHORTCUTS } from "@/lib/shortcut-registry";

const RUN_ID_LIKE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function RunIdQuickOpen({ onNavigate }: { onNavigate: (href: string) => void }) {
  const search = useCommandState((state) => state.search);
  const trimmed = search.trim();

  if (!RUN_ID_LIKE.test(trimmed)) {
    return null;
  }

  return (
    <CommandGroup heading="Quick open">
      <CommandItem
        value={`open-run-${trimmed}`}
        onSelect={() => {
          onNavigate(`/runs/${trimmed}`);
        }}
      >
        Open run detail for {trimmed}
      </CommandItem>
    </CommandGroup>
  );
}

/**
 * Ctrl+K / ⌘K command palette: jump to any operator page; optional run UUID opens run detail.
 */
export function CommandPalette() {
  const [open, setOpen] = useState(false);
  const router = useRouter();
  const callerAuthorityRank = useNavCallerAuthorityRank();

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
        className="gap-1 text-xs font-semibold"
        aria-label="Open command palette"
        onClick={() => {
          setOpen(true);
        }}
      >
        Jump…
      </Button>
      <CommandDialog open={open} onOpenChange={setOpen}>
        <CommandInput placeholder="Search pages or paste a run id…" />
        <CommandList>
          <RunIdQuickOpen onNavigate={navigate} />
          <CommandEmpty>No matching pages. Try another search or paste a run UUID.</CommandEmpty>
          {NAV_GROUPS.map((group) => {
            const authorityFiltered = filterNavLinksByAuthority(group.links, callerAuthorityRank);

            return (
              <CommandGroup key={group.id} heading={group.label}>
                {authorityFiltered.map((link) => (
                  <CommandItem
                    key={link.href}
                    value={`${link.label} ${link.href}`}
                    onSelect={() => {
                      navigate(link.href);
                    }}
                  >
                    {link.label}
                  </CommandItem>
                ))}
              </CommandGroup>
            );
          })}
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
