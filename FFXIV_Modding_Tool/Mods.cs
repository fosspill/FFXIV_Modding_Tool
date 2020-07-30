using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.Enums;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Mods.FileTypes;
using xivModdingFramework.Helpers;
using xivModdingFramework.SqPack.FileTypes;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace FFXIV_Modding_Tool.ModControl
{
    public class Mods
    {
        public Mods(){}
        public static DirectoryInfo _modlistPath = new DirectoryInfo(Path.Combine(MainClass._gameDirectory.FullName, "XivMods.json"));
        MainClass main = new MainClass();

        public class ModStatus
        {
            public string modpack { get; set; }
            public string name { get; set; }
            public string category { get; set; }
            public string type { get; set; }
            public string fullpath { get; set; }
            public bool enabled { get; set; }
            private Dictionary<int, string> dataTypes = new Dictionary<int, string>{[3] = "Model", [4] = "Texture"};

            public ModStatus() { }

            public ModStatus(Mod mod)
            {
                modpack = mod.modPack.name;
                name = mod.name;
                category = mod.category;
                type = dataTypes[mod.data.dataType];
                fullpath = mod.fullPath;
                enabled = mod.enabled;
            }
        }
        public ModStatus modStatus;

        public void GetModpackInfo(DirectoryInfo ttmpPath)
        {
            Dictionary<string, string> modpackInfo = new Dictionary<string, string>
            {
                ["name"] = Path.GetFileNameWithoutExtension(ttmpPath.FullName),
                ["type"] = "Simple",
                ["author"] = "N/A",
                ["version"] = "N/A",
                ["description"] = "N/A",
                ["modAmount"] = "0"
            };
            if (ttmpPath.Extension == ".ttmp2")
            {
                var ttmp = new TTMP(ttmpPath, "FFXIV_Modding_Tool");
                var ttmpData = ttmp.GetModPackJsonData(ttmpPath);
                ttmpData.Wait();
                var ttmpInfo = ttmpData.Result.ModPackJson;
                modpackInfo["name"] = ttmpInfo.Name;
                if (ttmpInfo.TTMPVersion.Contains("w"))
                {
                    modpackInfo["type"] = "Wizard";
                    modpackInfo["description"] = ttmpInfo.Description;
                    int modCount = 0;
                    foreach (var page in ttmpInfo.ModPackPages)
                    {
                        foreach (var group in page.ModGroups)
                            modCount += group.OptionList.Count;
                    }
                    modpackInfo["modAmount"] = modCount.ToString();
                }
                else
                    modpackInfo["modAmount"] = ttmpInfo.SimpleModsList.Count.ToString();
                modpackInfo["author"] = ttmpInfo.Author;
                modpackInfo["version"] = ttmpInfo.Version;
            }
            else if (ttmpPath.Extension == ".ttmp")
                modpackInfo["modAmount"] = GetOldModpackJson(ttmpPath).Count.ToString();
            main.PrintMessage($@"Name: {modpackInfo["name"]}
Type: {modpackInfo["type"]}
Author: {modpackInfo["author"]}
Version: {modpackInfo["version"]}
Description: {modpackInfo["description"]}
Number of mods: {modpackInfo["modAmount"]}");;
        }

        public void ImportModpackHandler(DirectoryInfo ttmpPath, bool useWizard, bool importAll, bool skipProblemCheck)
        {
            var importError = false;
            try
            {
                if (main.IndexLocked())
                {
                    main.PrintMessage("Unable to import while the game is running.", 2);
                    return;
                }
            }
            catch (Exception ex)
            {
                main.PrintMessage($"Problem reading index files:\n{ex.Message}", 2);
            }
            main.PrintMessage("Starting import...");
            try
            {
                var ttmp = new TTMP(ttmpPath, "FFXIV_Modding_Tool");

                try
                {
                    if (ttmpPath.Extension == ".ttmp2")
                    {
                        var ttmpData = ttmp.GetModPackJsonData(ttmpPath);
                        ttmpData.Wait();
                        ModpackDataHandler(ttmpPath, ttmpData.Result.ModPackJson, useWizard, importAll);
                    }
                    else
                        ModpackDataHandler(ttmpPath, null, useWizard, importAll);
                }
                catch
                {
                    importError = true;
                }
            }
            catch (Exception ex)
            {
                if (!importError)
                {
                    main.PrintMessage($"Exception was thrown:\n{ex.Message}\nRetrying import...", 3);
                    ModpackDataHandler(ttmpPath, null, useWizard, importAll);
                }
                else
                {
                    main.PrintMessage($"There was an error importing the modpack at {ttmpPath.FullName}\nMessage: {ex.Message}", 2);
                    return;
                }
            }
            if (!skipProblemCheck)
                main.ProblemChecker();
            return;
        }

        void ModpackDataHandler(DirectoryInfo ttmpPath, ModPackJson ttmpData, bool useWizard, bool importAll)
        {
            var modding = new Modding(MainClass._indexDirectory);
            string ttmpName = null;
            List<SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            TTMP _textoolsModpack = new TTMP(ttmpPath, "FFXIV_Modding_Tool");
            main.PrintMessage($"Extracting data from {ttmpPath.Name}...");
            if (ttmpData != null)
            {
                ttmpName = ttmpData.Name;
                if (ttmpData.TTMPVersion.Contains("w"))
                {
                    main.PrintMessage("Starting wizard...");
                    ttmpDataList = TTMP2DataList(WizardDataHandler(ttmpData), ttmpData, false, true);
                }
                else
                    ttmpDataList = TTMP2DataList(ttmpData.SimpleModsList, ttmpData, useWizard, importAll);
            }
            else
            {
                ttmpName = Path.GetFileNameWithoutExtension(ttmpPath.FullName);
                ttmpDataList = TTMPDataList(ttmpPath, useWizard, importAll);
            }
            ttmpDataList.Sort();
            main.PrintMessage("Data extraction successful.");
            int originalModCount = ttmpDataList.Count;
            main.PrintMessage($"Importing {ttmpDataList.Count}/{originalModCount} mods from modpack...");
            ImportModpack(ttmpDataList, _textoolsModpack, ttmpPath);
        }

        List<SimpleModPackEntries> TTMP2DataList(List<ModsJson> modsJsons, ModPackJson ttmpData, bool useWizard, bool importAll)
        {
            List <SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            foreach (var modsJson in modsJsons)
            {
                var race = main.GetRace(modsJson.FullPath);
                var number = main.GetNumber(modsJson.FullPath);
                var type = main.GetType(modsJson.FullPath);
                var part = main.GetPart(modsJson.FullPath);
                var map = main.GetMap(modsJson.FullPath);

                var active = false;
                var isActive = XivModStatus.Disabled;

                if (isActive == XivModStatus.Enabled)
                    active = true;

                modsJson.ModPackEntry = new ModPack
                { name = ttmpData.Name, author = ttmpData.Author, version = ttmpData.Version };

                ttmpDataList.Add(new SimpleModPackEntries
                {
                    Name = modsJson.Name,
                    Category = modsJson.Category,
                    Race = race.ToString(),
                    Part = part,
                    Type = type,
                    Num = number,
                    Map = map,
                    Active = active,
                    JsonEntry = modsJson,
                });
            }
            if (!useWizard && !importAll)
                useWizard = PromptWizardUsage(ttmpDataList.Count);
            if (useWizard)
            {
                main.PrintMessage($"\nName: {ttmpData.Name}\nVersion: {ttmpData.Version}\nAuthor: {ttmpData.Author}\n");
                return SimpleDataHandler(ttmpDataList);
            }
            return ttmpDataList;
        }

        List<SimpleModPackEntries> TTMPDataList(DirectoryInfo ttmpPath, bool useWizard, bool importAll)
        {
            List<SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            var originalModPackData = GetOldModpackJson(ttmpPath);
            string ttmpName = Path.GetFileNameWithoutExtension(ttmpPath.FullName);

            foreach (var modsJson in originalModPackData)
            {
                var race = main.GetRace(modsJson.FullPath);
                var number = main.GetNumber(modsJson.FullPath);
                var type = main.GetType(modsJson.FullPath);
                var part = main.GetPart(modsJson.FullPath);
                var map = main.GetMap(modsJson.FullPath);

                var active = false;
                var isActive = XivModStatus.Disabled;

                if (isActive == XivModStatus.Enabled)
                    active = true;

                ttmpDataList.Add(new SimpleModPackEntries
                {
                    Name = modsJson.Name,
                    Category = modsJson.Category,
                    Race = race.ToString(),
                    Part = part,
                    Type = type,
                    Num = number,
                    Map = map,
                    Active = active,
                    JsonEntry = new ModsJson
                    {
                        Name = modsJson.Name,
                        Category = modsJson.Category,
                        FullPath = modsJson.FullPath,
                        DatFile = modsJson.DatFile,
                        ModOffset = modsJson.ModOffset,
                        ModSize = modsJson.ModSize,
                        ModPackEntry = new ModPack { name = ttmpName, author = "N/A", version = "1.0.0" }
                    }
                });
            }
            if (!useWizard && !importAll)
                useWizard = PromptWizardUsage(ttmpDataList.Count);
            if (useWizard)
            {
                main.PrintMessage($"\nName: {ttmpName}\nVersion: N/A\nAuthor: N/A\n");
                return SimpleDataHandler(ttmpDataList);
            }
            return ttmpDataList;
        }

        List<OriginalModPackJson> GetOldModpackJson(DirectoryInfo ttmpPath)
        {
            List<OriginalModPackJson> originalModPackData = new List<OriginalModPackJson>();
            var fs = new FileStream(ttmpPath.FullName, FileMode.Open, FileAccess.Read);
            ZipFile archive = new ZipFile(fs);
            ZipEntry mplFile = archive.GetEntry("TTMPL.mpl");
            {
                using (var streamReader = new StreamReader(archive.GetInputStream(mplFile)))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (!line.ToLower().Contains("version"))
                            originalModPackData.Add(JsonConvert.DeserializeObject<OriginalModPackJson>(line));
                    }
                }
            }
            return originalModPackData;
        }

        bool PromptWizardUsage(int modCount)
        {
            bool userPicked = false;
            bool answer = false;
            
            if (modCount > 250)
                main.PrintMessage($"This modpack contains {modCount} mods, using the wizard could be a tedious process", 3);

            while (!userPicked && modCount > 1)
            {
                main.PrintMessage($"Would you like to use the Wizard for importing?\n(Y)es, let me select the mods\n(N)o, import everything");
                string reply = Console.ReadKey().KeyChar.ToString().ToLower();
                if (reply == "y")
                {
                    answer = true;
                    userPicked = true;
                }
                else if (reply == "n")
                {
                    answer = false;
                    userPicked = true;
                }
                main.PrintMessage("\n");
            }
            return answer;
        }

        List<ModsJson> WizardDataHandler(ModPackJson ttmpData)
        {
            List<ModsJson> modpackData = new List<ModsJson>();
            main.PrintMessage($"\nName: {ttmpData.Name}\nVersion: {ttmpData.Version}\nAuthor: {ttmpData.Author}\n{ttmpData.Description}\n");
            foreach (var page in ttmpData.ModPackPages)
            {
                if (ttmpData.ModPackPages.Count > 1)
                    main.PrintMessage($"Page {page.PageIndex}");
                foreach (var option in page.ModGroups)
                {
                    bool userDone = false;
                    while (!userDone)
                    {
                        main.PrintMessage($"{option.GroupName}\nChoices:");
                        List<string> choices = new List<string>();
                        foreach (var choice in option.OptionList)
                        {
                            string description = "";
                            if (!string.IsNullOrEmpty(choice.Description))
                                description = $"\n\t{choice.Description}";
                            choices.Add($"    {option.OptionList.IndexOf(choice)} - {choice.Name}{description}");
                        }
                        main.PrintMessage(string.Join("\n", choices));
                        int maxChoices = option.OptionList.Count;
                        if (option.SelectionType == "Multi")
                        {
                            Console.Write("Choose none, one or multiple (eg: 1 2 3, 0-3, *): ");
                            List<int> wantedIndexes = WizardUserInputValidation(Console.ReadLine(), maxChoices, true);
                            List<string> pickedChoices = new List<string>();
                            foreach (int index in wantedIndexes)
                                pickedChoices.Add($"{index} - {option.OptionList[index].Name}");
                            if (!pickedChoices.Any())
                                pickedChoices.Add("nothing");
                            Console.Write($"\nYou picked:\n{string.Join("\n", pickedChoices)}\nIs this correct? Y/n ");
                            string answer = Console.ReadKey().KeyChar.ToString();
                            if (answer == "y" || answer == "\n")
                            {
                                foreach (int index in wantedIndexes)
                                    modpackData.AddRange(option.OptionList[index].ModsJsons);
                                userDone = true;
                            }
                        }
                        if (option.SelectionType == "Single")
                        {
                            Console.Write("Choose one (eg: 0 1 2 3): ");
                            int wantedIndex = WizardUserInputValidation(Console.ReadLine(), maxChoices, false)[0];
                            Console.Write($"\nYou picked:\n{wantedIndex} - {option.OptionList[wantedIndex].Name}\nIs this correct? Y/n ");
                            string answer = Console.ReadKey().KeyChar.ToString();
                            if (answer == "y" || answer == "\n")
                            {
                                modpackData.AddRange(option.OptionList[wantedIndex].ModsJsons);
                                userDone = true;
                            }
                        }
                        Console.Write("\n");
                    }
                }
            }
            return modpackData;
        }

        List<SimpleModPackEntries> SimpleDataHandler(List<SimpleModPackEntries> ttmpDataList)
        {
            List<SimpleModPackEntries> desiredMods = new List<SimpleModPackEntries>();
            for (int i = 0; i < ttmpDataList.Count; i = i + 50)
            {
                var items = ttmpDataList.Skip(i).Take(50).ToList();
                if (ttmpDataList.Count > 50)
                    main.PrintMessage($"{i}-{i + items.Count} ({ttmpDataList.Count} total)");
                bool userDone = false;
                while (!userDone)
                {
                    main.PrintMessage("Mods:");
                    List<string> mods = new List<string>();
                    foreach (var item in items)
                        mods.Add($"    {items.IndexOf(item)} - {item.Name}, {item.Map}, {item.Race}");
                    main.PrintMessage(string.Join("\n", mods));
                    Console.Write("Choose mods you wish to import (eg: 1 2 3, 0-3, *): ");
                    List<int> wantedMods = WizardUserInputValidation(Console.ReadLine(), items.Count, true);
                    List<string> pickedMods = new List<string>();
                    foreach (int index in wantedMods)
                        pickedMods.Add(mods[index]);
                    if (!pickedMods.Any())
                        pickedMods.Add("nothing");
                    Console.Write($"\nYou picked:\n{string.Join("\n", pickedMods)}\nIs this correct? Y/n ");
                    string answer = Console.ReadKey().KeyChar.ToString();
                    if (answer == "y" || answer == "\n")
                    {
                        foreach (int index in wantedMods)
                            desiredMods.Add(items[index]);
                        userDone = true;
                    }
                    Console.Write("\n");
                }
            }
            return desiredMods;
        }

        List<int> WizardUserInputValidation(string input, int totalChoices, bool canBeEmpty)
        {
            List<int> desiredIndexes = new List<int>();
            if (!string.IsNullOrEmpty(input))
            {
                string[] answers = input.Split();
                foreach (string answer in answers)
                {
                    if (answer == "*")
                    {
                        desiredIndexes = Enumerable.Range(0, totalChoices).ToList();
                        break;
                    }
                    if (answer.Contains("-"))
                    {
                        try
                        {
                            int[] targets = answer.Split('-').Select(int.Parse).ToArray();
                            desiredIndexes.AddRange(Enumerable.Range(targets[0], targets[1] - targets[0] + 1));
                        }
                        catch
                        {
                            main.PrintMessage($"{answer} is not a valid range of choices", 2);
                        }
                        continue;
                    }
                    int wantedIndex;
                    if (int.TryParse(answer, out wantedIndex))
                    {
                        if (wantedIndex < totalChoices)
                        {
                            if (!desiredIndexes.Contains(wantedIndex))
                                desiredIndexes.Add(wantedIndex);
                        }
                        else
                            main.PrintMessage($"There are only {totalChoices} choices, not {wantedIndex + 1}", 2);
                    }
                    else
                        main.PrintMessage($"{answer} is not a valid choice", 2);
                }
            }
            else
            {
                if (!canBeEmpty)
                    desiredIndexes.Add(0);
            }
            return desiredIndexes;
        }

        void ImportModpack(List<SimpleModPackEntries> ttmpDataList, TTMP _textoolsModpack, DirectoryInfo ttmpPath)
        {
            var importList = (from SimpleModPackEntries selectedItem in ttmpDataList select selectedItem.JsonEntry).ToList();
            int totalModsImported = 0;
            var progressIndicator = new Progress<(int current, int total, string message)>(main.ReportProgress);

            try
            {
                var importResults = _textoolsModpack.ImportModPackAsync(ttmpPath, importList,
                MainClass._indexDirectory, _modlistPath, progressIndicator);
                importResults.Wait();
                if (!string.IsNullOrEmpty(importResults.Result.Errors))
                    main.PrintMessage($"There were errors importing some mods:\n{importResults.Result.Errors}", 2);
                else
                {
                    totalModsImported = ttmpDataList.Count();
                    main.PrintMessage($"\n{totalModsImported} mod(s) successfully imported.", 1);
                }
            }
            catch (Exception ex)
            {
                main.PrintMessage($"There was an error attempting to import mods:\n{ex.Message}", 2);
            }
        }

        public void ToggleModStates(bool enable)
        {
            string modstate = "";
            if (enable)
                modstate = "on";
            else
                modstate = "off";
            var modding = new Modding(MainClass._indexDirectory);
            main.PrintMessage($"Turning {modstate} all mods...");
            try
            {
                var toggle = modding.ToggleAllMods(enable);
                toggle.Wait();
                main.PrintMessage($"Successfully turned {modstate} all mods", 1);
            }
            catch (Exception e)
            {
                main.PrintMessage($"There was an issue turning {modstate} mods:\n{e}", 2);
            }
        }

        public void SetModActiveStates()
        {
            main.PrintMessage("Being reworked");
        }

        public void ResetMods()
        {
            bool allFilesAvailable = true;
            bool indexesUpToDate = true;
            var problemChecker = new ProblemChecker(MainClass._indexDirectory);
            if (!MainClass._backupDirectory.Exists)
            {
                main.PrintMessage($"{MainClass._backupDirectory.FullName} does not exist, please specify an existing directory", 2);
                return;
            }
            foreach (KeyValuePair<string, XivDataFile> indexFile in main.IndexFiles())
            {
                string backupPath = Path.Combine(MainClass._backupDirectory.FullName, indexFile.Key);
                if (!File.Exists(backupPath))
                {
                    main.PrintMessage($"{indexFile.Key} not found, aborting...", 3);
                    allFilesAvailable = false;
                    break;
                }
                var outdatedBackupsCheck = problemChecker.CheckForOutdatedBackups(indexFile.Value, MainClass._backupDirectory);
                outdatedBackupsCheck.Wait();
                if (!outdatedBackupsCheck.Result)
                {
                    main.PrintMessage($"{indexFile.Key} is out of date, aborting...", 3);
                    indexesUpToDate = false;
                    break;
                }
            }
            if (!allFilesAvailable || !indexesUpToDate)
            {
                main.PrintMessage($"{MainClass._backupDirectory.FullName} has missing or outdated index files. You can either\n1. Download them from the TT discord\n2. Run this command again using \"backup\" instead of \"reset\" using a clean install of the game", 2);
                return;
            }
            if (main.IndexLocked())
            {
                main.PrintMessage("Can't reset the game while the game is running", 2);
                return;
            }
            try
            {
                var reset = Task.Run(() =>
                {
                    var modding = new Modding(MainClass._indexDirectory);
                    var dat = new Dat(MainClass._indexDirectory);

                    var backupFiles = Directory.GetFiles(MainClass._backupDirectory.FullName);
                    foreach (var backupFile in backupFiles)
                    {
                        if (backupFile.Contains(".win32.index"))
                            File.Copy(backupFile, $"{MainClass._indexDirectory}/{Path.GetFileName(backupFile)}", true);
                    }

                    // Delete modded dat files
                    foreach (var xivDataFile in (XivDataFile[])Enum.GetValues(typeof(XivDataFile)))
                    {
                        var datFiles = dat.GetModdedDatList(xivDataFile);
                        datFiles.Wait();

                        foreach (var datFile in datFiles.Result)
                            File.Delete(datFile);

                        if (datFiles.Result.Count > 0)
                            problemChecker.RepairIndexDatCounts(xivDataFile);
                    }

                    // Delete mod list
                    File.Delete(_modlistPath.FullName);
                    modding.CreateModlist();
                });
                reset.Wait();
                main.PrintMessage("Reset complete!", 1);
            }
            catch (Exception ex)
            {
                main.PrintMessage($"Something went wrong during the reset process\n{ex.Message}", 2);
            }
        }
    }
}