#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

UPMDIR="$DIR/upm-ci~/packages"
PACKAGE=""

while (( "$#" )); do
  case "$1" in
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
      if [[ x"$PACKAGE" != x"" ]]; then
        echo "Invalid argument $1"
        exit -1
      fi
      PACKAGE="$1"
      shift
      ;;
  esac
done

if [[ x"$PACKAGE" == x"" ]]; then
    echo "Package name is required"
    exit -1
fi

pushd $DIR
{
    powershell scripts/CopyPackagesForUpm.ps1 "$DIR/artifacts/combined-$PACKAGE.json" "$UPMDIR"
    export GITHUB_UNITY_DISABLE=1
    node ../upm-ci-utils/index.js package test -u 2019.1 --package-path $DIR/PackageSources/$PACKAGE
} || true
popd
