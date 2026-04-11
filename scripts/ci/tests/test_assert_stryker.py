"""Unit tests for assert_stryker_score_vs_baseline helpers and _main()."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

import assert_stryker_score_vs_baseline as stryker


def test_mutation_score_from_report_embedded() -> None:
    score, det, denom = stryker.mutation_score_from_report({"mutationScore": 82.5})
    assert score == pytest.approx(82.5)
    assert det == 0
    assert denom == 0


def test_mutation_score_from_report_per_file_mutants() -> None:
    data = {
        "files": {
            "A.cs": {
                "mutants": [
                    {"status": "Killed"},
                    {"status": "Survived"},
                    {"status": "Ignored"},
                ],
            },
        },
    }
    score, det, denom = stryker.mutation_score_from_report(data)
    assert denom == 2
    assert det == 1
    assert score == pytest.approx(50.0)


def test_mutation_score_from_report_empty() -> None:
    score, det, denom = stryker.mutation_score_from_report({})
    assert score == pytest.approx(0.0)
    assert det == 0
    assert denom == 0


def test_find_mutation_report_json_nested(tmp_path: Path) -> None:
    nested = tmp_path / "a" / "b"
    nested.mkdir(parents=True)
    report = nested / "mutation-report.json"
    report.write_text("{}", encoding="utf-8")
    found = stryker.find_mutation_report_json(tmp_path)
    assert found == report


def test_find_mutation_report_json_empty_dir(tmp_path: Path) -> None:
    assert stryker.find_mutation_report_json(tmp_path) is None


def test_main_ok(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(
        json.dumps({"Persistence": {"mutationScore": 60.0}}),
        encoding="utf-8",
    )
    root = tmp_path / "StrykerOutput"
    sub = root / "2025"
    sub.mkdir(parents=True)
    (sub / "mutation-report.json").write_text(
        json.dumps({"mutationScore": 80.0}),
        encoding="utf-8",
    )
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "assert_stryker_score_vs_baseline.py",
            "--baseline",
            str(baseline),
            "--label",
            "Persistence",
            "--stryker-root",
            str(root),
            "--tolerance",
            "5",
        ],
    )
    assert stryker._main() == 0


def test_main_regression(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(
        json.dumps({"Persistence": {"mutationScore": 90.0}}),
        encoding="utf-8",
    )
    root = tmp_path / "StrykerOutput"
    root.mkdir()
    (root / "mutation-report.json").write_text(
        json.dumps({"mutationScore": 50.0}),
        encoding="utf-8",
    )
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "assert_stryker_score_vs_baseline.py",
            "--baseline",
            str(baseline),
            "--label",
            "Persistence",
            "--stryker-root",
            str(root),
            "--tolerance",
            "0.15",
        ],
    )
    assert stryker._main() == 1


def test_main_exit_2_missing_baseline_file(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "assert_stryker_score_vs_baseline.py",
            "--baseline",
            str(tmp_path / "nope.json"),
            "--label",
            "Persistence",
            "--stryker-root",
            str(tmp_path),
        ],
    )
    assert stryker._main() == 2


def test_main_exit_2_no_report(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(
        json.dumps({"Persistence": {"mutationScore": 60.0}}),
        encoding="utf-8",
    )
    empty = tmp_path / "empty_out"
    empty.mkdir()
    monkeypatch.setattr(
        sys,
        "argv",
        [
            "assert_stryker_score_vs_baseline.py",
            "--baseline",
            str(baseline),
            "--label",
            "Persistence",
            "--stryker-root",
            str(empty),
        ],
    )
    assert stryker._main() == 2
