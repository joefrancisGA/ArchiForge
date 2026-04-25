"use client";

/** Inline validation message: consistent with the usability prompt (focus-visible is on the input). */
export function WizardFieldError({ id, message }: { id?: string; message?: string }) {
  if (message == null || message.length === 0) {
    return null;
  }

  return (
    <p id={id} className="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
      {message}
    </p>
  );
}
