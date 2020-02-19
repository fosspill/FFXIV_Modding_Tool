using System;
using System.IO;
using Newtonsoft.Json;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Mods.DataContainers;

namespace FFXIV_Modding_Tool.Validation
{
    public class Validators
    {
        public Validators(){}
        MainClass main = new MainClass();

        public bool ValidateDirectory(DirectoryInfo directory, string directoryType)
        {
            if (!directory.Exists)
                return false;
            else
            {
                switch (directoryType)
                {
                    case "BackupDirectory":
                        return true;
                    case "GameDirectory":
                        if (directory.GetFiles("*.index").Length == 0)
                            return false;
                        return true;
                    case "ConfigDirectory":
                        if (directory.GetFiles("FFXIV*.cfg").Length == 0)
                            return false;
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool ValidateTTMPFile(string ttmpPath)
        {
            if (File.Exists(ttmpPath) && (ttmpPath.EndsWith(".ttmp") || ttmpPath.EndsWith(".ttmp2")))
                return true;
            return false;
        }

        public bool ValidateBackups()
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
                    var outdatedBackupsCheck = problemChecker.CheckForOutdatedBackups(file, MainClass._backupDirectory);
                    outdatedBackupsCheck.Wait();
                    if (!outdatedBackupsCheck.Result)
                    {
                        main.PrintMessage($"One or more index files are out of date in {MainClass._backupDirectory.FullName}. Recreating or downloading them from the TexTools discord is recommended", 2);
                        problemFound = true;
                        break;
                    }
                }
            }
            if (problemFound){
                if (PromptContinuation("Would you like to back up now?", true)){
                    main.BackupIndexes();
                    keepGoing = true;
                } else {
                    keepGoing = PromptContinuation();
                }
            }
            else
                main.PrintMessage("All backups present and up to date", 1);
            return keepGoing;
        }

        public bool ValidateIndexFiles()
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
                        var datCountCheck = problemChecker.CheckIndexDatCounts(file);
                        datCountCheck.Wait();
                        if (datCountCheck.Result)
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
                    string unknownSource = "";
                    foreach (Mod mod in modData.Mods)
                    {
                        if (mod.source != "FFXIV_Modding_Tool" && mod.source != "FilesAddedByTexTools")
                        {
                            unknownSource = mod.source;
                            unsupportedSource = true;
                            break;
                        }
                    }
                    if (unsupportedSource)
                    {
                        main.PrintMessage($"Found a mod applied by an unknown application, game stability cannot be guaranteed: {unknownSource}", 3);
                        keepGoing = PromptContinuation();
                    }
                }
            }
            return keepGoing;
        }
        
        bool PromptContinuationReply(string answer, bool defaultanswer = false){
              switch (answer.ToLower())
              {
                  case "y":
                      return true;
                      break;
                  case "n":
                      return false;
                      break;
                  case "\n":
                      return defaultanswer;
                  default:
                      return false;
                      break;
              }
        }

        bool PromptContinuation(string message = "Would you like to continue?", bool defaultanswer = false)
        {
            string choicestring;
            if (!defaultanswer)
                choicestring = "y/N";
            else if (defaultanswer)
                choicestring = "Y/n";
            else
                choicestring = "y/n";
            
            main.PrintMessage($"{message} {choicestring}", 1);
            string answer = Console.ReadKey().KeyChar.ToString().ToLower();
            Console.Write("\n");
            return PromptContinuationReply(answer, defaultanswer);
            
        }
    }
}
