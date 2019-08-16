#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

pushd $DIR
git clean -xdf -e .Editor -e UnityProject -e .vs -e .store
popd