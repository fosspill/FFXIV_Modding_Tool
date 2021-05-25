using System;
using System.IO;
using System.Linq;
using System.Reflection;
using xivModdingFramework.Cache;
using xivModdingFramework.General.Enums;
using System.Collections.Generic;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Validation;
using FFXIV_Modding_Tool.FirstTimeSetup;

namespace FFXIV_Modding_Tool.Commandline
{
    public class Arguments
    {
        public Arguments(){}
        MainClass main = new MainClass();
        Config config = new Config();
        Validators validation = new Validators();
        SetupCommand setup = new SetupCommand();
        List<DirectoryInfo> ttmpPaths = new List<DirectoryInfo>();
        DirectoryInfo outputFile = new DirectoryInfo("/tmp/placeholder.ttmp");
        bool useWizard = false;
        bool importAll = false;
        bool skipProblemCheck = false;
        Dictionary<string, Dictionary<string, Action>> fullActions = new Dictionary<string, Dictionary<string, Action>>();
        Dictionary<string, string> actionAliases = new Dictionary<string, string>();
        Dictionary<List<string>, Action<string>> argumentsDict = new Dictionary<List<string>, Action<string>>();
        string requestedAction;

        public void ArgumentHandler(string[] args)
        {
            if (!File.Exists(Config.configFile) || string.IsNullOrEmpty(File.ReadAllText(Config.configFile)))
                config.CreateDefaultConfig();
            if (args.Length == 0)
            {
                SendHelpText();
                return;
            }
            SetupDicts();
            ReadArguments(args);
        }

        public void SetupDicts()
        {
            fullActions = new Dictionary<string, Dictionary<string, Action>>{
                {"mods", new Dictionary<string, Action>{
                    {"refresh", new Action(() => { main.SetModActiveStates(); })},
                    {"enable", new Action(() => { main.ToggleModStates(true); })},
                    {"disable", new Action(() => { main.ToggleModStates(false); })}}},
                {"modpack", new Dictionary<string, Action>{
                    {"import", new Action(() => { 
                    if (useWizard && importAll)
                        {
                            main.PrintMessage("You can't use the import wizard and skip the wizard at the same time", 3);
                            useWizard = false;
                            importAll = false;
                        }
                        main.ImportModpackHandler(ttmpPaths, useWizard, importAll, skipProblemCheck); })},
                    {"info", new Action(() => { main.GetModpackInfo(ttmpPaths); })},
                    {"create", new Action(() => { main.CreateModpack(outputFile); })},
                    }},
                {"backup", new Dictionary<string, Action>{
                    {"", new Action(() => { main.BackupIndexes(); })}
                }},
                {"reset", new Dictionary<string, Action>{
                    {"", new Action(() => { main.ResetMods(); })}
                }},
                {"problemcheck", new Dictionary<string, Action>{
                    {"", new Action(() => { main.ProblemChecker(); })}
                }},
                {"version", new Dictionary<string, Action>{
                    {"", new Action(() => { if (MainClass._gameDirectory == null)
                    MainClass._gameDirectory = new DirectoryInfo(Path.Combine(config.ReadConfig("GameDirectory"), "game"));
                    main.CheckVersions(); })}
                }},
                {"help", new Dictionary<string, Action>{
                    {"", new Action(() => { SendHelpText(); })}
                }},
                {"setup", new Dictionary<string, Action>{
                    {"", new Action(() => { setup.ExecuteSetup(); })}
                }},
            };
            actionAliases = new Dictionary<string, string>{
                {"mpi", "modpack import"},
                {"mpinfo", "modpack info"},
                {"mpc", "modpack create"},
                {"mr", "mods refresh"},
                {"me", "mods enable"},
                {"md", "mods disable"},
                {"b", "backup"},
                {"r", "reset"},
                {"pc", "problemcheck"},
                {"v", "version"},
                {"h", "help"},
                {"s", "setup"}
            };
            argumentsDict = new Dictionary<List<string>, Action<string>>{
                {new List<string>{"-g", "--gamedirectory"}, new Action<string>((extraArg) => { MainClass._gameDirectory = new DirectoryInfo(Path.Combine(extraArg, "game"));
                    MainClass._indexDirectory = new DirectoryInfo(Path.Combine(extraArg, "game", "sqpack", "ffxiv")); })},
                {new List<string>{"-c", "--configdirectory"}, new Action<string>((extraArg) => { MainClass._configDirectory = new DirectoryInfo(extraArg); })},
                {new List<string>{"-b", "--backupdirectory"}, new Action<string>((extraArg) => { MainClass._backupDirectory = new DirectoryInfo(extraArg); })},
                {new List<string>{"-t", "--ttmp"}, new Action<string>((extraArg) => { ttmpPaths.Add(new DirectoryInfo(extraArg)); })},
                {new List<string>{"-w", "--wizard"}, new Action<string>((extraArg) => { useWizard = true; })},
                {new List<string>{"-a", "--all"}, new Action<string>((extraArg) => { importAll = true; })},
                {new List<string>{"-npc", "--noproblemcheck"}, new Action<string>((extraArg) => { skipProblemCheck = true; })},
                {new List<string>{"-v", "--version"}, new Action<string>((extraArg) => { requestedAction = "version"; })},
                {new List<string>{"-o", "--output"}, new Action<string>((extraArg) => { outputFile = new DirectoryInfo(extraArg); })},
                {new List<string>{"-h", "--help"}, new Action<string>((extraArg) => { requestedAction = "help"; })}
            };
        }

