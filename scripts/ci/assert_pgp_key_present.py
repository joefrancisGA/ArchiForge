"""
assert_pgp_key_present.py
-------------------------
PGP publication guard aligned with ``docs/go-to-market/TRUST_CENTER.md`` and ``docs/security/PGP_KEY_GENERATION_RECIPE.md``.

* If the Trust Center does **not** promise ``pgp-key.txt`` → exit **0** (nothing to check).
* If it **does** promise PGP but ``archlucid-ui/public/.well-known/pgp-key.txt`` is **missing** → log
  **pending owner publication** to stderr and exit **0** (warn-only; owner follows the recipe when ready).
* If the file **exists** → it must contain a valid **ASCII-armoured public key** block (BEGIN/END markers and
  substantial body). Malformed files exit **1** (merge-blocking once the workflow step drops ``continue-on-error``).

Run: ``python scripts/ci/assert_pgp_key_present.py``
"""

from __future__ import annotations

import os
import re
import sys

REPO_ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))

TRUST_CENTER_REL = os.path.normpath("docs/go-to-market/TRUST_CENTER.md")
PGP_KEY_REL = os.path.normpath("archlucid-ui/public/.well-known/pgp-key.txt")

# Match buyer-facing PGP promise without firing on unrelated "pgp" substrings in URLs we do not control.
PGP_SIGNAL = re.compile(r"(?i)pgp-key\.txt|/\.well-known/pgp|well-known/pgp")


def _read(rel_path: str) -> str:
    path = os.path.join(REPO_ROOT, rel_path)
    if not os.path.isfile(path):
        print(f"assert_pgp_key_present: missing tracked file: {rel_path}", file=sys.stderr)
        return ""

    with open(path, encoding="utf-8") as handle:
        return handle.read()


def _armored_public_key_valid(body: str) -> bool:
    stripped = body.strip()
    begin_tag = "-----BEGIN PGP PUBLIC KEY BLOCK-----"
    end_tag = "-----END PGP PUBLIC KEY BLOCK-----"

    if begin_tag not in stripped or end_tag not in stripped:
        return False

    begin_idx = stripped.index(begin_tag)
    end_idx = stripped.index(end_tag)
    if end_idx < begin_idx:
        return False

    inner = stripped[begin_idx + len(begin_tag) : end_idx].strip()
    # Real armored keys include multiple radix-64 lines; reject trivial placeholders.
    if len(inner) < 64:
        return False

    return True


def main() -> int:
    trust_center = _read(TRUST_CENTER_REL)

    if not trust_center.strip():
        return 0

    if not PGP_SIGNAL.search(trust_center):
        return 0

    key_path = os.path.join(REPO_ROOT, PGP_KEY_REL)

    if not os.path.isfile(key_path):
        print(
            "assert_pgp_key_present: pending owner publication - "
            f"{PGP_KEY_REL} is not present yet. Follow docs/security/PGP_KEY_GENERATION_RECIPE.md when ready.",
            file=sys.stderr,
        )
        return 0

    with open(key_path, encoding="utf-8") as handle:
        body = handle.read()

    if not body.strip():
        print(
            "assert_pgp_key_present: invalid PGP publication - "
            f"{PGP_KEY_REL} exists but is empty. Remove the file or add a valid ASCII-armored public key.",
            file=sys.stderr,
        )
        return 1

    if not _armored_public_key_valid(body):
        print(
            "assert_pgp_key_present: invalid PGP publication - "
            f"{PGP_KEY_REL} must contain a complete ASCII-armored public key block "
            f"(see docs/security/PGP_KEY_GENERATION_RECIPE.md, section Export public key).",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
