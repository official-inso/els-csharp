#!/usr/bin/env bash
# Mirrors sdks/csharp/ from the monorepo to the standalone GitHub repository
# https://github.com/official-inso/els-csharp.
#
# Strategy: copy the working tree into a fresh worktree of the target repo,
# strip commit trailers that mention AI assistants, run the hygiene check,
# then push.
#
# Usage:
#   ./scripts/mirror-to-github.sh [--dry-run]
#
# Requires:
#   - git, rsync
#   - SSH access to git@github.com:official-inso/els-csharp.git
#   - this script run from sdks/csharp/

set -euo pipefail

DRY_RUN=0
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    *) echo "unknown flag: $arg" >&2; exit 2 ;;
  esac
done

HERE="$(cd "$(dirname "$0")/.." && pwd)"
cd "$HERE"

echo "==> Running hygiene check"
bash scripts/check-no-ai-mentions.sh

# Use the github-inso SSH alias from ~/.ssh/config so the dedicated
# official-inso key is picked. Override with ELS_MIRROR_REMOTE if needed.
REMOTE="${ELS_MIRROR_REMOTE:-git@github-inso:official-inso/els-csharp.git}"
WORKDIR="$(mktemp -d -t els-csharp-mirror-XXXX)"
trap 'rm -rf "$WORKDIR"' EXIT

echo "==> Cloning $REMOTE into $WORKDIR"
git clone --depth=20 "$REMOTE" "$WORKDIR/els-csharp" || {
  # Empty remote? Initialize a fresh repo.
  rm -rf "$WORKDIR/els-csharp"
  mkdir -p "$WORKDIR/els-csharp"
  git -C "$WORKDIR/els-csharp" init -b main
  git -C "$WORKDIR/els-csharp" remote add origin "$REMOTE"
}

echo "==> Syncing files"
rsync -a --delete \
  --exclude '.git/' \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude 'TestResults/' \
  --exclude '*.user' \
  --exclude '.DS_Store' \
  "$HERE/" "$WORKDIR/els-csharp/"

cd "$WORKDIR/els-csharp"

echo "==> Hygiene check on mirrored tree"
bash scripts/check-no-ai-mentions.sh

if [ -z "$(git status --porcelain)" ]; then
  echo "Nothing to mirror — working tree is up to date."
  exit 0
fi

VERSION="$(grep -E '<VersionPrefix>' Directory.Build.props | head -1 | sed -E 's/.*<VersionPrefix>([^<]+)<.*/\1/')"
MSG="sync: mirror snapshot ($VERSION, $(date -u +%Y-%m-%dT%H:%M:%SZ))"

git add -A
git -c user.email='maintainers@official-inso.dev' -c user.name='inso-mirror' commit -m "$MSG"

if [ "$DRY_RUN" -eq 1 ]; then
  echo "Dry run complete. Diff:"
  git --no-pager log -1 --stat
  exit 0
fi

echo "==> Pushing to $REMOTE"
git push origin HEAD:main
echo "Done."
