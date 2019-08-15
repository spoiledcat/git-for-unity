#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

dotnet tool install --tool-path . nbgv || true
pushd $DIR/src/com.unity.git.api
version=$($DIR/nbgv get-version|grep AssemblyInformationalVersion|cut -d' ' -f2)
popd

echo "Packaging version $version"
scripts/create-packages.sh -v $version -t "$DIR/PackageSources" -u -p
