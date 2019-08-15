#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

YAMATO=0
PARAMS=""

TESTDIR="$DIR/build/tests/net471"
REPORTDIR="$DIR/test-results"
PACKAGESDIR="$DIR/packages"
NUNIT="$PACKAGESDIR/NUnit.ConsoleRunner.3.10.0/tools/nunit3-console.exe"
NURE="$PACKAGESDIR/Nure.1.2.0/tools/nure.exe"

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
    --) # end argument parsing
      shift
      break
      ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
      PARAMS="$PARAMS $1"
      shift
      ;;
  esac
done

# set positional arguments in their proper place
eval set -- "$PARAMS"

if [[ x"$YAMATO" == x"1" ]] && [[ x"$REPORTDIR" == x"" ]]; then
  echo "-o|--out cannot be empty"
fi

pushd "$TESTDIR"

{

  "$NUNIT" "$TESTDIR/*Tests.dll" --where "cat != DoNotRunOnAppVeyor"

  if [[ x"$YAMATO" == x"1" ]]; then
    "$NURE" TestResult.xml --html -o "$REPORTDIR"
  fi

} || {
  popd
}
