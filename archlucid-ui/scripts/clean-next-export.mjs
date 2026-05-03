/**
 * Next.js 15 on Windows can throw ENOTEMPTY when it tries to `rmdir` `.next/export`
 * if the folder is not empty (stale files, search indexer, AV). Node's recursive
 * `rmSync` clears it before `next build` runs, matching a POSIX `rm -rf` on that path.
 */
import { existsSync, rmSync } from "node:fs";
import { join } from "node:path";

const exportDir = join(process.cwd(), ".next", "export");
if (existsSync(exportDir)) rmSync(exportDir, { recursive: true, force: true });
