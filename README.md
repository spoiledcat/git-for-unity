# Git for Unity

This is a fork of [GitHub for Unity](https://github.com/github-for-unity/Unity).

[![Build Status](https://ci.appveyor.com/api/projects/status/github/unity-technologies/git-for-unity?branch=master&svg=true)](https://ci.appveyor.com/project/shana/git-for-unity)

## What's all this then?

This project is a fork of GitHub for Unity and is currently in preview. The API part of GitHub for Unity is a .NET Git Client library, without any dependencies on Unity itself. Only the UI part of GitHub for Unity is Unity-specific.

The same applies to this project, which is split into two packages - `com.unity.git.api` - the Git client library; and `com.unity.git.ui` - a Unity Git UI based on the GitHub for Unity UI, which examplifies how to use the Git client library.

Even though this project is currently a fork, we plan for the API part of this project to become the authoritative implementation source, and split entirely from the original GitHub for Unity implementation.

We're doing this so we can improving the integration between Git and Unity
by exposing additional version control C# APIs that Git for Unity can leverage.
Therefore, the API may change while we build this, and this project may become dependent on
more recent versions of Unity.

## License

**[MIT](LICENSE)**

Copyright 2019 Unity

The MIT license grant is not for Unity Technologies's trademarks, which include the logo
designs. Unity Technologies reserves all trademark and copyright rights in and to all
Unity Technologies trademarks.

The MIT license grant is not for GitHub's trademarks, which include the logo
designs. GitHub reserves all trademark and copyright rights in and to all
GitHub trademarks. GitHub's logos include, for instance, the stylized
Invertocat designs that include "logo" in the file title in the following
folder: [IconsAndLogos](src/com.unity.git.ui/UI/IconsAndLogos).

Copyright 2015 - 2018 GitHub, Inc.
