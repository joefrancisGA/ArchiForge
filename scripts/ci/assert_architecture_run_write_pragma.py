#!/usr/bin/env python3
"""
Fail CI if production C# references legacy IArchitectureRunRepository writes (or obsolete
implementations) without an active CS0618 suppression that documents RunsAuthorityConvergence.

See docs/adr/0012-runs-authority-convergence-write-freeze.md.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ADR = "docs/adr/0012-runs-authority-convergence-write-freeze.md"

# Repository variable names used in production for IArchitectureRunRepository today.
_WRITE_CALL_RE = re.compile(
    r"(?:(?:runRepository|_runRepository)\.(?:CreateAsync|UpdateStatusAsync)\s*\("
    r"|architectureRunRepository\.ApplyDeferredAuthoritySnapshotsAsync\s*\()",
)

# Obsolete concrete types still registered or constructed outside test assemblies.
_TYPE_SURFACE_RE = re.compile(
    r"(?:new\s+InMemoryArchitectureRunRepository\s*\(|,\s*ArchitectureRunRepository\s*>\s*\()",
)

_PRAGMA_DISABLE_CS0618 = re.compile(
    r"^\s*#\s*pragma\s+warning\s+disable\s+CS0618\b",
)
_PRAGMA_RESTORE_CS0618 = re.compile(
    r"^\s*#\s*pragma\s+warning\s+restore\s+CS0618\b",
)

_EXCLUDED_BASENAMES = frozenset(
    {
        "IArchitectureRunRepository.cs",
        "ArchitectureRunRepository.cs",
        "InMemoryArchitectureRunRepository.cs",
    },
)


def _is_test_path(path: Path) -> bool:
    parts = path.parts
    return any(".Tests" in part for part in parts)


def _should_skip_file(path: Path) -> bool:
    if path.suffix.lower() != ".cs":
        return True
    if path.name in _EXCLUDED_BASENAMES:
        return True
    if _is_test_path(path):
        return True
    parts_lower = {p.lower() for p in path.parts}
    if "bin" in parts_lower or "obj" in parts_lower or ".git" in parts_lower:
        return True
    return False


def _is_code_line(line: str) -> bool:
    stripped = line.lstrip()
    return bool(stripped) and not stripped.startswith("//")


def _line_matches_guard(line: str) -> bool:
    return bool(_WRITE_CALL_RE.search(line) or _TYPE_SURFACE_RE.search(line))


def _violations_in_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    stack: list[bool] = []
    out: list[str] = []

    for line_no, line in enumerate(lines, start=1):
        if not _is_code_line(line):
            continue

        if _PRAGMA_DISABLE_CS0618.match(line):
            stack.append("RunsAuthorityConvergence" in line)
            continue

        if _PRAGMA_RESTORE_CS0618.match(line):
            if stack:
                stack.pop()
            continue

        if _line_matches_guard(line) and not any(stack):
            out.append(f"{path}:{line_no}: {line.strip()}")

    return out


def scan_repository(repo_root: Path) -> list[str]:
    violations: list[str] = []
    for path in sorted(repo_root.rglob("*.cs")):
        if _should_skip_file(path):
            continue
        violations.extend(_violations_in_file(path))
    return violations


def _main(argv: list[str]) -> int:
    repo_root = Path(argv[1]).resolve() if len(argv) > 1 else Path.cwd().resolve()
    violations = scan_repository(repo_root)
    if not violations:
        return 0

    print(
        "::error::RunsAuthorityConvergence guard: legacy ArchitectureRuns write surface used without "
        f"an active #pragma warning disable CS0618 that includes 'RunsAuthorityConvergence' (see {ADR}).",
        file=sys.stderr,
    )
    print(
        "Wrap the call (or adjacent lines) with:\n"
        "  #pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30\n"
        "  ... await runRepository.<WriteMethod>(...);\n"
        "  #pragma warning restore CS0618\n",
        file=sys.stderr,
    )
    for v in violations:
        print(v, file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv))
