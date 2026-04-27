#!/usr/bin/env node
/**
 * Blocks parallel Task/subagent workers (is_parallel_worker === true).
 * Sequential subagents and the main agent are unaffected.
 * Requires Node.js on PATH (repo already uses Node for archlucid-ui).
 */
import fs from "fs";

const raw = fs.readFileSync(0, "utf8").trim();
let data = {};

try {
  data = raw ? JSON.parse(raw) : {};
} catch {
  process.stdout.write(JSON.stringify({ permission: "allow" }) + "\n");
  process.exit(0);
}

if (data.is_parallel_worker === true) {
  process.stdout.write(
    JSON.stringify({
      permission: "deny",
      user_message:
        "Parallel agent blocked by project hook. To allow parallel work, adjust or remove `.cursor/hooks.json` (subagentStart) after explicit approval.",
    }) + "\n",
  );
  process.exit(0);
}

process.stdout.write(JSON.stringify({ permission: "allow" }) + "\n");
process.exit(0);
