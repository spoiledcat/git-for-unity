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
BUILD=0
UPM=0
UNITYVERSION=2019.2
YAMATO=0

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
    ;;
    -r|--release)
      CONFIGURATION="Release"
    ;;
    -p|--public)
      PUBLIC="-p:PublicRelease=true"
    ;;
    -b|--build)
      BUILD=1
    ;;
    -u|--upm)
      UPM=1
    ;;
    -c)
      shift
      CONFIGURATION=$1
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"${YAMATO_JOB_ID:-}" != x"" ]]; then
  YAMATO=1
  export GITLAB_CI=1
  export CI_COMMIT_TAG="${GIT_TAG:-}"
  export CI_COMMIT_REF_NAME="${GIT_BRANCH:-}"
fi

pushd $DIR >/dev/null 2>&1

if [[ x"${APPVEYOR:-}" == x"" ]]; then
  if [[ x"$OS" != x"Windows" ]]; then
    mono common/nuget.exe restore
  else
    common/nuget.exe restore
  fi
  dotnet restore
fi
dotnet build --no-restore -c $CONFIGURATION $PUBLIC

popd >/dev/null 2>&1