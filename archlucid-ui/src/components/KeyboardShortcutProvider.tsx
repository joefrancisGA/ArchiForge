"use client";

import type { ReactNode } from "react";
import { useState } from "react";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { KeyboardShortcutsHelpContent } from "@/components/KeyboardShortcutsHelpContent";
import { useShortcutNavigation } from "@/hooks/useShortcutNavigation";

export type KeyboardShortcutProviderProps = {
  children: ReactNode;
  /** When set (e.g. from App shell), Shift+? invokes this instead of the built-in shortcuts-only dialog. */
  onHelpRequested?: () => void;
};

/**
 * Global keyboard shortcuts; Shift+? opens either the parent help panel or a shortcuts-only dialog.
 */
export function KeyboardShortcutProvider({ children, onHelpRequested }: KeyboardShortcutProviderProps) {
  const [helpOpen, setHelpOpen] = useState(false);
  const handler = onHelpRequested ?? (() => setHelpOpen(true));

  useShortcutNavigation({
    onHelpRequested: handler,
  });

  return (
    <>
      {children}

      {onHelpRequested === undefined ? (
        <Dialog open={helpOpen} onOpenChange={setHelpOpen}>
          <DialogContent className="max-h-[85vh] max-w-lg overflow-y-auto sm:max-w-xl">
            <DialogHeader>
              <DialogTitle>Keyboard shortcuts</DialogTitle>
              <DialogDescription>
                Press Alt + key to navigate. Works anywhere except inside text inputs.
              </DialogDescription>
            </DialogHeader>
            <KeyboardShortcutsHelpContent />
          </DialogContent>
        </Dialog>
      ) : null}
    </>
  );
}
