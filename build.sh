#!/bin/bash -eu
{ set +x; } 2>/dev/null
SOURCE=$0
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

OS="Mac"
if [[ -e "/c/" ]]; then
  OS="Windows"
fi

CONFIGURATION=Release
PUBLIC=""
UNITYBUILD=0
CI=0

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
    ;;
    -r|--release)
      CONFIGURATION="Release"
    ;;
    -n|--unity)
      UNITYBUILD=1
    ;;
    -p|--public)
      PUBLIC="-p:PublicRelease=true"
    ;;
    -c)
      shift
      CONFIGURATION=$1
    ;;
    --ispublic)
      shift
      if [[ x"$1" == x"1" ]]; then
        PUBLIC="-p:PublicRelease=true"
      fi
    ;;
    --ci)
      CI=1
    ;;
    --trace)
      { set -x; } 2>/dev/null
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"${APPVEYOR:-}" != x"" ]]; then
  CI=1
fi
if [[ x"${GITHUB_REPOSITORY:-}" != x"" ]]; then
  CI=1
fi
if [[ x"${UNITYBUILD}" == x"1" ]]; then
  CONFIGURATION="${CONFIGURATION}Unity"
fi

pushd $DIR >/dev/null 2>&1

if [[ x"${CI}" == x"0" ]]; then
  dotnet restore
fi
dotnet build -v Detailed --no-restore -c $CONFIGURATION $PUBLIC

popd >/dev/null 2>&1