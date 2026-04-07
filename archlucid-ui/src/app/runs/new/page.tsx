import type { Metadata } from "next";
import Link from "next/link";

import { NewRunWizardClient } from "./NewRunWizardClient";

export const metadata: Metadata = {
  title: "New run",
};

export default function NewRunPage() {
  return (
    <main>
      <h2>New run</h2>
      <p style={{ marginTop: 8 }}>
        <Link href="/runs">Runs list</Link>
        {" · "}
        <Link href="/">Home</Link>
      </p>
      <NewRunWizardClient />
    </main>
  );
}
