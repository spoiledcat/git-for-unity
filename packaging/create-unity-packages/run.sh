#!/bin/sh -ex
{ set +x; } 2>/dev/null
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
pushd $DIR >/dev/null
node ../yarn.js install --prefer-offline
src/index.ts $@
popd >/dev/null