# [How to use]({{ site.baseurl }}/usage)

```

> ./FFXIV_TexTools_CLI-LINUX
Usage: FFXIV_TexTools_CLI.exe [action] {arguments}

Available actions:
  modpack import, mpi      Import a modpack, requires a .ttmp(2) to be specified
  mods list, ml            List all currently installed mods
  mods enable, me          Enable all installed mods
  mods disable, md         Disable all installed mods
  mods refresh, mr         Enable/disable mods as specified in modlist.cfg
  backup, b                Backup clean index files for use in resetting the game
  reset, r                 Reset game to clean state
  problemcheck, p          Check if there are any problems with the game, mod or backup files
  version, v               Display current game version
  help, h                  Display this text

Available arguments:
  -g, --gamedirectory      Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
  -c, --configdirectory    Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
  -b, --backupdirectory    Full path to directory with your index backups
  -t, --ttmp               Full path to .ttmp(2) file (mods import only)
  -C, --custom             Use a modpack's config file to selectively import mods from the pack (modpack import only)

```
### Example uses
#### First time setup
This is only a recommended first time setup. You are not required to make use of the config file created by this application, as all required directories can be specified through commandline instead.
1. The first run will create the application's config file
```
> ./FFXIV_TexTools_CLI-LINUX
```
2. Edit the config file with your editor of choice
```
> nano ~/.config/FFXIV_TexTools_CLI/config.cfg
```
3. Create a backup of your clean index files
```
> ./FFXIV_TexTools_CLI-LINUX backup
```
You are now ready to start modding the game.

#### Before patching the game
You can either:
1. Disable all mods
```
> ./FFXIV_TexTools_CLI-LINUX mods disable -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn
```
2. Reset the game's files entirely 
```
> ./FFXIV_TexTools_CLI-LINUX reset -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -b /path/to/index/backups
```
#### After patching the game
Backup the new index files first
```
> ./FFXIV_TexTools_CLI-LINUX backup -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -b /path/to/index/backups
```
Depending on the chosen step before patching, you now either:
1. Re-enable all mods
```
> ./FFXIV_TexTools_CLI-LINUX mods enable -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn
```
2. Import your modpacks from scratch
```
> ./FFXIV_TexTools_CLI-LINUX modpack import -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -t /path/to/modpack.ttmp2
```

#### Importing a modpack
Importing a full modpack
```
> ./FFXIV_TexTools_CLI-LINUX mpi -t /path/to/modpack.ttmp2
```
Selectively importing mods from a modpack
```
> ./FFXIV_TexTools_CLI-LINUX mpi -t /path/to/modpack.ttmp2 --custom
> nano ~/.config/FFXIV_TexTools_CLI/ModPacks/modpack.cfg
> ./FFXIV_TexTools_CLI-LINUX mpi -t /path/to/modpack.ttmp2 --custom
```
