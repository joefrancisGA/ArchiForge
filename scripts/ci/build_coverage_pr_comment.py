#!/usr/bin/env python3
"""Build markdown for sticky PR comment from full-regression coverage metrics files."""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path


def parse_metrics(path: Path) -> tuple[float | None, float | None]:
    if not path.is_file():
        return None, None

    text = path.read_text(encoding="utf-8", errors="replace")
    line_m = re.search(r"Line coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    branch_m = re.search(r"Branch coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    line_pct = float(line_m.group(1)) if line_m else None
    branch_pct = float(branch_m.group(1)) if branch_m else None

    return line_pct, branch_pct


def format_delta(current: float | None, previous: float | None) -> str:
    if current is None:
        return ""

    if previous is None:
        return ""

    delta = current - previous
    if abs(delta) < 0.01:
        return " (±0.0 pp vs base)"

    sign = "+" if delta > 0 else ""
    return f" ({sign}{delta:.1f} pp vs base)"


def append_github_output(name: str, value: str) -> None:
    out_path = os.environ.get("GITHUB_OUTPUT")
    if not out_path:
        print(value, file=sys.stderr)
        return

    with open(out_path, "a", encoding="utf-8") as handle:
        handle.write(f"{name}<<EOF\n{value}\nEOF\n")


def main() -> int:
    run_url = os.environ.get("RUN_URL", "").strip()
    base_ref = os.environ.get("BASE_REF", "").strip()
    head_file = Path(os.environ.get("HEAD_FILE", "head-metrics/coverage-metrics.txt"))
    base_file = Path(os.environ.get("BASE_FILE", "base-metrics/coverage-metrics.txt"))

    head_line, head_branch = parse_metrics(head_file)
    base_line, base_branch = parse_metrics(base_file)

    if head_line is None and head_branch is None:
        append_github_output("body", "_Could not read coverage metrics for this PR._")
        return 0

    lines: list[str] = [
        "### Code coverage (full .NET regression)",
        "",
        "| Metric | This PR |",
        "| --- | --- |",
    ]

    if head_line is not None:
        lines.append(
            f"| Line | **{head_line:.1f}%**{format_delta(head_line, base_line)} |",
        )

    if head_branch is not None:
        lines.append(
            f"| Branch | **{head_branch:.1f}%**{format_delta(head_branch, base_branch)} |",
        )

    lines.append("")

    if base_line is None and base_branch is None and base_ref:
        lines.append(
            f"_No cached metrics for base `{base_ref}` yet (merge a green run to `main`/`master` to enable deltas)._",
        )
        lines.append("")

    if run_url:
        lines.append(f"[View workflow run]({run_url})")

    append_github_output("body", "\n".join(lines))
    return 0


if __name__ == "__main__":
    sys.exit(main())
