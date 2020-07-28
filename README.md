[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/fosspill/FFXIV_Modding_Tool/graphs/commit-activity) [![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=fosspill/FFXIV_Modding_Tool)](https://dependabot.com) [![CodeFactor](https://www.codefactor.io/repository/github/fosspill/ffxiv_modding_tool/badge/default)](https://www.codefactor.io/repository/github/fosspill/ffxiv_modding_tool/overview/default) ![GitHub All Releases](https://img.shields.io/github/downloads/fosspill/FFXIV_Modding_Tool/total) ![.NET Core Build](https://github.com/fosspill/FFXIV_Modding_Tool/workflows/.NET%20Core%20Build/badge.svg)

Documentation with examples: https://ffmt.onrender.com/docs 👈

# FFMT - FFXIV Modding Tool



**FFMT** is a crossplatform CLI alternative to the Windows-Only *Textools* for Mac, Windows and Linux!

**This project is NOT affiliated with FFXIV_TexTools_UI**

Depends on the development version (2.0.12.9) of *[xivModdingFramework](https://github.com/esrinzou/xivModdingFramework/)*

# Features!
List is sorted by priority
- [x] [Full Mac, Linux and Windows support](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/1)
- [x] [**Import modpacks (ttmp files)**](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/2)
- [x] [Storable configuration for important directories](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/3)
- [x] [Backup and restore of important game files](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/4)
- [x] [Manage mods (enable/disable)](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/27)
- [ ] [Import specific textures / models (including advanced import options)](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/5)
- [ ] [Export specific textures / models](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/6)
- [x] [Check for problems](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/7)
- [ ] [ModPack creation](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/8)
- [ ] [TexTools interchangeability](https://github.com/fosspill/FFXIV_TexTools_CLI/issues/67)

## How to Install, Build and Use:

https://ffmt.pwd.cat

https://ffmt.pwd.cat/docs/

## Notes on building the framework

Ensure that the two non-existant project files (.xUnit and exChecker) are removed from the .sln. To build the framework dotnet core version 3.1.100+ is required. Build the framework using `dotnet build -c Release` and place the resulting dll file found in `xivModdingFramework/xivModdingFramework/bin/Release/netstandard2.0` in FFMT's `references` folder.

License
----

GNU General Public License v3.0


**Free Software, Hell Yeah!**
