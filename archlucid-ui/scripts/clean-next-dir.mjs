/**
 * Windows (and occasional Linux) flaky builds when Next tries to reuse `.next`:
 * ENOTEMPTY on `rmdir .next/export`, or stale `.next/types` confusing `tsc --noEmit`.
 * Removing `.next` before `next build` forces a coherent artifact tree (similar to CI).
 */
import { existsSync, rmSync } from "node:fs";
import { join } from "node:path";

const nextDir = join(process.cwd(), ".next");
if (!existsSync(nextDir)) process.exit(0);
rmSync(nextDir, { recursive: true, force: true });
