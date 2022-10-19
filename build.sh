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
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"$UNITYBUILD" == x"1" ]]; then
  CONFIGURATION="${CONFIGURATION}Unity"
fi

pushd $DIR >/dev/null 2>&1

if [[ x"${APPVEYOR:-}" == x"" ]]; then
  dotnet restore
fi
dotnet build --no-restore -c $CONFIGURATION $PUBLIC

popd >/dev/null 2>&1