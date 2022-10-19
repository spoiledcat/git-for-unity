#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

YAMATO=0
PARAMS=""

TESTDIR="$DIR/build/tests"
REPORTDIR="$DIR/test-results"
PACKAGESDIR="$DIR/packages"
NUNIT="$PACKAGESDIR/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe"
NURE="$PACKAGESDIR/Nure.1.2.0/tools/nure.exe"

CONFIGURATION=Release
PUBLIC=""
BUILD=0
UPM=0
UNITYVERSION=2019.2
YAMATO=0

while (( "$#" )); do
  case "$1" in
    --yamato)
      YAMATO=1
      shift 1
      ;;
    -o|--out)
      REPORTDIR="$2"
      shift 2
      ;;
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

# set positional arguments in their proper place
eval set -- "$PARAMS"

if [[ x"$YAMATO" == x"1" ]] && [[ x"$REPORTDIR" == x"" ]]; then
  echo "-o|--out cannot be empty"
fi

pushd $DIR >/dev/null 2>&1
{

  if [[ x"$BUILD" == x"1" ]]; then

    if [[ x"${APPVEYOR:-}" == x"" ]]; then
      dotnet restore
    fi

    dotnet build --no-restore -c $CONFIGURATION $PUBLIC
  fi

} || {
  popd
}


pushd "$TESTDIR"
{
  echo "\"$NUNIT\" *Tests.dll --where \"cat != DoNotRunOnAppVeyor\""
  "$NUNIT" *Tests.dll --where "cat != DoNotRunOnAppVeyor"

  if [[ x"$YAMATO" == x"1" ]]; then
    "$NURE" TestResult.xml --html -o "$REPORTDIR"
  fi

} || {
  popd
}
