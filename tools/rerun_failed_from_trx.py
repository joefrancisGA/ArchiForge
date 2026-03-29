"""Run only tests that failed in a TRX file (skips Passed / NotExecuted)."""
from __future__ import annotations

import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# Map assembly name (root namespace ending in .Tests) -> csproj path relative to ROOT.
ASSEMBLY_TO_CSPROJ: dict[str, str] = {
    "ArchiForge.Api.Tests": "ArchiForge.Api.Tests/ArchiForge.Api.Tests.csproj",
    "ArchiForge.Persistence.Tests": "ArchiForge.Persistence.Tests/ArchiForge.Persistence.Tests.csproj",
    "ArchiForge.AgentRuntime.Tests": "ArchiForge.AgentRuntime.Tests/ArchiForge.AgentRuntime.Tests.csproj",
    "ArchiForge.Coordinator.Tests": "ArchiForge.Coordinator.Tests/ArchiForge.Coordinator.Tests.csproj",
    "ArchiForge.ContextIngestion.Tests": "ArchiForge.ContextIngestion.Tests/ArchiForge.ContextIngestion.Tests.csproj",
    "ArchiForge.Contracts.Tests": "ArchiForge.Contracts.Tests/ArchiForge.Contracts.Tests.csproj",
    "ArchiForge.DecisionEngine.Tests": "ArchiForge.DecisionEngine.Tests/ArchiForge.DecisionEngine.Tests.csproj",
    "ArchiForge.Decisioning.Tests": "ArchiForge.Decisioning.Tests/ArchiForge.Decisioning.Tests.csproj",
    "ArchiForge.KnowledgeGraph.Tests": "ArchiForge.KnowledgeGraph.Tests/ArchiForge.KnowledgeGraph.Tests.csproj",
    "ArchiForge.Retrieval.Tests": "ArchiForge.Retrieval.Tests/ArchiForge.Retrieval.Tests.csproj",
    "ArchiForge.Cli.Tests": "ArchiForge.Cli.Tests/ArchiForge.Cli.Tests.csproj",
}


def assembly_from_test_name(test_name: str) -> str:
    for asm in sorted(ASSEMBLY_TO_CSPROJ.keys(), key=len, reverse=True):
        if test_name.startswith(asm + "."):
            return asm
    raise ValueError(f"Cannot infer assembly for: {test_name}")


def escape_vstest_filter_value(value: str) -> str:
    """Escape characters that VSTest treats as filter syntax (see selective unit tests docs)."""
    escaped: list[str] = []
    for ch in value:
        if ch in "\\()|&":
            escaped.append("\\")
        escaped.append(ch)
    return "".join(escaped)


def filter_chunks(names: list[str], max_len: int = 7000) -> list[str]:
    """Build OR-of-FullyQualifiedName~ clauses; split so argv stays under Windows limits (~8191)."""
    chunks: list[str] = []
    parts: list[str] = []
    current = 0
    for n in names:
        piece = f"FullyQualifiedName~{escape_vstest_filter_value(n)}"
        sep = "|" if parts else ""
        add_len = len(sep) + len(piece)
        if parts and current + add_len > max_len:
            chunks.append("|".join(parts))
            parts = [piece]
            current = len(piece)
        else:
            parts.append(piece)
            current += add_len
    if parts:
        chunks.append("|".join(parts))
    return chunks


def main() -> int:
    trx_path = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "TestResults" / "full-run.trx"
    if not trx_path.is_file():
        print(f"TRX not found: {trx_path}", file=sys.stderr)
        return 2

    tree = ET.parse(trx_path)
    root = tree.getroot()
    failed: set[str] = set()
    for el in root.findall(".//t:UnitTestResult", TRX_NS):
        if el.get("outcome") == "Failed":
            tn = el.get("testName")
            if tn:
                failed.add(tn)

    if not failed:
        print("No failed tests in TRX; nothing to rerun.")
        return 0

    by_asm: dict[str, list[str]] = defaultdict(list)
    for name in sorted(failed):
        asm = assembly_from_test_name(name)
        by_asm[asm].append(name)

    print(f"Rerunning {len(failed)} failed test(s) across {len(by_asm)} project(s).\n")

    exit_code = 0
    for asm, names in sorted(by_asm.items()):
        csproj_rel = ASSEMBLY_TO_CSPROJ.get(asm)
        if not csproj_rel:
            print(f"Skip unknown assembly (add to ASSEMBLY_TO_CSPROJ): {asm} ({len(names)} tests)", file=sys.stderr)
            exit_code = 1
            continue

        csproj = ROOT / csproj_rel
        chunks = filter_chunks(names)
        print(f"=== {asm} ({len(names)} tests) ===")
        for chunk_idx, filt in enumerate(chunks):
            if len(chunks) > 1:
                print(f"  batch {chunk_idx + 1}/{len(chunks)}")
            cmd = [
                "dotnet",
                "test",
                str(csproj),
                "-c",
                "Release",
                "--filter",
                filt,
            ]
            r = subprocess.run(cmd, cwd=ROOT)
            if r.returncode != 0:
                exit_code = r.returncode

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
