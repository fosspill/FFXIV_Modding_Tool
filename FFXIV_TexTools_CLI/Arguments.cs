using System;
using System.IO;
using System.Linq;
using xivModdingFramework.Mods;

namespace FFXIV_TexTools_CLI.Commandline
{
    public class Arguments
    {
        public Arguments(){}

        public void ArgumentHandler(string[] args)
        {
            MainClass main = new MainClass();
            string helpText = $"Usage: {Path.GetFileName(Environment.GetCommandLineArgs()[0])} [action] {"{arguments}"}\n\n";
            helpText = helpText + @"Available actions:
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
  -C, --custom             Use a modpack's config file to selectively import mods from the pack (mods import only)";
            int i = 0;
            string ttmpPath = "";
            bool customImport = false;
            foreach (string cmdArg in args)
            {
                string nextArg = "";
                if (args.Count() > 1 && i + 1 < args.Count())
                    nextArg = args[i + 1];
                if (cmdArg.StartsWith("-"))
                {
                    string arg = cmdArg.Split('-').Last();
                    switch (arg)
                    {
                        case "g":
                        case "gamedirectory":
                            if (!nextArg.StartsWith("-"))
                            {
                                main._gameDirectory = new DirectoryInfo(Path.Combine(nextArg, "game"));
                                main._indexDirectory = new DirectoryInfo(Path.Combine(nextArg, "game", "sqpack", "ffxiv"));
                            }
                            continue;
                        case "c":
                        case "configdirectory":
                            if (!nextArg.StartsWith("-"))
                                main._configDirectory = new DirectoryInfo(nextArg);
                            continue;
                        case "b":
                        case "backupdirectory":
                            if (!nextArg.StartsWith("-"))
                                main._backupDirectory = new DirectoryInfo(nextArg);
                            continue;
                        case "t":
                        case "ttmp":
                            if (!nextArg.StartsWith("-"))
                                ttmpPath = nextArg;
                            continue;
                        case "C":
                        case "custom":
                            customImport = true;
                            continue;
                        default:
                            main.PrintMessage($"Unknown argument {arg}", 3);
                            continue;
                    }
                }
                i++;
            }
            string secondAction = "";
            if (args.Count() > 1)
                secondAction = args[1];
            switch (args[0])
            {
                case "mpi":
                    if (string.IsNullOrEmpty(ttmpPath))
                    {
                        main.PrintMessage("Can't import without a modpack to import. Specify one with -t", 2);
                        return;
                    }
                    if (main._gameDirectory != null)
                        main.ImportModpackHandler(new DirectoryInfo(ttmpPath), customImport);
                    else
                        main.PrintMessage("Importing requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "mpe":
                    // function to export modpacks
                    break;
                case "mpinfo":
                    // function to list info about modpack
                    break;
                case "modpack":
                    if (secondAction == "import")
                        goto case "mpi";
                    if (secondAction == "export")
                        goto case "mpe";
                    if (secondAction == "info")
                        goto case "mpinfo";
                    break;
                case "mr":
                    if (main._gameDirectory != null)
                        main.SetModActiveStates();
                    else
                        main.PrintMessage("Enabling/disabling mods requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "ml":
                    // function to list current mods
                    break;
                case "me":
                    if (main._gameDirectory != null)
                    {
                        var modding = new Modding(main._indexDirectory);
                        modding.ToggleAllMods(true);
                        main.PrintMessage("Successfully enabled all mods", 1);
                    }
                    else
                        main.PrintMessage("Enabling mods requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "md":
                    if (main._gameDirectory != null)
                    {
                        var modding = new Modding(main._indexDirectory);
                        modding.ToggleAllMods(false);
                        main.PrintMessage("Successfully disabled all mods", 1);
                    }
                    else
                        main.PrintMessage("Disabling mods requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "mods":
                    if (secondAction == "refresh")
                        goto case "mr";
                    if (secondAction == "list")
                        goto case "ml";
                    if (secondAction == "enable")
                        goto case "me";
                    if (secondAction == "disable")
                        goto case "md";
                    break;
                case "backup":
                case "b":
                    if (main._gameDirectory != null && main._backupDirectory != null)
                        main.BackupIndexes();
                    else
                        main.PrintMessage("Backing up index files requires having both your game and backup directories set through the config file or with -g and -b specified", 2);
                    break;
                case "reset":
                case "r":
                    if (main._gameDirectory != null && main._backupDirectory != null)
                        main.ResetMods();
                    else
                        main.PrintMessage("Reseting game files requires having both your game and backup directories set through the config file or with -g and -b specified", 2);
                    break;
                case "problemcheck":
                case "p":
                    if (main._gameDirectory != null && main._backupDirectory != null && main._configDirectory != null)
                        main.ProblemChecker();
                    else
                        main.PrintMessage("Checking for problems requires having your game, backup and config directories set through the config file or with -g, -b and -c specified", 2);
                    break;
                case "version":
                case "v":
                    if (main._gameDirectory != null)
                        main.CheckGameVersion();
                    else
                        main.PrintMessage("Checking the game version requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "help":
                case "h":
                    main.PrintMessage(helpText);
                    break;
                default:
                    main.PrintMessage($"Unknown action: {args[0]}");
                    main.PrintMessage(helpText);
                    break;
            }
        }
    }
}
