# Git for Unity

This is a Git client for your Unity Editor, supporting all versions from 2017.x onwards (technically also Unity 5.6, but ymmv). It tries very hard to find and use whatever git you have available on your system, and whatever authentication mechanism you have configured for git on the command line, so you can just use it out of the box.

This is a fork of [Git for Unity](https://github.com/unity-technologies/git-for-unity), which in itself is a fork of [GitHub for Unity](https://github.com/github-for-unity/Unity).

[![Build Status](https://ci.appveyor.com/api/projects/status/github/spoiledcat/git-for-unity?branch=main&svg=true)](https://ci.appveyor.com/project/shana/git-for-unity)

## What's all this then?

This is a git client for the Unity editor, split into two parts: The API part is a .NET Git Client library, without any dependencies on Unity itself; The UI part is Unity-specific.

**Until I get to renaming the packages**, the build output of this project is two packages - `com.unity.git.api` - the Git client library; and `com.unity.git.ui` - a Unity Git UI based on the GitHub for Unity UI, which exemplifies how to use the Git client library.

Even though this project is currently a fork, since neither GitHub nor Unity seem very interested in supporting developer tooling, this is probably going to become the main implementation of this - this is why this repository is not a GitHub(tm) fork, but a completely separate repository, inheriting the history of both GitHub for Unity and Git for Unity.

## How to Build

This repository is LFS-enabled. To clone it, you should use a git client that supports git LFS 2.x.

Check [How to Build](https://raw.githubusercontent.com/spoiledcat/git-for-unity/master/BUILD.md) for all the build, packaging and versioning details.

### Release build 

`build[.sh|cmd] -r`

### Release build and package

`pack[.sh|cmd] -r -b`

### Release build and test

`test[.sh|cmd] -r -b`


### Where are the build artifacts?

Packages sources are in `build/packages`.

Nuget packages are in `build/nuget`.

Packman (npm) packages are in `upm-ci~/packages`.

Binaries for each project are in `build/bin` for the main projects, `build/Samples/bin` for the samples, and `build/bin/tests` for the tests.

### How to bump the major or minor parts of the version

The `version.json` file in the root of the repo controls the version for all packages.
Set the major and/or minor number in it and **commit the change** so that the next build uses the new version.
The patch part of the version is the height of the commit tree since the last manual change of the `version.json`
file, so once you commit a change to the major or minor parts, the patch will reset back to 0.

## License

**[MIT](LICENSE)**

Copyright 2020-2022 Andreia Gaita

Copyright 2019 Unity

The MIT license grant is not for Unity Technologies's trademarks, which include the Unity logo designs. Unity Technologies reserves all trademark and copyright rights in and to all Unity Technologies trademarks.

Copyright 2015 - 2018 GitHub, Inc.

The MIT license grant is not for GitHub's trademarks, which include the GitHub logo designs. GitHub reserves all trademark and copyright rights in and to all GitHub trademarks.
