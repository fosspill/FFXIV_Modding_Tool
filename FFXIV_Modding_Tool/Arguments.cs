﻿using System;
using System.IO;
using System.Linq;
using xivModdingFramework.Mods;
using System.Collections.Generic;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Validation;

namespace FFXIV_Modding_Tool.Commandline
{
    public class Arguments
    {
        public Arguments(){}
        MainClass main = new MainClass();
        Config config = new Config();
        Validators validation = new Validators();
        string ttmpPath = "";
        bool useWizard = false;
        bool importAll = false;
        bool skipProblemCheck = false;
        string requestedAction = "";
        string wantedItem = "";

        public void ArgumentHandler(string[] args)
        {
            if (!File.Exists(Config.configFile) || string.IsNullOrEmpty(File.ReadAllText(Config.configFile)))
                config.CreateDefaultConfig();
            if (args.Length == 0)
            {
                SendHelpText();
                return;
            }
            ReadArguments(args);
            ReadAction(args);
            if (ActionRequirementsChecker())
                ActionHandler();
        }

        public void ReadArguments(string[] args)
        { 
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
                        case "h":
                        case "help":
                            requestedAction = "h";
                            continue;
                        case "v":
                        case "version":
                            requestedAction = "v";
                            continue;
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
                        case "n":
                        case "name":
                            if (!nextArg.StartsWith("-"))
                                wantedItem = nextArg;
                            continue;
                        case "w":
                        case "wizard":
                            useWizard = true;
                            continue;
                        case "a":
                        case "all":
                            importAll = true;
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
        }

        public void ReadAction(string[] args)
        { 
            string secondAction = "";
            if (args.Count() > 1)
                secondAction = args[1];
            if (string.IsNullOrEmpty(requestedAction))
            {
                switch (args[0])
                {
                    case "mpi":
                        requestedAction = "mpi";
                        break;
                    case "mpinfo":
                        requestedAction = "mpinfo";
                        break;
                    case "modpack":
                        if (secondAction == "import")
                            goto case "mpi";
                        if (secondAction == "info")
                            goto case "mpinfo";
                        break;
                    case "mr":
                        requestedAction = "mr";
                        break;
                    case "me":
                        requestedAction = "me";
                        break;
                    case "md":
                        requestedAction = "md";
                        break;
                    case "mex":
                        requestedAction = "mex";
                        break;
                    case "mi":
                        requestedAction = "mi";
                        break;
                    case "mods":
                        if (secondAction == "refresh")
                            goto case "mr";
                        if (secondAction == "enable")
                            goto case "me";
                        if (secondAction == "disable")
                            goto case "md";
                        break;
                    case "mod":
                        if (secondAction == "export")
                            goto case "mex";
                        if (secondAction == "import")
                            goto case "mi";
                        break;
                    case "backup":
                    case "b":
                        requestedAction = "b";
                        break;
                    case "reset":
                    case "r":
                        requestedAction = "r";
                        break;
                    case "problemcheck":
                    case "pc":
                        requestedAction = "pc";
                        break;
                    case "version":
                    case "v":
                        requestedAction = "v";
                        break;
                    case "help":
                    case "h":
                        requestedAction = "h";
                        break;
                    default:
                        main.PrintMessage($"Unknown action: {args[0]}");
                        requestedAction = "h";
                        break;
                }
            }
        }

        public bool ActionRequirementsChecker()
        {
            List<string> requiresGameDirectory = new List<string> { "mpi", "mr", "me", "md", "mex", "b", "r", "pc" };
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

        public void ActionHandler()
        {
            switch (requestedAction)
            {
                case "h":
                    SendHelpText();
                    break;
                case "v":
                    if (MainClass._gameDirectory == null)
                        MainClass._gameDirectory = new DirectoryInfo(Path.Combine(config.ReadConfig("GameDirectory"), "game"));
                    main.CheckVersions();
                    break;
                case "mpi":
                    if (useWizard && importAll)
                    {
                        main.PrintMessage("You can't use the import wizard and skip the wizard at the same time", 3);
                        useWizard = false;
                        importAll = false;
                    }
                    main.ImportModpackHandler(new DirectoryInfo(ttmpPath), useWizard, importAll, skipProblemCheck);
                    break;
                case "mpinfo":
                    Dictionary<string, string> modpackInfo = main.GetModpackInfo(new DirectoryInfo(ttmpPath));
                    main.PrintMessage($@"Name: {modpackInfo["name"]}
Type: {modpackInfo["type"]}
Author: {modpackInfo["author"]}
Version: {modpackInfo["version"]}
Description: {modpackInfo["description"]}
Number of mods: {modpackInfo["modAmount"]}");
                    break;
                case "mr":
                    main.SetModActiveStates();
                    break;
                case "me":
                    main.ToggleModStates(true);
                    break;
                case "md":
                    main.ToggleModStates(false);
                    break;
                case "mex":
                    main.ExportRequestHandler(wantedItem);
                    break;
                case "mi":
                case "b":
                    main.BackupIndexes();
                    break;
                case "r":
                    main.ResetMods();
                    break;
                case "pc":
                    main.ProblemChecker();
                    break;
                default:
                    SendHelpText();
                    break;
            }
        }

        public void SendHelpText()
        {
            string helpText = $@"Usage: {Path.GetFileName(Environment.GetCommandLineArgs()[0])} [action] {"{arguments}"}

Available actions:
  modpack import, mpi      Import a modpack, requires a .ttmp(2) to be specified
  modpack info, mpinfo     Show info about a modpack, requires a .ttmp(2) to be specified
  mods enable, me          Enable all installed mods
  mods disable, md         Disable all installed mods
  mods refresh, mr         Enable/disable mods as specified in modlist.cfg
  mod export, mex          Export the textures and/or model of an item
  backup, b                Backup clean index files for use in resetting the game
  reset, r                 Reset game to clean state
  problemcheck, pc         Check if there are any problems with the game, mod or backup files
  version, v               Display current application and game version
  help, h                  Display this text

Available arguments:
  -g, --gamedirectory      Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
  -c, --configdirectory    Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
  -b, --backupdirectory    Full path to directory with your index backups
  -t, --ttmp               Full path to .ttmp(2) file (modpack import/info only)
  -n, --name               Name of item to import/export (mod import/export only)
  -w, --wizard             Use the modpack wizard to select what mods to import (modpack import only)
  -a, --all                Import all mods in a modpack immediately (modpack import only)
  -npc, --noproblemcheck   Skip the problem check after importing a modpack
  -v, --version            Display current application and game version
  -h, --help               Display this text";
            main.PrintMessage(helpText);
        }
    }
}
