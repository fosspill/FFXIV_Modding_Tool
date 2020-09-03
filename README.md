[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/fosspill/FFXIV_Modding_Tool/graphs/commit-activity) [![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=fosspill/FFXIV_Modding_Tool)](https://dependabot.com) [![CodeFactor](https://www.codefactor.io/repository/github/fosspill/ffxiv_modding_tool/badge/default)](https://www.codefactor.io/repository/github/fosspill/ffxiv_modding_tool/overview/default) ![GitHub All Releases](https://img.shields.io/github/downloads/fosspill/FFXIV_Modding_Tool/total) ![.NET Core Default](https://github.com/fosspill/FFXIV_Modding_Tool/workflows/.NET%20Core%20Default/badge.svg)
<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-4-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

Documentation with examples: https://ffmt.onrender.com/docs 👈

# FFMT - FFXIV Modding Tool



**FFMT** is a crossplatform CLI alternative to the Windows-Only *Textools* for Mac, Windows and Linux!

**This project is NOT affiliated with FFXIV_TexTools_UI**

Depends on the latest version (2.3.0.1) of *[xivModdingFramework](https://github.com/TexTools/xivModdingFramework)*

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

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/kainz0r"><img src="https://avatars0.githubusercontent.com/u/6439314?v=4?s=100" width="100px;" alt=""/><br /><sub><b>kainz0r</b></sub></a><br /><a href="https://github.com/fosspill/FFXIV_Modding_Tool/issues?q=author%3Akainz0r" title="Bug reports">🐛</a> <a href="#userTesting-kainz0r" title="User Testing">📓</a></td>
    <td align="center"><img src="https://avatars0.githubusercontent.com/u/36456160?v=4?s=100" width="100px;" alt=""/><br /><sub><b>taylor85345</b></sub><br /><a href="https://github.com/fosspill/FFXIV_Modding_Tool/issues?q=author%3Ataylor85345" title="Bug reports">🐛</a> <a href="#userTesting-taylor85345" title="User Testing">📓</a></td>
    <td align="center"><a href="https://github.com/shinnova"><img src="https://avatars0.githubusercontent.com/u/12647312?v=4?s=100" width="100px;" alt=""/><br /><sub><b>shinnova</b></sub></a><br /><a href="https://github.com/fosspill/FFXIV_Modding_Tool/commits?author=shinnova" title="Code">💻</a> <a href="#example-shinnova" title="Examples">💡</a> <a href="#maintenance-shinnova" title="Maintenance">🚧</a> <a href="https://github.com/fosspill/FFXIV_Modding_Tool/pulls?q=is%3Apr+reviewed-by%3Ashinnova" title="Reviewed Pull Requests">👀</a></td>
    <td align="center"><a href="https://github.com/fosspill"><img src="https://avatars3.githubusercontent.com/u/1491401?v=4?s=100" width="100px;" alt=""/><br /><sub><b>fosspill</b></sub></a><br /><a href="https://github.com/fosspill/FFXIV_Modding_Tool/commits?author=fosspill" title="Code">💻</a> <a href="#example-fosspill" title="Examples">💡</a> <a href="https://github.com/fosspill/FFXIV_Modding_Tool/commits?author=fosspill" title="Documentation">📖</a> <a href="#ideas-fosspill" title="Ideas, Planning, & Feedback">🤔</a></td>
  </tr>
</table>

<!-- markdownlint-enable -->
<!-- prettier-ignore-end -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
