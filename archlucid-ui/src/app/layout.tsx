import type { Metadata } from "next";
import type { ReactNode } from "react";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ArchLucid operator shell",
    template: "%s · ArchLucid",
  },
  description:
    "Operator UI for architecture runs, manifests, artifacts, graphs, compare, replay, and governance.",
  manifest: "/manifest.webmanifest",
  themeColor: "#1E3A5F",
  icons: {
    icon: [{ url: "/logo/favicon.svg", type: "image/svg+xml" }],
    apple: [{ url: "/logo/icon-192.png", sizes: "192x192", type: "image/png" }],
  },
};

/** Root layout: global styles only. Route groups supply operator shell (`(operator)/layout`) or marketing chrome (`(marketing)/layout`). */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html:
              "(function(){try{var k='archlucid_color_mode';var m=localStorage.getItem(k)||'system';var d=m==='dark'||(m!=='light'&&window.matchMedia('(prefers-color-scheme: dark)').matches);document.documentElement.classList.toggle('dark',d);}catch(e){}})();",
          }}
        />
      </head>
      <body className="min-h-screen font-sans">
        {children}
      </body>
    </html>
  );
}
