"use client";

import { Loader2 } from "lucide-react";

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { cn } from "@/lib/utils";

export type ConfirmationDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "destructive" | "default";
  onConfirm: () => void;
  busy?: boolean;
};

const defaultConfirmLabel = "Confirm";
const defaultCancelLabel = "Cancel";

export function ConfirmationDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = defaultConfirmLabel,
  cancelLabel = defaultCancelLabel,
  variant = "destructive",
  onConfirm,
  busy = false,
}: ConfirmationDialogProps) {
  const resolvedConfirmLabel = confirmLabel;
  const isDestructive = variant === "destructive";

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={busy}>{cancelLabel}</AlertDialogCancel>
          <AlertDialogAction
            disabled={busy}
            className={cn(
              !isDestructive &&
                "border-transparent bg-neutral-900 text-neutral-50 shadow-sm hover:bg-neutral-800 hover:text-neutral-50 focus-visible:ring-neutral-400 dark:bg-neutral-200 dark:text-neutral-900 dark:hover:bg-neutral-300 dark:focus-visible:ring-neutral-500",
            )}
            onClick={onConfirm}
          >
            {busy ? (
              <span className="inline-flex items-center gap-2">
                <Loader2
                  className="h-4 w-4 shrink-0 animate-spin"
                  aria-hidden
                />
                Processing…
              </span>
            ) : (
              resolvedConfirmLabel
            )}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
