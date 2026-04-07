/**
 * Playwright webServer entry: serves typed fixture JSON on a loopback port, then starts Next.js
 * with ARCHIFORGE_API_BASE_URL pointing at that stub (RSC run/manifest fetches).
 *
 * Uses `output: "standalone"` from next.config — `next start` does not serve that layout correctly
 * (Next logs a warning and pages break). Mirror Dockerfile: copy static + public into standalone, then
 * `node server.js` from `.next/standalone`.
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

import { startMockArchiforgeApiServer } from "./mock-archiforge-api-server";

const MOCK_PORT = Number(process.env.E2E_MOCK_API_PORT ?? "18765");
const MOCK_BASE = `http://127.0.0.1:${MOCK_PORT}`;

function syncStandaloneRuntimeAssets(projectRoot: string): string {
  const standaloneRoot = path.join(projectRoot, ".next", "standalone");
  const serverJs = path.join(standaloneRoot, "server.js");

  if (!fs.existsSync(serverJs)) {
    throw new Error(
      `Missing ${serverJs}. Run "npm run build" first (next.config uses output: "standalone").`,
    );
  }

  const staticSrc = path.join(projectRoot, ".next", "static");
  const staticDest = path.join(standaloneRoot, ".next", "static");

  if (!fs.existsSync(staticSrc)) {
    throw new Error(`Missing ${staticSrc} after build; client assets are required for e2e.`);
  }

  fs.mkdirSync(path.dirname(staticDest), { recursive: true });
  fs.cpSync(staticSrc, staticDest, { recursive: true });

  const publicSrc = path.join(projectRoot, "public");
  const publicDest = path.join(standaloneRoot, "public");

  if (fs.existsSync(publicSrc)) {
    fs.cpSync(publicSrc, publicDest, { recursive: true });
  }
  else {
    fs.mkdirSync(publicDest, { recursive: true });
  }

  return standaloneRoot;
}

async function main(): Promise<void> {
  const mock = await startMockArchiforgeApiServer(MOCK_PORT);

  const projectRoot = process.cwd();
  const standaloneRoot = syncStandaloneRuntimeAssets(projectRoot);
  const serverJs = path.join(standaloneRoot, "server.js");

  const child = spawn(process.execPath, [serverJs], {
    stdio: "inherit",
    env: {
      ...process.env,
      ARCHIFORGE_API_BASE_URL: MOCK_BASE,
      NODE_ENV: "production",
      PORT: process.env.PORT ?? "3000",
      // Bind all interfaces so Playwright can reach 127.0.0.1:3000 (do not inherit shell HOSTNAME).
      HOSTNAME: "0.0.0.0",
    },
    cwd: standaloneRoot,
  });

  let mockStopped = false;

  const stopMock = async (): Promise<void> => {
    if (mockStopped) {
      return;
    }

    mockStopped = true;
    await mock.stop();
  };

  const onSignal = (): void => {
    child.kill("SIGTERM");
    void stopMock().finally(() => process.exit(0));
  };

  process.on("SIGTERM", onSignal);
  process.on("SIGINT", onSignal);

  child.on("exit", (code, signal) => {
    void stopMock().finally(() => {
      process.exit(code ?? (signal ? 1 : 0));
    });
  });
}

void main().catch((err: unknown) => {
  console.error(err);
  process.exit(1);
});
