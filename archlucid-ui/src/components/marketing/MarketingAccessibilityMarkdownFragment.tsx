import Link from "next/link";
import type { ReactNode } from "react";

function renderInline(text: string, keyPrefix: string): ReactNode[] {
  const nodes: ReactNode[] = [];
  let remaining = text;
  let i = 0;

  while (remaining.length > 0) {
    const linkOpen = remaining.indexOf("[");
    const boldOpen = remaining.indexOf("**");

    const pickLink = linkOpen >= 0;
    const pickBold = boldOpen >= 0;
    let nextKind: "link" | "bold" | "none" = "none";
    let nextAt = -1;

    if (pickLink && pickBold) {
      nextKind = linkOpen <= boldOpen ? "link" : "bold";
      nextAt = nextKind === "link" ? linkOpen : boldOpen;
    }
    else if (pickLink) {
      nextKind = "link";
      nextAt = linkOpen;
    }
    else if (pickBold) {
      nextKind = "bold";
      nextAt = boldOpen;
    }

    if (nextKind === "none" || nextAt < 0) {
      nodes.push(<span key={`${keyPrefix}-t-${i}`}>{remaining}</span>);
      break;
    }

    if (nextAt > 0) {
      nodes.push(<span key={`${keyPrefix}-p-${i}`}>{remaining.slice(0, nextAt)}</span>);
    }

    if (nextKind === "bold") {
      const close = remaining.indexOf("**", nextAt + 2);
      if (close < 0) {
        nodes.push(<span key={`${keyPrefix}-b-${i}`}>{remaining.slice(nextAt)}</span>);
        break;
      }

      const inner = remaining.slice(nextAt + 2, close);
      nodes.push(
        <strong key={`${keyPrefix}-s-${i}`} className="font-semibold">
          {renderInline(inner, `${keyPrefix}-bi-${i}`)}
        </strong>,
      );
      remaining = remaining.slice(close + 2);
      i++;
      continue;
    }

    const closeBracket = remaining.indexOf("]", nextAt);
    const openParen = closeBracket >= 0 ? remaining.indexOf("(", closeBracket) : -1;
    const closeParen = openParen >= 0 ? remaining.indexOf(")", openParen) : -1;

    if (closeBracket < 0 || openParen !== closeBracket + 1 || closeParen < 0) {
      nodes.push(<span key={`${keyPrefix}-lbroken-${i}`}>{remaining.slice(nextAt)}</span>);
      break;
    }

    const label = remaining.slice(nextAt + 1, closeBracket);
    const href = remaining.slice(openParen + 1, closeParen);
    const safe = href.startsWith("https://") || href.startsWith("http://") || href.startsWith("mailto:");

    if (!safe) {
      nodes.push(<span key={`${keyPrefix}-unsafe-${i}`}>{remaining.slice(nextAt, closeParen + 1)}</span>);
      remaining = remaining.slice(closeParen + 1);
      i++;
      continue;
    }

    const isExternal = href.startsWith("http://") || href.startsWith("https://");
    nodes.push(
      <Link
        key={`${keyPrefix}-a-${i}`}
        href={href}
        className="text-blue-700 underline underline-offset-2 hover:text-blue-900 dark:text-blue-300 dark:hover:text-blue-200"
        {...(isExternal ? { rel: "noopener noreferrer", target: "_blank" } : {})}
      >
        {renderInline(label, `${keyPrefix}-al-${i}`)}
      </Link>,
    );
    remaining = remaining.slice(closeParen + 1);
    i++;
  }

  return nodes;
}

function isTableRow(line: string): boolean {
  const t = line.trim();
  return t.startsWith("|") && t.endsWith("|");
}

function isTableDivider(line: string): boolean {
  const t = line.trim();
  return /^\|?[\s|:-]+\|?$/.test(t) && t.includes("-");
}

type MarketingAccessibilityMarkdownFragmentProps = {
  markdownBody: string;
  tableCaption: string;
};

/**
 * Minimal Markdown → HTML for trusted repo policy fragments (no `dangerouslySetInnerHTML`).
 * Supports paragraphs, `###` headings, `-` lists, ordered lists, GitHub-style tables, **bold**, and `[text](url)` links.
 */
