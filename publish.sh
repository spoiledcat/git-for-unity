#!/bin/bash -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

OS="Mac"
if [[ -e "/c/" ]]; then
  OS="Windows"
fi

PUBLIC=""
BUILD=0
NPM=0
UNITYVERSION=2019.2
BRANCHES=0
NUGET=0
VERSION=
PUBLIC=0

while (( "$#" )); do
  case "$1" in
    -p|--public)
      PUBLIC=1
    ;;
    -b|--build)
      BUILD=1
    ;;
    -u|--npm)
      NPM=1
    ;;
    -c|--branches)
      BRANCHES=1
    ;;
    -g|--nuget)
      NUGET=1
    ;;
    -g|--github)
      GITHUB=1
    ;;
    -v|--version)
      shift
      VERSION=$1
    ;;
    --ispublic)
      shift
      PUBLIC=$1
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


function updateBranchAndPush() {
  local branch=$1
  local destdir=$2
  local pkgdir=$3
  local msg=$4
  local ver=$5
  local publ=$6

  echo "Publishing branch: $branch/latest ($VERSION)"

  pushd $destdir

  git reset --hard 38cb467e3d9d8b49f98019eee5cd463631d576a1
  git clean -xdf
  git reset --hard origin/$branch/latest >/dev/null 2>&1||true
  rm -rf *
  cp -R $pkgdir/* .
  git add .
  git commit -m "$msg"
  git push origin HEAD:$branch/latest

  if [[ $publ -eq 1 ]]; then
      echo "Publishing branch: $branch/$VERSION"
      git push origin HEAD:$branch/$VERSION
  fi

  popd
}

if [[ x"$BRANCHES" == x"1" ]]; then

  if [[ x"$VERSION" == x"" ]]; then
    dotnet tool install -v q --tool-path . nbgv || true
    VERSION=$(./nbgv cloud -s VisualStudioTeamServices --all-vars -p src|grep NBGV_CloudBuildNumber|cut -d']' -f2)
    _public=$(./nbgv cloud -s VisualStudioTeamServices --all-vars -p src|grep NBGV_PublicRelease|cut -d']' -f2)
    if [[ x"${_public}" == x"True" ]]; then
      PUBLIC=1
    fi
  fi

  srcdir=$DIR/build/packages
  destdir=$( cd .. >/dev/null 2>&1 && pwd )/branches
  test -d $destdir && rm -rf $destdir
  mkdir -p $destdir
  git clone -q --branch=empty git@github.com:spoiledcat/git-for-unity $destdir

  pushd $srcdir

  for name in *;do
    test -f $name/package.json || continue
    branch=packages/$name
    msg="$name v$VERSION"
    pkgdir=$srcdir/$name

    updateBranchAndPush "$branch" "$destdir" "$pkgdir" "$msg" "$VERSION" $PUBLIC

  done

  popd

fi

if [[ x"$NUGET" == x"1" ]]; then

  if [[ x"${PUBLISH_KEY:-}" == x"" ]]; then
    echo "Can't publish without a PUBLISH_KEY environment variable in the user:token format" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  if [[ x"${PUBLISH_URL:-}" == x"" ]]; then
    echo "Can't publish without a PUBLISH_URL environment variable" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  for p in "$DIR/build/nuget/**/*nupkg"; do
    dotnet nuget push $p -ApiKey "${PUBLISH_KEY}" -Source "${PUBLISH_URL}"
  done

fi

if [[ x"$NPM" == x"1" ]]; then

  if [[ x"${NPM_TOKEN:-}" == x"" ]]; then
    echo "Can't publish without a NPM_TOKEN environment variable" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  npm config set //registry.spoiledcat.com/:_authToken $NPM_TOKEN
  npm config set always-auth true
  pushd build/npm
  for pkg in *.tgz;do
    npm publish -quiet $pkg
  done
  popd
fi
