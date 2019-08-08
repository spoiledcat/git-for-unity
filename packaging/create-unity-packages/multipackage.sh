#!/bin/sh -ex
{ set +x; } 2>/dev/null
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
pushd $DIR >/dev/null
if [ ! -d 'node_modules' ]; then node ../yarn.js install --prefer-offline; fi
src/multipackage.ts $@
popd >/dev/null