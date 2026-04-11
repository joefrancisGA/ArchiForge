"""Tests for assert_stryker_score_vs_baseline helpers and _main."""

from __future__ import annotations

import json
import time
from pathlib import Path

import pytest

import assert_stryker_score_vs_baseline as stryker


def test_mutation_score_from_report_embedded() -> None:
    data = {"mutationScore": 82.5, "files": {}}
    score, det, denom = stryker.mutation_score_from_report(data)
    assert score == pytest.approx(82.5)
    assert det == 0
    assert denom == 0


def test_mutation_score_from_report_mutant_statuses() -> None:
    data = {
        "files": {
            "a.cs": {
                "mutants": [
                    {"status": "Killed"},
                    {"status": "Killed"},
                    {"status": "Survived"},
                    {"status": "Ignored"},
                    {"status": "NoCoverage"},
                    {"status": "Pending"},
                ],
            },
        },
    }
    score, det, denom = stryker.mutation_score_from_report(data)
    assert denom == 3
    assert det == 2
    assert score == pytest.approx(100.0 * 2 / 3)


def test_mutation_score_from_report_empty_object() -> None:
    score, det, denom = stryker.mutation_score_from_report({})
    assert score == 0.0
    assert det == 0
    assert denom == 0


def test_find_mutation_report_json_finds_newest(tmp_path: Path) -> None:
    old_dir = tmp_path / "a"
    new_dir = tmp_path / "b"
    old_dir.mkdir()
    new_dir.mkdir()
    (old_dir / "mutation-report.json").write_text("{}", encoding="utf-8")
    time.sleep(0.05)
    (new_dir / "mutation-report.json").write_text("{}", encoding="utf-8")

    found = stryker.find_mutation_report_json(tmp_path)
    assert found is not None
    assert found.parent.name == "b"


def test_find_mutation_report_json_empty_dir(tmp_path: Path) -> None:
    assert stryker.find_mutation_report_json(tmp_path) is None


def test_find_mutation_report_json_not_a_dir(tmp_path: Path) -> None:
    p = tmp_path / "file.txt"
    p.write_text("x", encoding="utf-8")
    assert stryker.find_mutation_report_json(p) is None


def test_main_ok_above_baseline(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(json.dumps({"Persistence": {"mutationScore": 70.0}}), encoding="utf-8")
    root = tmp_path / "StrykerOutput"
    (root / "run").mkdir(parents=True)
    (root / "run" / "mutation-report.json").write_text(json.dumps({"mutationScore": 75.0}), encoding="utf-8")

    monkeypatch.setattr(
        stryker.sys,
        "argv",
        [
            "prog",
            "--baseline",
            str(baseline),
            "--label",
            "Persistence",
            "--stryker-root",
            str(root),
        ],
    )
    assert stryker._main() == 0


def test_main_regression_below_floor(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(json.dumps({"Persistence": {"mutationScore": 80.0}}), encoding="utf-8")
    root = tmp_path / "StrykerOutput"
    (root / "run").mkdir(parents=True)
    (root / "run" / "mutation-report.json").write_text(json.dumps({"mutationScore": 75.0}), encoding="utf-8")

    monkeypatch.setattr(
        stryker.sys,
        "argv",
        [
            "prog",
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


def test_main_exit_2_missing_baseline_label(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(json.dumps({"Other": {"mutationScore": 1.0}}), encoding="utf-8")
    root = tmp_path / "StrykerOutput"
    (root / "run").mkdir(parents=True)
    (root / "run" / "mutation-report.json").write_text(json.dumps({"mutationScore": 99.0}), encoding="utf-8")

    monkeypatch.setattr(
        stryker.sys,
        "argv",
        ["prog", "--baseline", str(baseline), "--label", "Persistence", "--stryker-root", str(root)],
    )
    assert stryker._main() == 2


def test_main_exit_2_no_report(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    baseline = tmp_path / "baselines.json"
    baseline.write_text(json.dumps({"Persistence": {"mutationScore": 70.0}}), encoding="utf-8")
    root = tmp_path / "StrykerOutput"
    root.mkdir()

    monkeypatch.setattr(
        stryker.sys,
        "argv",
        ["prog", "--baseline", str(baseline), "--label", "Persistence", "--stryker-root", str(root)],
    )
    assert stryker._main() == 2
