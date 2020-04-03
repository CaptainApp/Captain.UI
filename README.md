# Captain.UI
![version: 0.6](https://img.shields.io/badge/version-0.6-blue.svg)
![license: BSD 2-Clause](https://img.shields.io/badge/license-BSD_2--Clause-brightgreen.svg)
> Implements [Captain](https://github.com/CaptainApp)'s desktop and in-game HUD

## What's this?
This is a shared library that implements the user interface code that's common to all containers (i.e. desktop and
DirectX software.)

In other words, it implements the HUD, which is responsible for showing the recording toolbar or the informational _tidbits_.

| ![Recording toolbar](https://i.imgur.com/BtoewCd.png) | ![Sample tidbit](https://i.imgur.com/6xNRmAJ.png) |
|---|---|

## Why a shared library?
Because we want to display the UI on top of existing, not only on the desktop. Yes, I am that haywire.

## Building
This project does not make much sense as a standalone library. Refer to the main
[CaptainApp/Captain](https://github.com/CaptainApp/Captain) repository for build instructions.

## Localization
Captain uses .NET resource files (`*.resx`) which include strings and other assets that may be localized. For
translating the UI, you may use the built-in Windows Forms designer features in Visual Studio to modify strings and
other properties. You may want to look to the `Resources/Resources.resx` file, which contains other strings that
are used to display notifications, dialogs and other strings not directly bound to a Windows Forms control.

## Open-source code
This software depends upon awesome open-source software without which it could not be possible.

### Third-party
These open-source projects are also being used (albeit with no actual source code of these being included):
- Multiple [SharpDX](http://sharpdx.org/) libraries, as NuGet package dependencies.  
  `Copyright (c) 2010-2015 SharpDX - Alexandre Mutel`