        public void ReadArguments(string[] args)
        {
            if (fullActions.ContainsKey(args[0]))
            {
                if (args.Length > 1 && fullActions[args[0]].Keys.Contains(args[1]))
                {
                    requestedAction = $"{args[0]} {args[1]}";
                    args = args.Skip(2).ToArray();
                }
                else
                {
                    requestedAction = args[0];
                    args = args.Skip(1).ToArray();
                }
            }
            else if (actionAliases.ContainsKey(args[0]))
            {
                requestedAction = actionAliases[args[0]];
                args = args.Skip(1).ToArray();
            }
            else
                requestedAction = null;
            ProcessArguments(args);
            // Execute this last, after all the arguments are dealt with
            if (string.IsNullOrEmpty(requestedAction))
                main.PrintMessage($"{args[0]} is not a valid action", 2);
            if (ActionRequirementsChecker(requestedAction))
            {
                string[] requestedActionSplit = requestedAction.Split(' ');
                if (requestedActionSplit.Length > 1)
                    fullActions[requestedActionSplit[0]][requestedActionSplit[1]]();
                else
                    fullActions[requestedActionSplit[0]][""]();
            }
        }

        void ProcessArguments(string[] args)
        {
            List<string> requiresPair = new List<string>{ "-t", "--ttmp", "-g", "--gamedirectory", "-b", "--backupdirectory", "-c", "--configdirectory", "-o", "--output" };
            foreach (var (cmdArg, cmdIndex) in args.Select((value, i) => (value, i)))
            {
                if (cmdArg.StartsWith("-"))
                {
                    string nextArg;
                    //The argument parsed needs a pair. Ex: "-t ttmp.ttmp"
                    if (requiresPair.Contains(cmdArg))
                    {
                        //To be removed: Deprecation warning!
                        if (new List<string>{ "-t", "--ttmp" }.Contains(cmdArg))
                            main.PrintMessage("-t and --ttmp will be deprecated and replaced with free-standing paths. Ex: ffmt mpi path/to/modpack.ttmp", 3);
                        if (cmdIndex < args.Length - 1)
                            nextArg = args[cmdIndex+1];
                        else
                            nextArg = null;
                        if (string.IsNullOrEmpty(nextArg) || nextArg.StartsWith("-"))
                            main.PrintMessage($"{cmdArg} is missing an argument", 2);
                    }
                    else
                        nextArg = null;
                    foreach(List<string> argumentList in argumentsDict.Keys)
                    {
                        if (argumentList.Contains(cmdArg))
                        {
                            argumentsDict[argumentList](nextArg);
                            break;
                        }
                    } 
                }
                //The statement isn't first in the argument list and isn't required by a previous argument
                //Or it is the first... Assume these are TTMP files
                else if ((cmdIndex == 0 || (cmdIndex > 0 && !requiresPair.Contains(args[cmdIndex-1]))))
                {
                    ttmpPaths.Add(new DirectoryInfo(cmdArg));
                }
            }
        }

        public bool ActionRequirementsChecker(string requestedAction)
        {
            List<string> requiresGameDirectory = new List<string> { "modpack import", "modpack create", "mods refresh", "mods enable", "mods disable", "backup", "reset", "problemcheck" };
            List<string> requiresBackupDirectory = new List<string> { "modpack import", "mods refresh", "mods enable", "mods disable", "backup", "reset", "problemcheck" };
            List<string> requiresConfigDirectory = new List<string> { "modpack import", "problemcheck" };
            List<string> requiresUpdatedBackups = new List<string> { "modpack import", "mods refresh", "mods enable", "mods disable", "reset" };
            List<string> requiresValidIndexes = new List<string> { "modpack import", "backup" };
            List<string> requiresTTMPFile = new List<string> { "modpack import", "modpack info" };

            if (requiresGameDirectory.Contains(requestedAction))
            {
                if (!CheckGameDirectory())
                    return false;
            }
            if (requiresBackupDirectory.Contains(requestedAction))
            {
                if (!CheckBackupDirectory())
                    return false;
            }
            if (requiresConfigDirectory.Contains(requestedAction))
            {
                if (!CheckConfigDirectory())
                    return false;
            }
            if (requiresUpdatedBackups.Contains(requestedAction))
            {
                if (!validation.ValidateBackups())
                    return false;
            }
            if (requiresValidIndexes.Contains(requestedAction))
            {
                if (!validation.ValidateIndexFiles())
                    return false;
            }
            if (requiresTTMPFile.Contains(requestedAction))
            {
                if (!CheckTTMPFile())
                    return false;
            }
            return true;
        }

