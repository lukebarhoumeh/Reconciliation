#!/usr/bin/env bash
set -euo pipefail
# Checks if the TestResults directory exists and contains files.
# Prints a warning if missing or empty but never exits with failure.
results_dir="${1:-TestResults}"
if [ ! -d "$results_dir" ] || [ -z "$(ls -A "$results_dir" 2>/dev/null)" ]; then
    echo "::warning ::$results_dir directory missing or empty"
fi