export function MarketingAccessibilityMarkdownFragment(props: MarketingAccessibilityMarkdownFragmentProps): React.ReactNode {
  if (props.markdownBody.length === 0) {
    return null;
  }

  const lines = props.markdownBody.replace(/\r\n/g, "\n").split("\n");
  const blocks: ReactNode[] = [];
  let key = 0;

  let i = 0;
  while (i < lines.length) {
    const line = lines[i] ?? "";

    if (line.trim().length === 0) {
      i++;
      continue;
    }

    if (line.startsWith("## ") && !line.startsWith("###")) {
      const title = line.slice(3).trim();
      blocks.push(
        <h2 key={`h2-${key}`} className="mt-8 text-xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-50">
          {renderInline(title, `h2-${key}`)}
        </h2>,
      );
      key++;
      i++;
      continue;
    }

    if (line.startsWith("# ") && !line.startsWith("##")) {
      const title = line.slice(2).trim();
      blocks.push(
        <h1 key={`h1-${key}`} className="mt-2 text-2xl font-bold tracking-tight text-neutral-900 dark:text-neutral-50">
          {renderInline(title, `h1-${key}`)}
        </h1>,
      );
      key++;
      i++;
      continue;
    }

    if (line.startsWith("### ")) {
      const title = line.slice(4).trim();
      blocks.push(
        <h3 key={`h3-${key}`} className="mt-4 text-base font-semibold text-neutral-900 dark:text-neutral-50">
          {renderInline(title, `h3-${key}`)}
        </h3>,
      );
      key++;
      i++;
      continue;
    }

    if (line.trimStart().startsWith(">")) {
      const quoteLines: string[] = [];
      while (i < lines.length) {
        const l = lines[i] ?? "";
        const t = l.trimStart();
        if (!t.startsWith(">")) {
          break;
        }

        quoteLines.push(t.slice(1).trimStart());
        i++;
      }

      const body = quoteLines.join("\n").trim();
      if (body.length > 0) {
        blocks.push(
          <blockquote
            key={`bq-${key}`}
            className="my-4 border-l-4 border-neutral-300 pl-4 text-sm italic text-neutral-700 dark:border-neutral-600 dark:text-neutral-300"
          >
            <p className="m-0 leading-relaxed">{renderInline(body, `bq-${key}`)}</p>
          </blockquote>,
        );
        key++;
      }

      continue;
    }

    if (isTableRow(line)) {
      const tableLines: string[] = [];
      while (i < lines.length && isTableRow(lines[i] ?? "")) {
        tableLines.push(lines[i] ?? "");
        i++;
      }

      const bodyRows = tableLines.filter((r) => !isTableDivider(r));
      if (bodyRows.length === 0) {
        continue;
      }

      const headerCells = bodyRows[0]!
        .split("|")
        .map((c) => c.trim())
        .filter((c) => c.length > 0);
      const dataStart = isTableDivider(bodyRows[1] ?? "") ? 2 : 1;

      blocks.push(
        <div key={`tbl-${key}`} className="my-4 overflow-x-auto">
          <table className="w-full border-collapse border border-neutral-200 text-sm dark:border-neutral-800">
            <caption className="sr-only">{props.tableCaption}</caption>
            <thead className="bg-neutral-100 dark:bg-neutral-900">
              <tr>
                {headerCells.map((c, idx) => (
                  <th
                    key={`th-${key}-${idx}`}
                    scope="col"
                    className="border border-neutral-200 px-3 py-2 text-left font-semibold dark:border-neutral-800"
                  >
                    {renderInline(c, `th-${key}-${idx}`)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {bodyRows.slice(dataStart).map((row, rIdx) => {
                const cells = row
                  .split("|")
                  .map((c) => c.trim())
                  .filter((c) => c.length > 0);

                return (
                  <tr key={`tr-${key}-${rIdx}`} className="odd:bg-white even:bg-neutral-50 dark:odd:bg-neutral-950 dark:even:bg-neutral-900/60">
                    {cells.map((c, cIdx) => (
                      <td key={`td-${key}-${rIdx}-${cIdx}`} className="border border-neutral-200 px-3 py-2 dark:border-neutral-800">
                        {renderInline(c, `td-${key}-${rIdx}-${cIdx}`)}
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>,
      );
      key++;
      continue;
    }

    if (line.trimStart().startsWith("- ")) {
      const items: string[] = [];
      while (i < lines.length) {
        const l = lines[i] ?? "";
        if (l.trim().length === 0) {
          break;
        }

        if (!l.trimStart().startsWith("- ")) {
          break;
        }

        items.push(l.trim().slice(2));
        i++;
      }

      blocks.push(
        <ul key={`ul-${key}`} className="my-3 list-disc space-y-2 pl-6 text-neutral-800 dark:text-neutral-200">
          {items.map((it, idx) => (
            <li key={`li-${key}-${idx}`}>{renderInline(it, `li-${key}-${idx}`)}</li>
          ))}
        </ul>,
      );
      key++;
      continue;
    }

    if (/^\d+\.\s+/.test(line.trimStart())) {
      const items: string[] = [];
      while (i < lines.length) {
        const l = lines[i] ?? "";
        if (l.trim().length === 0) {
          break;
        }

        const t = l.trimStart();
        if (!/^\d+\.\s+/.test(t)) {
          break;
        }

        items.push(t.replace(/^\d+\.\s+/, ""));
        i++;
      }

      blocks.push(
        <ol key={`ol-${key}`} className="my-3 list-decimal space-y-2 pl-6 text-neutral-800 dark:text-neutral-200">
          {items.map((it, idx) => (
            <li key={`oli-${key}-${idx}`}>{renderInline(it, `oli-${key}-${idx}`)}</li>
          ))}
        </ol>,
      );
      key++;
      continue;
    }

    const paraLines: string[] = [];
    while (i < lines.length) {
      const l = lines[i] ?? "";
      if (l.trim().length === 0) {
        break;
      }

      if (
        (l.startsWith("## ") && !l.startsWith("###")) ||
        (l.startsWith("# ") && !l.startsWith("##")) ||
        l.startsWith("### ") ||
        l.trimStart().startsWith(">") ||
        isTableRow(l) ||
        l.trimStart().startsWith("- ") ||
        /^\d+\.\s+/.test(l.trimStart())
      ) {
        break;
      }

      paraLines.push(l);
      i++;
    }

    const paragraph = paraLines.join(" ").trim();
    if (paragraph.length > 0) {
      blocks.push(
        <p key={`p-${key}`} className="my-3 leading-relaxed text-neutral-800 dark:text-neutral-200">
          {renderInline(paragraph, `p-${key}`)}
        </p>,
      );
      key++;
    }
  }

  return <div className="space-y-1">{blocks}</div>;
}
