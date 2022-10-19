#!/bin/sh -eu
{ set +x; } 2>/dev/null

SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )"/.. >/dev/null 2>&1 && pwd )"

FLAGS=""
PARAMS=""
OUTDIR="$DIR/artifacts"
BUILDDIR="$DIR/build/packages"
SRCDIR="$DIR/src"
SCRIPTDIR="$DIR/packaging/create-unity-packages"
TMPDIR=""
VERSION=""

RM="rm -rf"

while (( "$#" )); do
  case "$1" in
    -v|--version)
      VERSION="$2"
      shift 2
      ;;
    -t|--tmp)
      TMPDIR="$2"
      shift 2
      ;;
    -u|--skipUnity)
      FLAGS="$FLAGS $1"
      shift
      ;;
    -p|--skipPackman)
      FLAGS="$FLAGS $1"
      shift
      ;;
    -m|--skipUpm)
      FLAGS="$FLAGS $1"
      shift
      ;;
    -k|--skip)
      FLAGS="$FLAGS $1"
      shift
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

if [[ x"$VERSION" == x"" ]]; then
    echo "-v|--version argument missing"
    exit -1
fi

if [[ x"$TMPDIR" == x"" ]]; then
    TMPDIR="$DIR/obj"
fi

if [[ -e "$TMPDIR" ]]; then
    $RM "$TMPDIR"
fi

# set positional arguments in their proper place
eval set -- "$PARAMS"

PKGNAME="com.spoiledcat.git.api"
PKGSRCDIR="$BUILDDIR/$PKGNAME"
IGNOREFILE="$DIR/src/$PKGNAME/.npmignore"
BASEINSTALL="Packages/$PKGNAME"

"$SCRIPTDIR/run.sh" -s "$PKGSRCDIR" -o "$OUTDIR" -n "$PKGNAME" -v "$VERSION" -i "$IGNOREFILE" -t "$BASEINSTALL" --tmp "$TMPDIR" "$FLAGS"

PKGNAME="com.spoiledcat.git.ui"
PKGSRCDIR="$BUILDDIR/$PKGNAME"
IGNOREFILE="$DIR/src/$PKGNAME/.npmignore"
BASEINSTALL="Packages/$PKGNAME"

"$SCRIPTDIR/run.sh" -s "$PKGSRCDIR" -o "$OUTDIR" -n "$PKGNAME" -v "$VERSION" -i "$IGNOREFILE" -t "$BASEINSTALL" --tmp "$TMPDIR" "$FLAGS"