        bool CheckGameDirectory()
        {
            if (MainClass._indexDirectory == null)
            {
                string configGameDirectory = config.ReadConfig("GameDirectory");
                MainClass._gameDirectory = new DirectoryInfo(Path.Combine(configGameDirectory, "game"));
                MainClass._indexDirectory = new DirectoryInfo(Path.Combine(configGameDirectory, "game", "sqpack", "ffxiv"));
            }
            if (MainClass._indexDirectory == null || !validation.ValidateDirectory(MainClass._indexDirectory, "GameDirectory"))
            {
                main.PrintMessage("Invalid game directory", 2);
                return false;
            }
            if (!validation.ValidateCache())
            {
                File.Delete(Path.Combine(MainClass._gameDirectory.FullName, "mod_cache.db"));
                File.Delete(Path.Combine(MainClass._gameDirectory.FullName, "item_sets.db"));
            }
            XivCache.SetGameInfo(MainClass._indexDirectory, XivLanguage.English);
            XivCache.CacheWorkerEnabled = false;
            return true;
        }

        bool CheckBackupDirectory()
        {
            if (MainClass._backupDirectory == null)
                MainClass._backupDirectory = new DirectoryInfo(config.ReadConfig("BackupDirectory"));
            if (MainClass._backupDirectory == null || !validation.ValidateDirectory(MainClass._backupDirectory, "BackupDirectory"))
            {
                main.PrintMessage("Invalid backup directory", 2);
                return false;
            }
            return true;
        }

        bool CheckConfigDirectory()
        {
            if (MainClass._configDirectory == null)
                MainClass._configDirectory = new DirectoryInfo(config.ReadConfig("ConfigDirectory"));
            if (MainClass._configDirectory == null || !validation.ValidateDirectory(MainClass._configDirectory, "ConfigDirectory"))
            {
                main.PrintMessage("Invalid game config directory", 2);
                return false;
            }
            return true;
        }

        bool CheckTTMPFile()
        {
            if (!ttmpPaths.Any())
            {
                main.PrintMessage("Can't import without a modpack to import. At least 1 must be specificed. Ex: path/to/modpack.ttmp", 2);
                return false;
            }
            foreach (DirectoryInfo ttmp in ttmpPaths)
            {
                if (!validation.ValidateTTMPFile(ttmp.FullName))
                {
                    main.PrintMessage($"{ttmp.FullName} is an invalid ttmp file", 2);
                    return false;
                }
            }
            return true;
        }

        public void SendHelpText()
        {
            string helpText = $@"Usage: {Assembly.GetEntryAssembly().GetName().Name} [action] {"{arguments}"}

Available actions:
  modpack import, mpi      Import a modpack, requires a .ttmp(2) to be specified
  modpack info, mpinfo     Show info about a modpack, requires a .ttmp(2) to be specified
  modpack create, mpc      Create a modpack out of your currently active mods
  mods enable, me          Enable all installed mods
  mods disable, md         Disable all installed mods
  mods refresh, mr         Enable/disable mods as specified in modlist.cfg
  backup, b                Backup clean index files for use in resetting the game
  reset, r                 Reset game to clean state
  problemcheck, pc         Check if there are any problems with the game, mod or backup files
  version, v               Display current application and game version
  help, h                  Display this text
  setup, s                 Run First-time Setup Wizard

Available arguments:
  -g, --gamedirectory      Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
  -c, --configdirectory    Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
  -b, --backupdirectory    Full path to directory with your index backups
  -t, --ttmp               Will be deprecated - Full path to .ttmp(2) file (modpack import/info only)
  -w, --wizard             Use the modpack wizard to select what mods to import (modpack import only)
  -a, --all                Import all mods in a modpack immediately (modpack import only)
  -npc, --noproblemcheck   Skip the problem check after importing a modpack
  -o, --output             Path and filename to save .ttmp2 during Modpack Creation
  -v, --version            Display current application and game version
  -h, --help               Display this text
  path/to/modpack.ttmp     Full path to modpack(s). Imports in the order given.";
            main.PrintMessage(helpText);
        }
    }
}
