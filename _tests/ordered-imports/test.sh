#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
export MSYS2_ARG_CONV_EXCL='https://'
dotnet restore "$here/ordered-imports.csproj" \
    --source "$root/Antlr4BuildTasks/bin/Release" \
    --source https://api.nuget.org/v3/index.json
dotnet build "$here/ordered-imports.csproj" -t:Rebuild --no-restore -v:q
