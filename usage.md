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
  -C, --custom             Use a modpack's config file to selectively import mods from the pack (mods import only)

```

