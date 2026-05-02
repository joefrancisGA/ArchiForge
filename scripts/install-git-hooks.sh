#!/usr/bin/env bash
# Installs the shared pre-commit hook from scripts/hooks/ into .git/hooks/.
# Run once after cloning: bash scripts/install-git-hooks.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HOOKS_SRC="$SCRIPT_DIR/hooks"
GIT_HOOKS_DIR="$(git -C "$SCRIPT_DIR" rev-parse --git-dir)/hooks"

install_hook() {
  local name="$1"
  local src="$HOOKS_SRC/$name"
  local dest="$GIT_HOOKS_DIR/$name"

  if [[ ! -f "$src" ]]; then
    echo "  skip: $name (no source file)"
    return
  fi

  cp "$src" "$dest"
  chmod +x "$dest"
  echo "  installed: $name"
}

echo "Installing git hooks from $HOOKS_SRC → $GIT_HOOKS_DIR"
install_hook "pre-commit"
echo "Done. Run 'git commit --no-verify' to bypass in emergencies."
