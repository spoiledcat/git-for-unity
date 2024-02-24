# Git for Unity

This is a Git client for your Unity Editor, supporting all versions from 2017.x onwards (technically also Unity 5.6, but ymmv). It tries very hard to find and use whatever git you have available on your system, and whatever authentication mechanism you have configured for git on the command line, so you can just use it out of the box.

This is a fork of [Git for Unity](https://github.com/unity-technologies/git-for-unity), which in itself is a fork of [GitHub for Unity](https://github.com/github-for-unity/Unity), which I wrote, so I guess the wheel has turned all the way back around 🤷‍♀️ 😁

[![Build status](https://ci.appveyor.com/api/projects/status/8l2f8numfkp6yhnw?svg=true)](https://ci.appveyor.com/project/shana/git-for-unity)

![spinner](https://raw.githubusercontent.com/spoiledcat/git-for-unity/main/spinner.gif)

## How to Install

### Quick install

To install the latest package release, add a new scoped registry in the Package Manager settings, with url `https://registry.spoiledcat.com` and scope `com.spoiledcat`

![Screen Shot 2022-10-19 at 17 07 10](https://user-images.githubusercontent.com/310137/196755895-a2865ae3-70bd-45cc-816d-535b46f09034.png)

In the `My Registries` section, install the `Git for Unity` package

![Screen Shot 2022-10-19 at 19 05 54](https://user-images.githubusercontent.com/310137/196757921-57d29b3f-3376-45ba-8476-c5e1f1c505b6.png)

Once installed, make sure the paths to git and git lfs are configured correctly by opening the Git window and going to the Settings tab.

![image](https://user-images.githubusercontent.com/310137/196758333-178c9561-77fa-4d5b-8ee4-7113c4d8a1dd.png)

Make sure your username and email are correctly set, too.

![image](https://user-images.githubusercontent.com/310137/196758888-7118b3c6-bdbf-46c0-aaf7-68da7433a723.png)

Git for Unity comes with a bundled version of git and git lfs (on Windows), which are old but should work. If you have git installed in your system, click on the `Find system git` button to use those instead.

![image](https://user-images.githubusercontent.com/310137/196759278-d0ab5441-1992-40a7-92d8-bbd4ff38ed38.png)

## What's all this then?

This is a git client for the Unity editor, split into two parts: The API part is a .NET Git Client library, without any dependencies on Unity itself; The UI part is Unity-specific.

The same applies to this project, which is split into two packages - `com.spoiledcat.git.api` - the Git client library; and `com.spoiledcat.git.ui` - the Git UI for the Unity Editor.

Even though this project is currently a fork, since neither GitHub nor Unity seem very interested in supporting developer tooling, this is probably going to become the main implementation of this - this is why this repository is not a GitHub(tm) fork, but a completely separate repository, inheriting the history of both GitHub for Unity and Git for Unity.

## Tracking the latest builds

This repo publishes preview and non-preview packages from the main branch to named branches, one for each package, every time there's a push to the main branch. You can install Git for Unity packages directly from those branches instead, if you want to track the very latest changes.

- Git for Unity latest: https://github.com/spoiledcat/git-for-unity#packages/com.spoiledcat.git/latest
- Git for Unity API latest: https://github.com/spoiledcat/git-for-unity#packages/com.spoiledcat.git.api/latest
- Git for Unity UI latest: https://github.com/spoiledcat/git-for-unity#packages/com.spoiledcat.git.ui/latest

![image](https://user-images.githubusercontent.com/310137/196762066-56e71462-c634-4328-8d0a-00cf2b9e2de9.png)

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

Copyright 2020-2024 Andreia Gaita

Copyright 2019 Unity

The MIT license grant is not for Unity Technologies's trademarks, which include the Unity logo designs. Unity Technologies reserves all trademark and copyright rights in and to all Unity Technologies trademarks.

Copyright 2015 - 2018 GitHub, Inc.

The MIT license grant is not for GitHub's trademarks, which include the GitHub logo designs. GitHub reserves all trademark and copyright rights in and to all GitHub trademarks.
