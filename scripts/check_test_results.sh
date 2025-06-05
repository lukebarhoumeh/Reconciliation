#!/usr/bin/env bash
set -euo pipefail
# Checks if the TestResults directory exists and contains files.
# Prints a warning if missing or empty but never exits with failure.
# Sets the upload_artifact output for GitHub Actions.
results_dir="${1:-TestResults}"
if [ -d "$results_dir" ] && [ "$(ls -A "$results_dir" 2>/dev/null)" ]; then
    echo "::set-output name=upload_artifact::true"
else
    echo "::warning ::$results_dir directory missing or empty"
    echo "::set-output name=upload_artifact::false"
fi

