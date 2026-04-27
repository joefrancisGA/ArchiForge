#!/usr/bin/env node
/**
 * sessionStart is fire-and-forget; emits a user_message when a background agent session starts.
 */
import fs from "fs";

const raw = fs.readFileSync(0, "utf8").trim();
let data = {};

try {
  data = raw ? JSON.parse(raw) : {};
} catch {
  process.stdout.write("{}\n");
  process.exit(0);
}

if (data.is_background_agent === true) {
  process.stdout.write(
    JSON.stringify({
      user_message:
        "Background agent session started. Project policy: prefer the main Agent chat unless parallel/background work was explicitly approved.",
    }) + "\n",
  );
  process.exit(0);
}

process.stdout.write("{}\n");
process.exit(0);
