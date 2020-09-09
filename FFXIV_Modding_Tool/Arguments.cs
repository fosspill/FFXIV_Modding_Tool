using System;
using System.IO;
using System.Linq;
using System.Reflection;
using xivModdingFramework.Mods;
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
        string ttmpPath = "";
        bool useWizard = false;
        bool importAll = false;
        bool skipProblemCheck = false;
        Dictionary<List<string>, Action> actionDict = new Dictionary<List<string>, Action>();
        Dictionary<List<string>, Action<string>> argumentDict = new Dictionary<List<string>, Action<string>>();

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
            print(args);
        }

        public void SetupDicts()
        {
            actionDict = new Dictionary<List<string>, Action>{
                {new List<string>{"mpi", "modpack import"}, new Action(() => { 
                    if (useWizard && importAll)
                        {
                            main.PrintMessage("You can't use the import wizard and skip the wizard at the same time", 3);
                            useWizard = false;
                            importAll = false;
                        }
                        main.ImportModpackHandler(new DirectoryInfo(ttmpPath), useWizard, importAll, skipProblemCheck); })},
                {new List<string>{"mpinfo", "modpack info"}, new Action(() => { Dictionary<string, string> modpackInfo = main.GetModpackInfo(new DirectoryInfo(ttmpPath));
                        main.PrintMessage($@"Name: {modpackInfo["name"]}
Type: {modpackInfo["type"]}
Author: {modpackInfo["author"]}
Version: {modpackInfo["version"]}
Description: {modpackInfo["description"]}
Number of mods: {modpackInfo["modAmount"]}"); })},
                {new List<string>{"mr", "mods refresh"}, new Action(() => { main.SetModActiveStates(); })},
                {new List<string>{"me", "mods enable"}, new Action(() => { main.ToggleModStates(true); })},
                {new List<string>{"md", "mods disable"}, new Action(() => { main.ToggleModStates(false); })},
                {new List<string>{"b", "backup"}, new Action(() => { main.BackupIndexes(); })},
                {new List<string>{"r", "reset"}, new Action(() => { main.ResetMods(); })},
                {new List<string>{"pc", "problemcheck"}, new Action(() => { main.ProblemChecker(); })},
                {new List<string>{"v", "version"}, new Action(() => { if (MainClass._gameDirectory == null)
                    MainClass._gameDirectory = new DirectoryInfo(Path.Combine(config.ReadConfig("GameDirectory"), "game"));
                    main.CheckVersions(); })},
                {new List<string>{"h", "help"}, new Action(() => { SendHelpText(); })},
                {new List<string>{"s", "setup"}, new Action(() => { setup.ExecuteSetup(); })}
            };
            argumentDict = new Dictionary<List<string>, Action<string>>{
                {new List<string>{"g", "gamedirectory"}, new Action<string>((extraArg) => { MainClass._gameDirectory = new DirectoryInfo(Path.Combine(extraArg, "game"));
                    MainClass._indexDirectory = new DirectoryInfo(Path.Combine(extraArg, "game", "sqpack", "ffxiv")); })},
                {new List<string>{"c", "configdirectory"}, new Action<string>((extraArg) => { MainClass._configDirectory = new DirectoryInfo(extraArg); })},
                {new List<string>{"b", "backupdirectory"}, new Action<string>((extraArg) => { MainClass._backupDirectory = new DirectoryInfo(extraArg); })},
                {new List<string>{"t", "ttmp"}, new Action<string>((extraArg) => { ttmpPath = extraArg; })},
                {new List<string>{"w", "wizard"}, new Action<string>((extraArg) => { useWizard = true; })},
                {new List<string>{"a", "all"}, new Action<string>((extraArg) => { importAll = true; })},
                {new List<string>{"npc", "noproblemcheck"}, new Action<string>((extraArg) => { skipProblemCheck = true; })},
                {new List<string>{"v", "version"}, new Action<string>((extraArg) => { if (MainClass._gameDirectory == null)
                    MainClass._gameDirectory = new DirectoryInfo(Path.Combine(config.ReadConfig("GameDirectory"), "game"));
                    main.CheckVersions(); })},
                {new List<string>{"h", "help"}, new Action<string>((extraArg) => { SendHelpText(); })}
            };
        }

        public void ReadArguments(string[] args)
        {
            List<string> requiresPair = new List<string> { "-t", "--ttmp"};
            foreach (var (cmdArg, cmdIndex) in args.Select((value, i) => (value, i)))
            {
                if (cmdArg.StartsWith("-"))
                {
                    string nextArg
                    if (requiresPair.Contains(cmdArg))
                    {
                        if ( cmdIndex < args.Length - 1 )
                            nextArg = args[cmdIndex+1]
                        else
                            nextArg = None
                    }
                    //ToDo: Rewrite this part. Make sure arguments are added to a dict for later consumption
                    string arg = cmdArg.Split('-').Last();
                    foreach(List<string> argumentList in argumentDict.Keys)
                    {
                        if (argumentList.Contains(arg))
                            argumentDict[-ttmp](mymodpack.ttmp);
                    }
                    args.remove(cmdArg)
                }
            }
            string fullAction = "";
            if (args.Count() > 1)
                fullAction = $"{args[0]} {args[1]}";
                args.remove(args[1])
            foreach(List<string> actionList in actionDict.Keys)
                {
                    if (actionList.Contains(args[0]) || actionList.Contains(fullAction))
                    {
                        if (ActionRequirementsChecker(actionList[0]))
                            actionDict[actionList]();
                    }
                }
            args.remove(args[0])
        }

        public bool ActionRequirementsChecker(string requestedAction)
        {
            List<string> requiresGameDirectory = new List<string> { "mpi", "mr", "me", "md", "b", "r", "pc" };
            List<string> requiresBackupDirectory = new List<string> { "mpi", "mr", "me", "md", "b", "r", "pc" };
            List<string> requiresConfigDirectory = new List<string> { "mpi", "pc" };
            List<string> requiresUpdatedBackups = new List<string> { "mpi", "mr", "me", "md", "r" };
            List<string> requiresValidIndexes = new List<string> { "mpi", "b" };
            List<string> requiresTTMPFile = new List<string> { "mpi", "mpinfo" };

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
            if (string.IsNullOrEmpty(ttmpPath))
            {
                main.PrintMessage("Can't import without a modpack to import. Specify one with -t", 2);
                return false;
            }
            if (!validation.ValidateTTMPFile(ttmpPath))
            {
                main.PrintMessage("Invalid ttmp file", 2);
                return false;
            }
            return true;
        }

        public void SendHelpText()
        {
            string helpText = $@"Usage: {Assembly.GetEntryAssembly().GetName().Name} [action] {"{arguments}"}

Available actions:
  modpack import, mpi      Import a modpack, requires a .ttmp(2) to be specified
  modpack info, mpinfo     Show info about a modpack, requires a .ttmp(2) to be specified
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
  -t, --ttmp               Full path to .ttmp(2) file (modpack import/info only)
  -w, --wizard             Use the modpack wizard to select what mods to import (modpack import only)
  -a, --all                Import all mods in a modpack immediately (modpack import only)
  -npc, --noproblemcheck   Skip the problem check after importing a modpack
  -v, --version            Display current application and game version
  -h, --help               Display this text";
            main.PrintMessage(helpText);
        }
    }
}
