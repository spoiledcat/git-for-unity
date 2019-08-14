#!/bin/sh -eu
{ set +x; } 2>/dev/null

SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )"/.. >/dev/null 2>&1 && pwd )"

dotnet tool install -g nbgv >/dev/null 2>&1 || true
nbgv get-version -p "$DIR/src/com.unity.git.api" -v AssemblyInformationalVersion