#!/usr/bin/env python3
"""Print one version's section of CHANGELOG.md, for `gh release create --notes-file`.

Fails when the changelog has no section for the version, so a release never goes out without
notes.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

CHANGELOG = Path(__file__).resolve().parents[1] / "CHANGELOG.md"


def section(version: str) -> str:
    lines = CHANGELOG.read_text(encoding="utf-8").splitlines()
    heading = re.compile(rf"^## \[{re.escape(version)}\]")
    start = next((index + 1 for index, line in enumerate(lines) if heading.match(line)), None)
    if start is None:
        raise SystemExit(f"CHANGELOG.md has no section for {version}.")
    end = next((index for index in range(start, len(lines)) if lines[index].startswith("## [")), len(lines))
    notes = "\n".join(lines[start:end]).strip()
    # Drop the link references at the bottom of the file if the section is the last one.
    notes = re.sub(r"\n\[[^\]]+\]: \S+", "", notes).strip()
    return notes + "\n"


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: release_notes.py X.Y.Z")
    sys.stdout.write(section(sys.argv[1]))
