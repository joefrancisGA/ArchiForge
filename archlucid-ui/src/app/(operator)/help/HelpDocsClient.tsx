"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

export type DocIndexEntry = {
  title: string;
  summary: string;
  category: string;
  url: string;
};

const CATEGORY_ORDER = [
  "Getting Started",
  "Architecture",
  "Operations",
  "Security",
  "API",
  "Go-to-Market",
] as const;

/** Default reference links so Help always has content before /doc-index.json loads. */
const HELP_DOCS_STATIC_ENTRIES: readonly DocIndexEntry[] = [
  { title: "Operator home (first run)", summary: "Pilot checklist and recent runs.", category: "Getting Started", url: "/" },
  {
    title: "New architecture request",
    summary: "Create a run with the guided wizard.",
    category: "Getting Started",
    url: "/runs/new",
  },
  { title: "Requests and runs list", summary: "Browse runs for the workspace.", category: "Operations", url: "/runs" },
  {
    title: "Governance findings",
    summary: "Review cross-run findings and policy signals.",
    category: "Operations",
    url: "/governance/findings",
  },
  { title: "Policy packs", summary: "Declarative policy bundles for review.", category: "Security", url: "/policy-packs" },
  {
    title: "Indexed search",
    summary: "Search manifests, findings, and related records where enabled.",
    category: "Operations",
    url: "/search",
  },
];

function mergeDocIndex(staticRows: readonly DocIndexEntry[], fetched: DocIndexEntry[] | null): DocIndexEntry[] {
  if (fetched === null || fetched.length === 0) {
    return [...staticRows];
  }

  const seen = new Set<string>();

  for (const e of staticRows) {
    seen.add(`${e.category}|${e.title}|${e.url}`);
  }

  const merged: DocIndexEntry[] = [...staticRows];

  for (const e of fetched) {
    const k = `${e.category}|${e.title}|${e.url}`;

    if (seen.has(k)) {
      continue;
    }

    seen.add(k);
    merged.push(e);
  }

  return merged;
}

export function HelpDocsClient() {
  const [entries, setEntries] = useState<DocIndexEntry[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [query, setQuery] = useState("");

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch("/doc-index.json", { cache: "no-store" });

        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }

        const data = (await res.json()) as DocIndexEntry[];

        if (!cancelled) {
          setEntries(Array.isArray(data) ? mergeDocIndex(HELP_DOCS_STATIC_ENTRIES, data) : [...HELP_DOCS_STATIC_ENTRIES]);
        }
      } catch (e) {
        if (!cancelled) {
          setLoadError(e instanceof Error ? e.message : "Failed to load documentation index.");
          setEntries([...HELP_DOCS_STATIC_ENTRIES]);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const mergedEntries = entries ?? [...HELP_DOCS_STATIC_ENTRIES];

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();

    if (q.length === 0) {
      return mergedEntries;
    }

    return mergedEntries.filter((e) => {
      const hay = `${e.title} ${e.summary}`.toLowerCase();

      return hay.includes(q);
    });
  }, [mergedEntries, query]);

  const grouped = useMemo(() => {
    const map = new Map<string, DocIndexEntry[]>();

    for (const e of filtered) {
      const list = map.get(e.category) ?? [];
      list.push(e);
      map.set(e.category, list);
    }

    return map;
  }, [filtered]);

  function linkProps(url: string): { rel?: string; target?: "_blank" } {
    const external = /^https?:\/\//i.test(url);

    if (!external) {
      return {};
    }

    return { rel: "noreferrer", target: "_blank" };
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        <strong>Shortcuts</strong> — Use the command palette or search in the shell header where available; shortcut hints appear
        on nav items when configured.
      </p>
      {loadError !== null ? (
        <p className="text-sm text-amber-800 dark:text-amber-200" role="status">
          Full documentation index could not be refreshed ({loadError}). Quick links below are always available.
        </p>
      ) : null}
      {entries === null ? (
        <p className="text-sm text-neutral-500 dark:text-neutral-400" role="status">
          Refreshing documentation index…
        </p>
      ) : null}
      <label className="block text-sm font-medium text-neutral-800 dark:text-neutral-200" htmlFor="help-doc-search">
        Search documentation
      </label>
      <input
        id="help-doc-search"
        type="search"
        value={query}
        onChange={(ev) => setQuery(ev.target.value)}
        placeholder="Filter by title or summary"
        className="w-full max-w-xl rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-teal-600 dark:border-neutral-700 dark:bg-neutral-950"
        autoComplete="off"
      />

      {filtered.length === 0 ? (
        <p className="text-sm text-neutral-600 dark:text-neutral-400">No results</p>
      ) : null}

      {CATEGORY_ORDER.map((cat) => {
        const rows = grouped.get(cat);

        if (!rows || rows.length === 0) {
          return null;
        }

        return (
          <section key={cat} aria-labelledby={`help-cat-${cat}`} className="space-y-2">
            <h2
              id={`help-cat-${cat}`}
              className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
            >
              {cat}
            </h2>
            <ul className="space-y-3">
              {rows.map((row) => (
                <li key={`${cat}-${row.title}-${row.url}`}>
                  <Link
                    href={row.url}
                    className="font-medium text-teal-700 underline-offset-2 hover:underline dark:text-teal-400"
                    {...linkProps(row.url)}
                  >
                    {row.title}
                  </Link>
                  <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">{row.summary}</p>
                </li>
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
