import { Suspense } from "react";

import { CallbackClient } from "@/app/auth/callback/CallbackClient";

export default function AuthCallbackPage() {
  return (
    <Suspense
      fallback={
        <div style={{ maxWidth: 560 }}>
          <h2 style={{ marginTop: 0 }}>Sign-in callback</h2>
          <p>Loading…</p>
        </div>
      }
    >
      <CallbackClient />
    </Suspense>
  );
}
