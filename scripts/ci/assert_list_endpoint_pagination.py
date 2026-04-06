#!/usr/bin/env python3
"""
CI guard: HttpGet list-style controller actions should expose pagination (or query DTOs that carry it).

Grandfathered endpoints: list_endpoint_pagination_allowlist.txt (ControllerName.MethodName per line).
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_CONTROLLER_DIR_CANDIDATES = (
    ROOT / "ArchLucid.Api" / "Controllers",
    ROOT / "ArchiForge.Api" / "Controllers",
)
ALLOWLIST_PATH = Path(__file__).resolve().parent / "list_endpoint_pagination_allowlist.txt"


def _controllers_dir() -> Path | None:
    for candidate in _CONTROLLER_DIR_CANDIDATES:
        if candidate.is_dir():
            return candidate
    return None


def _read_controller_text(path: Path) -> str:
    raw = path.read_bytes()
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        return raw.decode("cp1252", errors="replace")

_PAGINATION = re.compile(
    r"\b(take|skip|limit|topK|cursor|pageSize|pageNumber|page\b|maxRows|maxCount|max\b|"
    r"ComparisonHistoryQuery|RetrievalQuery|Paging|Paged)\b",
    re.IGNORECASE,
)

_HTTP_GET_SPLIT = re.compile(r"(?=\[HttpGet(?:\(|\s*\]))")


def _load_allowlist() -> set[str]:
    if not ALLOWLIST_PATH.is_file():
        return set()
    out: set[str] = set()
    for line in ALLOWLIST_PATH.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        out.add(line)
    return out


def _class_name(text_before: str) -> str | None:
    m = re.search(
        r"(?:public\s+)?(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)\s*(?:\(|:)",
        text_before,
    )
    return m.group(1) if m else None


def _is_listish(method: str) -> bool:
    if method.startswith("List"):
        return True

    return method.startswith("Search") and method not in {"SearchAsync"}


def _parse_method_params(after_http_get: str) -> tuple[str, str] | None:
    """After [HttpGet...], find `public async Task<...>? name(params)` and return (name, params)."""
    t = after_http_get.find("public async Task")
    if t < 0:
        return None

    i = t + len("public async Task")
    while i < len(after_http_get) and after_http_get[i].isspace():
        i += 1

    if i < len(after_http_get) and after_http_get[i] == "<":
        depth = 1
        i += 1
        while i < len(after_http_get) and depth > 0:
            c = after_http_get[i]
            if c == "<":
                depth += 1
            elif c == ">":
                depth -= 1
            i += 1

    while i < len(after_http_get) and after_http_get[i].isspace():
        i += 1

    m = re.match(r"(\w+)\s*\(", after_http_get[i:])
    if not m:
        return None

    name = m.group(1)
    open_paren = i + m.end() - 1
    depth = 0
    j = open_paren
    while j < len(after_http_get):
        c = after_http_get[j]
        if c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            if depth == 0:
                params = after_http_get[open_paren + 1 : j]
                return name, params
        j += 1

    return None


def main() -> int:
    controllers = _controllers_dir()
    if controllers is None:
        tried = ", ".join(str(p) for p in _CONTROLLER_DIR_CANDIDATES)
        print(
            f"Missing API Controllers directory (expected ArchLucid.Api or ArchiForge.Api). Tried: {tried}",
            file=sys.stderr,
        )
        return 2

    allow = _load_allowlist()
    violations: list[str] = []

    for path in sorted(controllers.glob("*.cs")):
        src = _read_controller_text(path)
        parts = _HTTP_GET_SPLIT.split(src)
        for part in parts:
            if not part.strip().startswith("[HttpGet"):
                continue

            parsed = _parse_method_params(part)
            if parsed is None:
                continue

            method, params = parsed
            if not _is_listish(method):
                continue

            if _PAGINATION.search(params):
                continue

            before = src[: src.index(part)]
            controller = _class_name(before)
            if controller is None:
                violations.append(f"{path.name}:?::{method} (could not resolve controller class)")
                continue

            key = f"{controller}.{method}"
            if key in allow:
                continue

            violations.append(
                f"{key} — add take/skip/limit/cursor/page/topK/max* or *Query, or allowlist with justification",
            )

    if violations:
        print("List endpoint pagination audit failed:", file=sys.stderr)
        for v in violations:
            print(f"  {v}", file=sys.stderr)
        return 1

    print("list endpoint pagination audit: ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
