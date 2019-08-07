#!/bin/sh -ex
{ set +x; } 2>/dev/null
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
pushd $DIR >/dev/null
src/multipackage.ts $@
popd >/dev/null