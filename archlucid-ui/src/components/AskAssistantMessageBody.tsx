"use client";

import Link from "next/link";
import { Fragment, type ReactNode } from "react";

const UUID_RE =
  /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi;

/**
 * Renders assistant markdown-free content with best-effort deep links for run-shaped UUIDs in plain text.
 * Structured citations from the API would be more precise; this avoids bare opaque ids in demo reviews.
 */
export function AskAssistantMessageBody(props: { readonly content: string }) {
  const { content } = props;
  const parts: ReactNode[] = [];
  let lastIndex = 0;

  for (const match of content.matchAll(UUID_RE)) {
    const m = match;

    if (m.index === undefined) {
      continue;
    }

    if (m.index > lastIndex) {
      parts.push(<Fragment key={`t-${lastIndex}`}>{content.slice(lastIndex, m.index)}</Fragment>);
    }

    const id = m[0];
    parts.push(
      <Link
        key={`id-${m.index}-${id}`}
        href={`/reviews/${encodeURIComponent(id)}`}
        className="font-medium text-teal-800 underline decoration-teal-300/60 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:decoration-teal-700 dark:hover:text-teal-200"
        title="Open as run detail (IDs may reference manifests in some answers — confirm in context)."
      >
        {id}
      </Link>,
    );

    lastIndex = m.index + id.length;
  }

  if (lastIndex < content.length) {
    parts.push(<Fragment key={`t-${lastIndex}`}>{content.slice(lastIndex)}</Fragment>);
  }

  if (parts.length === 0) {
    return <>{content}</>;
  }

  return (
    <p className="m-0 whitespace-pre-wrap text-sm text-neutral-800 dark:text-neutral-200">
      {parts}
    </p>
  );
}
