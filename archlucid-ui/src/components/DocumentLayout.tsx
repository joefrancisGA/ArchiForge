import { cn } from "@/lib/utils";

export type DocumentTocItem = {
  id: string;
  label: string;
};

export type DocumentLayoutProps = {
  children: React.ReactNode;
  /** When length ≥ 3, a sticky TOC appears from the `xl` breakpoint. */
  tocItems?: DocumentTocItem[];
  className?: string;
};

const articleBodyClass = cn(
  "min-w-0 max-w-3xl flex-1 space-y-6 text-neutral-800 dark:text-neutral-200",
  "print:max-w-none print:text-black",
  "[&_p]:text-base [&_p]:leading-relaxed",
  "[&_h2]:scroll-mt-20 [&_h2]:text-xl [&_h2]:font-bold [&_h2]:text-neutral-900 dark:[&_h2]:text-neutral-50",
  "[&_h3]:scroll-mt-20 [&_h3]:text-lg [&_h3]:font-semibold [&_h3]:text-neutral-900 dark:[&_h3]:text-neutral-100",
  "[&_h4]:scroll-mt-16 [&_h4]:text-base [&_h4]:font-semibold [&_h4]:text-neutral-900 dark:[&_h4]:text-neutral-100",
  "[&_.doc-meta]:text-sm [&_.doc-meta]:text-neutral-500 dark:[&_.doc-meta]:text-neutral-400",
  "[&_ul]:my-0 [&_ul]:list-disc [&_ul]:space-y-1 [&_ul]:pl-5 [&_ul]:text-base [&_ul]:leading-relaxed",
  "[&_pre]:overflow-x-auto [&_pre]:whitespace-pre-wrap [&_pre]:rounded-md [&_pre]:border [&_pre]:border-neutral-200 [&_pre]:bg-neutral-100 [&_pre]:p-3 [&_pre]:text-sm dark:[&_pre]:border-neutral-700 dark:[&_pre]:bg-neutral-800",
  "[&_table]:w-full [&_table]:border-collapse [&_table]:text-sm",
  "[&_thead_th]:border-b [&_thead_th]:border-neutral-200 [&_thead_th]:bg-neutral-50/90 [&_thead_th]:p-2 [&_thead_th]:text-left [&_thead_th]:font-semibold dark:[&_thead_th]:border-neutral-700 dark:[&_thead_th]:bg-neutral-900/50",
  "[&_tbody_tr:nth-child(odd)]:bg-neutral-50/70 dark:[&_tbody_tr:nth-child(odd)]:bg-neutral-900/35",
  "[&_td]:border-b [&_td]:border-neutral-100 [&_td]:p-2 [&_td]:align-top dark:[&_td]:border-neutral-800",
);

/**
 * GitBook-like reading column: comfortable measure, relaxed body type, optional sticky TOC (xl+), print-friendly width.
 * Pure layout — no data fetching.
 */
export function DocumentLayout({ children, tocItems, className }: DocumentLayoutProps) {
  const showToc = tocItems !== undefined && tocItems.length >= 3;

  return (
    <div
      className={cn(
        "mx-auto w-full print:max-w-none",
        showToc && "flex flex-col gap-8 xl:flex-row xl:items-start xl:gap-10",
        className,
      )}
      data-testid="document-layout"
    >
      <article className={articleBodyClass} data-testid="document-layout-article">
        {children}
      </article>
      {showToc ? (
        <nav
          className="hidden w-52 shrink-0 xl:sticky xl:top-24 xl:block xl:self-start print:hidden"
          aria-label="On this page"
          data-testid="document-layout-toc"
        >
          <p className="m-0 mb-2 text-[10px] font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
            On this page
          </p>
          <ul className="m-0 list-none space-y-1.5 p-0 text-xs">
            {tocItems.map((item) => (
              <li key={item.id}>
                <a
                  href={`#${item.id}`}
                  className="text-neutral-600 underline decoration-neutral-300 decoration-1 underline-offset-2 hover:text-teal-800 dark:text-neutral-400 dark:decoration-neutral-600 dark:hover:text-teal-300"
                >
                  {item.label}
                </a>
              </li>
            ))}
          </ul>
        </nav>
      ) : null}
    </div>
  );
}
