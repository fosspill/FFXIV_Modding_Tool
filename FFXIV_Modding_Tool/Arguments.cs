using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Helpers;

namespace FFXIV_Modding_Tool.Commandline
{
    public class Arguments
    {
        public Arguments(){}
        MainClass main = new MainClass();

        public void ArgumentHandler(string[] args)
        {
            string helpText = $"Usage: {Path.GetFileName(Environment.GetCommandLineArgs()[0])} [action] {"{arguments}"}\n\n";
            helpText = helpText + @"Available actions:
  modpack import, mpi      Import a modpack, requires a .ttmp(2) to be specified
  mods enable, me          Enable all installed mods
  mods disable, md         Disable all installed mods
  mods refresh, mr         Enable/disable mods as specified in modlist.cfg
  backup, b                Backup clean index files for use in resetting the game
  reset, r                 Reset game to clean state
  problemcheck, pc         Check if there are any problems with the game, mod or backup files
  version, v               Display current application and game version
  help, h                  Display this text

Available arguments:
  -g, --gamedirectory      Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
  -c, --configdirectory    Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
  -b, --backupdirectory    Full path to directory with your index backups
  -t, --ttmp               Full path to .ttmp(2) file (mods import only)
  -C, --custom             Use a modpack's config file to selectively import mods from the pack (modpack import only)
  -npc, --noproblemcheck   Skip the problem check after importing a modpack";
            string ttmpPath = "";
            bool customImport = false;
            bool skipProblemCheck = false;
            if (args.Length == 0)
            {
                main.PrintMessage(helpText);
                return;
            }
            foreach (string cmdArg in args)
            {
                string nextArg = "";
                int i = Array.IndexOf(args, cmdArg);
                if (args.Length > 1 && i + 1 < args.Length)
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
                                MainClass._gameDirectory = new DirectoryInfo(Path.Combine(nextArg, "game"));
                                MainClass._indexDirectory = new DirectoryInfo(Path.Combine(nextArg, "game", "sqpack", "ffxiv"));
                            }
                            continue;
                        case "c":
                        case "configdirectory":
                            if (!nextArg.StartsWith("-"))
                                MainClass._configDirectory = new DirectoryInfo(nextArg);
                            continue;
                        case "b":
                        case "backupdirectory":
                            if (!nextArg.StartsWith("-"))
                                MainClass._backupDirectory = new DirectoryInfo(nextArg);
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
                        case "npc":
                        case "noproblemcheck":
                            skipProblemCheck = true;
                            continue;
                        default:
                            main.PrintMessage($"Unknown argument {arg}", 3);
                            continue;
                    }
                }
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
                    if (BackupsMissingOrOutdated())
                        return;
                    if (PreviouslyModifiedGame())
                        return;
                    if (MainClass._gameDirectory != null)
                        main.ImportModpackHandler(new DirectoryInfo(ttmpPath), customImport, skipProblemCheck);
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
                    if (MainClass._gameDirectory != null)
                        main.SetModActiveStates();
                    else
                        main.PrintMessage("Enabling/disabling mods requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "ml":
                    // function to list current mods
                    break;
                case "me":
                    if (MainClass._gameDirectory != null)
                    {
                        var modding = new Modding(MainClass._indexDirectory);
                        modding.ToggleAllMods(true);
                        main.PrintMessage("Successfully enabled all mods", 1);
                    }
                    else
                        main.PrintMessage("Enabling mods requires having your game directory set either through the config file or with -g specified", 2);
                    break;
                case "md":
                    if (MainClass._gameDirectory != null)
                    {
                        var modding = new Modding(MainClass._indexDirectory);
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
                    if (PreviouslyModifiedGame())
                        return;
                    if (MainClass._gameDirectory != null && MainClass._backupDirectory != null)
                        main.BackupIndexes();
                    else
                        main.PrintMessage("Backing up index files requires having both your game and backup directories set through the config file or with -g and -b specified", 2);
                    break;
                case "reset":
                case "r":
                    if (MainClass._gameDirectory != null && MainClass._backupDirectory != null)
                        main.ResetMods();
                    else
                        main.PrintMessage("Resetting game files requires having both your game and backup directories set through the config file or with -g and -b specified", 2);
                    break;
                case "problemcheck":
                case "pc":
                    if (PreviouslyModifiedGame())
                        return;
                    if (MainClass._gameDirectory != null && MainClass._backupDirectory != null && MainClass._configDirectory != null)
                        main.ProblemChecker();
                    else
                        main.PrintMessage("Checking for problems requires having your game, backup and config directories set through the config file or with -g, -b and -c specified", 2);
                    break;
                case "version":
                case "v":
                    main.CheckVersions();
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

        bool BackupsMissingOrOutdated()
        {
            main.PrintMessage("Checking backups before proceeding...");
            bool keepGoing = true;
            bool problemFound = false;
            if (MainClass._backupDirectory == null)
            {
                main.PrintMessage($"No backup directory specified, can't check the status of backups.\nYou are strongly recommended to add a backup directory in {Path.Combine(MainClass._projectconfDirectory.FullName, "config.cfg")} and running the 'backup' command before proceeding", 2);
                problemFound = true;
            }
            else if (MainClass._gameDirectory == null)
            {
                main.PrintMessage("No game directory specified, can't check if backups are up to date", 2);
                problemFound = true;
            }
            else
            {
                var filesToCheck = new XivDataFile[] { XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
                ProblemChecker problemChecker = new ProblemChecker(MainClass._indexDirectory);
                foreach (var file in filesToCheck)
                {
                    if (!File.Exists(Path.Combine(MainClass._backupDirectory.FullName, $"{file.GetDataFileName()}.win32.index")))
                    {
                        main.PrintMessage($"One or more index files could not be found in {MainClass._backupDirectory.FullName}. Creating new ones or downloading them from the TexTools discord is recommended", 2);
                        problemFound = true;
                        break;
                    }
                    if (!problemChecker.CheckForOutdatedBackups(file, MainClass._backupDirectory))
                    {
                        main.PrintMessage($"One or more index files are out of date in {MainClass._backupDirectory.FullName}. Recreating or downloading them from the TexTools discord is recommended", 2);
                        problemFound = true;
                        break;
                    }
                }
            }
            if (problemFound)
                keepGoing = PromptContinuation();
            else
                main.PrintMessage("All backups present and up to date", 1);
            return !keepGoing;
        }

        bool PreviouslyModifiedGame()
        {
            bool keepGoing = true;
            if (MainClass._gameDirectory != null)
            {
                string modlistPath = Path.Combine(MainClass._gameDirectory.FullName, "XivMods.json");
                if (!File.Exists(modlistPath))
                {
                    ProblemChecker problemChecker = new ProblemChecker(MainClass._indexDirectory);
                    var filesToCheck = new XivDataFile[] { XivDataFile._0A_Exd, XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
                    bool modifiedIndex = false;
                    foreach (var file in filesToCheck)
                    {
                        if (problemChecker.CheckIndexDatCounts(file))
                        {
                            modifiedIndex = true;
                            break;
                        }
                    }
                    if (modifiedIndex)
                    {
                        main.PrintMessage("HERE BE DRAGONS\nPreviously modified game files found\nUse the originally used tool to start over, or reinstall the game before using this tool", 2);
                        keepGoing = PromptContinuation();
                    }
                }
                else
                {
                    var modData = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(modlistPath));
                    bool unsupportedSource = false;
                    foreach (Mod mod in modData.Mods)
                    {
                        if (mod.source != "FFXIV_Modding_Tool")
                        {
                            unsupportedSource = true;
                            break;
                        }
                    }
                    if (unsupportedSource)
                    {
                        main.PrintMessage("Found a mod applied by an unknown application, game stability cannot be guaranteed", 3);
                        keepGoing = PromptContinuation();
                    }
                }
            }
            return !keepGoing;
        }

        bool PromptContinuation()
        {
            main.PrintMessage("Continue anyway? y/N");
            string answer = Console.ReadKey().KeyChar.ToString();
            if (answer == "y")
            {
                Console.Write("\n");
                return true;
            }
            if (answer != "\n")
                Console.Write("\n");
            return false;
        }
    }
}
