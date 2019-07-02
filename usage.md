# [How to use]({{ site.baseurl }}/usage)

```

> ./FFMT-LINUX
Usage: FFMT.exe [action] {arguments}

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

<details>
<summary>
  <b>First time setup 👈</b>
</summary>
  
This is only a recommended first time setup. You are not required to make use of the config file created by this application, as all required directories can be specified through commandline instead.

1. The first run will create the application's config file
```
> ./FFMT-LINUX
```
2. Edit the config file with your editor of choice
```
> nano ~/.config/FFMT/config.cfg
```
3. Create a backup of your clean index files
```
> ./FFMT-LINUX backup
```

You are now ready to start modding the game.

</details>



<details>
<summary>
  <b>Before updating/patching the game (Game Updates are announced on the lodestone) 👈</b>
</summary>
  
##### Before game patch
You can either:
1. Disable all mods
```
> ./FFMT-LINUX mods disable -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn
```
2. Reset the game's files entirely 
```
> ./FFMT-LINUX reset -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -b /path/to/index/backups
```

##### After game patch
Backup the new index files first
```
> ./FFMT-LINUX backup -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -b /path/to/index/backups
```
Depending on the chosen step before patching, you now either:
1. Re-enable all mods
```
> ./FFMT-LINUX mods enable -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn
```
2. Import your modpacks from scratch
```
> ./FFMT-LINUX modpack import -g /path/to/FINAL\ FANTASY\ XIV\ - A\ Realm\ Reborn -t /path/to/modpack.ttmp2
```
</details>




<details>
<summary>
  <b>Importing a modpack 👈</b>
</summary>
  
There are two main types of modpacks: Simple and Wizard.

Simple requires no user interaction, but you can customize your import if you'd like.
Wizard does require user interaction.

#### Find the type

Firstly, figure out what kind of modpack this is:

```
> ./FFMT-LINUX mpinfo -t /path/to/modpack.ttmp2
Type: Simple
Author: 80cent
Version: 1.1
Description: N/A
Number of mods: 9001
```

#### Importing

Importing a full modpack
```
> ./FFMT-LINUX mpi -t /path/to/modpack.ttmp2
```
#### Simple Modpack Custom Import

Selectively importing mods from a simple modpack
```
> ./FFMT-LINUX mpi -t /path/to/modpack.ttmp2 --custom
> nano ~/.config/FFMT/ModPacks/modpack.cfg
> ./FFMT-LINUX mpi -t /path/to/modpack.ttmp2 --custom
```

#### Wizard Modpack import

Follow the on-screen prompts. Example:
```
Choices:
    1 - Gryffindor
         Description
    2 - Slytherin
         Description
    3 - Ravenclaw
         Description
    4 - HuFflEpuFf
         Description
         
  Choose one or multiple (eg: 1 2 3, 0-3, *)
  > 1-3
```

- Choices are separated by spaces.
- \* selects all.
- Ranges can be combined (eg: 1-3 5-6, to skip choice 4)


</details>

<details>
<summary>
  <b>Enabling or disabling specific mods (Mod management) 👈</b>
</summary>


Edit the modlist.cfg file within your operatingsystem's configuration directory. 
Set Enabled to True or False depending on what you want.
 Then run:
 ```
 > ./FFMT-LINUX mr
 ```
 or
 ```
 > ./FFMT-LINUX mods refresh
 ```
 
 </details>
