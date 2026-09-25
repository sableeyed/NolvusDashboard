#!/usr/bin/env bash
# Mutation testing for Nolvus.Package with Stryker.NET: it makes small changes to the code under
# test and checks that some test fails for each one. Run from anywhere:
#
#   Nolvus.Tests/stryker.sh
#
# Settings live in stryker-config.json next to this script. The HTML report is written to
# Nolvus.Tests/StrykerOutput/<timestamp>/reports/mutation-report.html.
#
# Only the files the tests cover are mutated. ModOrganizer.cs is mostly untested, so only its
# tested methods are. Stryker takes those as character ranges, which move whenever the file
# changes, so they are worked out here from the method names on every run.
set -euo pipefail

cd "$(dirname "$0")"

MOD_ORGANIZER=../Nolvus.Package/Mods/ModOrganizer.cs

SPANS=$(python3 - "$MOD_ORGANIZER" <<'EOF'
import re, sys

text = open(sys.argv[1], encoding="utf-8-sig", newline="").read()
lines = text.splitlines(keepends=True)

for method in ["EnsureInstanceIni", "RepairIniDriveLetters", "ToWinePath", "ToWineIniPath"]:
    start = next(i for i, l in enumerate(lines) if re.search(rf"public static \w+ {method}\(", l))
    end = next(i for i in range(start + 1, len(lines)) if lines[i].rstrip("\r\n") == "        }")
    print(f"{sum(map(len, lines[:start]))}..{sum(map(len, lines[:end + 1]))}")
EOF
)

MUTATE=(
    -m "**/Rules/CopyRule.cs"
    -m "**/Rules/FileCopy.cs"
    -m "**/Rules/RenameRule.cs"
    -m "**/Rules/Rule.cs"
    -m "**/Conditions/CompareCondition.cs"
    -m "**/Conditions/FileSizeCondition.cs"
    -m "**/Utilities/PathResolver.cs"
)

for SPAN in $SPANS; do
    MUTATE+=(-m "**/Mods/ModOrganizer.cs{$SPAN}")
done

dotnet tool restore > /dev/null
dotnet stryker "${MUTATE[@]}" "$@"
