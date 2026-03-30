import type { ReactNode } from "react";

/** Bordered card with a title, used to visually group a section of page content. */
export function SectionCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section
      style={{
        border: "1px solid #ddd",
        borderRadius: 8,
        padding: 16,
        marginBottom: 16,
      }}
    >
      <h3 style={{ marginTop: 0 }}>{title}</h3>
      {children}
    </section>
  );
}
