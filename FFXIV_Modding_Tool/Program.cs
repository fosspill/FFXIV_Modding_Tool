// Copyright © 2019 Ole Erik Brennhagen - All Rights Reserved
// Copyright © 2019 Ivanka Heins - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Helpers;
using xivModdingFramework.Mods.FileTypes;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Textures.Enums;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Commandline;
using FFXIV_Modding_Tool.Validation;
using Newtonsoft.Json;

namespace FFXIV_Modding_Tool
{
    public class MainClass
    {
        public static DirectoryInfo _gameDirectory;
        public static DirectoryInfo _indexDirectory;
        public static DirectoryInfo _backupDirectory;
        public static DirectoryInfo _configDirectory;
        public static DirectoryInfo _modpackDirectory;
        public static DirectoryInfo _projectconfDirectory;
        public static string modActiveConfFile;
        private bool importStarted;

        public class ModActiveStatus
        {
            MainClass main = new MainClass();
            public string modpack { get; set; }
            public string name { get; set; }
            public string map { get; set; }
            public string part { get; set; }
            public string race { get; set; }
            public string file { get; set; }
            public bool enabled { get; set; }

            public ModActiveStatus() { }

            public ModActiveStatus(ModsJson entry)
            {
                modpack = entry.ModPackEntry.name;
                name = entry.Name;
                file = entry.FullPath;
                map = main.GetMap(file);
                part = main.GetType(file);
                race = main.GetRace(file).ToString();
                enabled = true;
            }
        }
        public ModActiveStatus modpackActiveStatus;

        /* Print slightly nicer messages. Can add logging here as well if needed.
         1 = Success message, 2 = Error message, 3 = Warning message 
        */
        public void PrintMessage(string message, int importance = 0)
        {
            Console.ResetColor();
            switch (importance)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Green;
                    goto default;
                case 2:
                    Console.Write("ERROR: ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ResetColor();
                    Environment.Exit(1);
                    break;
                case 3:
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    goto default;
                default:
                    Console.WriteLine(message);
                    Console.ResetColor();
                    break;
            }
        }

        public void CheckVersions()
        {
            string ffxivVersion = "not detected";
            if (_gameDirectory != null)
            {
                var versionFile = Path.Combine(_gameDirectory.FullName, "ffxivgame.ver");
                if (File.Exists(versionFile) && File.ReadAllText(versionFile).Length > 0)
                {
                    var versionData = File.ReadAllLines(versionFile);
                    ffxivVersion = new Version(versionData[0].Substring(0, versionData[0].LastIndexOf("."))).ToString();
                }
            }
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            PrintMessage($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title} {version}\nGame version {ffxivVersion}");
        }

        public void GetModpackInfo(List<DirectoryInfo> ttmpPaths)
        {
            foreach (DirectoryInfo ttmpPath in ttmpPaths)
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
                PrintMessage($@"Name: {modpackInfo["name"]}
Type: {modpackInfo["type"]}
Author: {modpackInfo["author"]}
Version: {modpackInfo["version"]}
Description: {modpackInfo["description"]}
Number of mods: {modpackInfo["modAmount"]}
");
            }
        }

        public bool IndexLocked()
        {
            var index = new xivModdingFramework.SqPack.FileTypes.Index(_indexDirectory);
            bool indexLocked = index.IsIndexLocked(XivDataFile._0A_Exd);
            return indexLocked;
        }

        #region Importing Functions
        public void ImportModpackHandler(List<DirectoryInfo> ttmpPaths, bool useWizard, bool importAll, bool skipProblemCheck)
        {
            try
            {
                if (IndexLocked())
                {
                    PrintMessage("Unable to import while the game is running.", 2);
                    return;
                }
            }
            catch (Exception ex)
            {
                PrintMessage($"Problem reading index files:\n{ex.Message}", 2);
            }
            PrintMessage("Starting import...");
            try
            {
                ModpackDataHandler(ttmpPaths, useWizard, importAll);
            }
            catch (Exception ex)
            {
                PrintMessage($"There was an error importing a modpack.\nMessage: {ex.Message}", 2);
            }
            if (!skipProblemCheck)
                ProblemChecker();
            return;
        }

        void ModpackDataHandler(List<DirectoryInfo> ttmpPaths, bool useWizard, bool importAll)
        {
            Dictionary<DirectoryInfo, List<ModsJson>> ttmpDataLists = new Dictionary<DirectoryInfo, List<ModsJson>>();
            foreach(DirectoryInfo ttmpPath in ttmpPaths)
            {
                var ttmp = new TTMP(ttmpPath, "FFXIV_Modding_Tool");
                ModPackJson ttmpData = null;
                string ttmpName = null;
                List<ModsJson> ttmpDataList = new List<ModsJson>();
                PrintMessage($"Extracting data from {ttmpPath.Name}...");
                if (ttmpPath.Extension == ".ttmp2")
                {
                    var _ttmpData = ttmp.GetModPackJsonData(ttmpPath);
                    _ttmpData.Wait();
                    ttmpData = _ttmpData.Result.ModPackJson;
                }
                if (ttmpData != null)
                {
                    ttmpName = ttmpData.Name;
                    if (ttmpData.TTMPVersion.Contains("w"))
                    {
                        PrintMessage("Starting wizard...");
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
                ttmpDataLists[ttmpPath] = ttmpDataList;
            }
            
            var _totalMods = ttmpDataLists.Sum(x => x.Value.Count);
            PrintMessage($"Data extraction successful.");
            var _currentModpackNum = 1;
            foreach(var ttmpDataList in ttmpDataLists)
            {
                TTMP _textoolsModpack = new TTMP(ttmpDataList.Key, "FFXIV_Modding_Tool");
                int originalModCount = ttmpDataList.Value.Count;
                List<ModActiveStatus> modActiveStates = UpdateActiveModsConfFile(ttmpDataList.Value);
                PrintMessage($"Importing {ttmpDataList.Value.Count}/{originalModCount} mods from {ttmpDataList.Value[0].ModPackEntry.name} (Modpack {_currentModpackNum}/{ttmpDataLists.Count})...");
                ImportModpack(ttmpDataList.Value, _textoolsModpack, ttmpDataList.Key);
                File.WriteAllText(modActiveConfFile, JsonConvert.SerializeObject(modActiveStates, Formatting.Indented));
                PrintMessage($"Updated {modActiveConfFile} to reflect changes.", 1);
                _currentModpackNum++;
            }
        }
        
        List<ModsJson> TTMP2DataList(List<ModsJson> ttmpJson, ModPackJson ttmpData, bool useWizard, bool importAll)
        {
            List<ModsJson> ttmpDataList = new List<ModsJson>();
            if (!useWizard && !importAll)
                useWizard = PromptWizardUsage(ttmpJson.Count);
            if (useWizard)
            {
                PrintMessage($"\nName: {ttmpData.Name}\nVersion: {ttmpData.Version}\nAuthor: {ttmpData.Author}\n");
                ttmpJson = SimpleDataHandler(ttmpJson);
            }
            foreach (ModsJson mod in ttmpJson)
            {
                if (mod.ModPackEntry == null)
                    mod.ModPackEntry = new ModPack { name = ttmpData.Name, author = ttmpData.Author, version = ttmpData.Version, url = ttmpData.Url };
                ttmpDataList.Add(mod);
            }
            return ttmpDataList;
        }

        List<ModsJson> TTMPDataList(DirectoryInfo ttmpPath, bool useWizard, bool importAll)
        {
            List<ModsJson> ttmpJson = new List<ModsJson>();
            var originalModPackData = GetOldModpackJson(ttmpPath);
            string ttmpName = Path.GetFileNameWithoutExtension(ttmpPath.FullName);
            
            foreach (var modsJson in originalModPackData)
            {
                ttmpJson.Add(new ModsJson
                {
                    Name = modsJson.Name,
                    Category = modsJson.Category,
                    FullPath = modsJson.FullPath,
                    ModOffset = modsJson.ModOffset,
                    ModSize = modsJson.ModSize,
                    DatFile = modsJson.DatFile,
                    IsDefault = false,
                    ModPackEntry = new ModPack { name = ttmpName, author = "N/A", version = "1.0.0", url = "N/A" }
                });
            }
            if (!useWizard && !importAll)
                useWizard = PromptWizardUsage(ttmpJson.Count);
            if (useWizard)
            {
                PrintMessage($"\nName: {ttmpName}\nVersion: N/A\nAuthor: N/A\n");
                return SimpleDataHandler(ttmpJson);
            }
            return ttmpJson;
        }

        List<OriginalModPackJson> GetOldModpackJson(DirectoryInfo ttmpPath)
        {
            List<OriginalModPackJson> originalModPackData = new List<OriginalModPackJson>();
            var fs = new FileStream(ttmpPath.FullName, FileMode.Open, FileAccess.Read);
            ZipArchive archive = new ZipArchive(fs);
            ZipArchiveEntry mplFile = archive.GetEntry("TTMPL.mpl");
            {
                using (var streamReader = new StreamReader(mplFile.Open()))
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
                PrintMessage($"This modpack contains {modCount} mods, using the wizard could be a tedious process", 3);

            while (!userPicked && modCount > 1)
            {
                PrintMessage($"Would you like to use the Wizard for importing?\n(Y)es, let me select the mods\n(N)o, import everything");
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
                PrintMessage("\n");
            }
            return answer;
        }

        List<ModsJson> WizardDataHandler(ModPackJson ttmpData)
        {
            Validators validation = new Validators();
            List<ModsJson> modpackData = new List<ModsJson>();
            PrintMessage($"\nName: {ttmpData.Name}\nVersion: {ttmpData.Version}\nAuthor: {ttmpData.Author}\n{ttmpData.Description}\n");
            foreach (var page in ttmpData.ModPackPages)
            {
                if (ttmpData.ModPackPages.Count > 1)
                    PrintMessage($"Page {page.PageIndex}");
                foreach (var option in page.ModGroups)
                {
                    bool userDone = false;
                    while (!userDone)
                    {
                        PrintMessage($"{option.GroupName}\nChoices:");
                        List<string> choices = new List<string>();
                        foreach (var choice in option.OptionList)
                        {
                            string description = "";
                            if (!string.IsNullOrEmpty(choice.Description))
                                description = $"\n\t{choice.Description}";
                            choices.Add($"    {option.OptionList.IndexOf(choice)} - {choice.Name}{description}");
                        }
                        PrintMessage(string.Join("\n", choices));
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
                            if (validation.PromptContinuation($"\nYou picked:\n{string.Join("\n", pickedChoices)}\nIs this correct?", true))
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
                            if (validation.PromptContinuation($"\nYou picked:\n{wantedIndex} - {option.OptionList[wantedIndex].Name}\nIs this correct?", true))
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

        List<ModsJson> SimpleDataHandler(List<ModsJson> ttmpJson)
        {
            Validators validation = new Validators();
            List<ModsJson> desiredMods = new List<ModsJson>();
            for (int i = 0; i < ttmpJson.Count; i = i + 50)
            {
                var items = ttmpJson.Skip(i).Take(50).ToList();
                if (ttmpJson.Count > 50)
                    PrintMessage($"{i}-{i + items.Count} ({ttmpJson.Count} total)");
                bool userDone = false;
                while (!userDone)
                {
                    PrintMessage("Mods:");
                    List<string> mods = new List<string>();
                    foreach (var item in items)
                        mods.Add($"    {items.IndexOf(item)} - {item.Name}, {GetMap(item.FullPath)}, {GetRace(item.FullPath)}");
                    PrintMessage(string.Join("\n", mods));
                    Console.Write("Choose mods you wish to import (eg: 1 2 3, 0-3, *): ");
                    List<int> wantedMods = WizardUserInputValidation(Console.ReadLine(), items.Count, true);
                    List<string> pickedMods = new List<string>();
                    foreach (int index in wantedMods)
                        pickedMods.Add(mods[index]);
                    if (!pickedMods.Any())
                        pickedMods.Add("nothing");
                    if (validation.PromptContinuation($"\nYou picked:\n{string.Join("\n", pickedMods)}\nIs this correct?", true))
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
                            PrintMessage($"{answer} is not a valid range of choices", 2);
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
                            PrintMessage($"There are only {totalChoices} choices, not {wantedIndex + 1}", 2);
                    }
                    else
                        PrintMessage($"{answer} is not a valid choice", 2);
                }
            }
            else
            {
                if (!canBeEmpty)
                    desiredIndexes.Add(0);
            }
            return desiredIndexes;
        }

        public List<ModActiveStatus> UpdateActiveModsConfFile(List<ModsJson> ttmpJson)
        {
            List<ModActiveStatus> modActiveStates = new List<ModActiveStatus>();
            if (File.Exists(modActiveConfFile) && !string.IsNullOrEmpty(File.ReadAllText(modActiveConfFile)))
                modActiveStates = JsonConvert.DeserializeObject<List<ModActiveStatus>>(File.ReadAllText(modActiveConfFile));
            bool alreadyExists = false;
            int modIndex = 0;
            foreach (ModsJson entry in ttmpJson)
            {
                foreach (ModActiveStatus modState in modActiveStates)
                {
                    if (entry.FullPath == modState.file)
                    {
                        modIndex = modActiveStates.IndexOf(modState);
                        alreadyExists = true;
                        break;
                    }
                }
                if (!alreadyExists)
                    modActiveStates.Add(new ModActiveStatus(entry));
                else
                    modActiveStates[modIndex] = new ModActiveStatus(entry);
            }
            return modActiveStates;
        }

        void ImportModpack(List<ModsJson> ttmpJson, TTMP _textoolsModpack, DirectoryInfo ttmpPath)
        {
            var modlistPath = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
            int totalModsImported = 0;
            
            try
            {
                string importErrors = ImportStarter(_textoolsModpack, ttmpPath, ttmpJson, modlistPath).Result;
                if (!string.IsNullOrEmpty(importErrors))
                    PrintMessage($"There were errors importing some mods:\n{importErrors}", 2);
                else
                {
                    totalModsImported = ttmpJson.Count();
                    PrintMessage($"\n{totalModsImported} mod(s) successfully imported.", 1);
                }
            }
            catch (Exception ex)
            {
                PrintMessage($"There was an error attempting to import mods:\n{ex.Message}", 2);
            }
        }

        async Task<string> ImportStarter(TTMP _textoolsModpack, DirectoryInfo ttmpPath, List<ModsJson> ttmpJson, DirectoryInfo modlistPath)
        {
            var importer = ImportManager(_textoolsModpack, ttmpPath, ttmpJson, modlistPath);
            var watchdog = ImportWatcher(importer);
            await watchdog;
            await importer;
            return importer.Result;
        }

        async Task ImportWatcher(Task<string> task)
        {
            int timeout = 10000;
            int loops = 0;
            bool importStartedOrFinished = false;
            PrintMessage("Waiting for import process to respond...\nIf percentage doesn't display for a few minutes, wipe your mod_cache.db and try again");
            while (!importStartedOrFinished)
            {
                if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                {
                    if (importStarted)
                        importStartedOrFinished = true;
                    else
                    {
                        task.Dispose();
                        task.Start();
                    }
                }
                else
                    importStartedOrFinished = true;
                if (loops == 5 && !importStartedOrFinished)
                    PrintMessage($"\nImport failed to start after {loops} retries", 2);
                loops++;
            }
        }

        async Task<string> ImportManager(TTMP _textoolsModpack, DirectoryInfo ttmpPath, List<ModsJson> ttmpJson, DirectoryInfo modlistPath)
        {
            var progressIndicator = new Progress<(int current, int total, string message)>(ReportProgress);
            var importResults = await _textoolsModpack.ImportModPackAsync(ttmpPath, ttmpJson,
                _indexDirectory, modlistPath, progressIndicator);
            return importResults.Errors;
        }

        void ReportProgress((int current, int total, string message) report)
        {
            importStarted = true;
            double progress = new double();
            if (report.message == "Job Done.")
                report.message = "Done!";
            if (report.total == 0)
                Console.Write("\r" + new string(' ', Console.WindowWidth) + $"\r{report.message}");
            else
            {
                progress = ((double)report.current / (double)report.total) * 100;
                Console.Write("\r" + new string(' ', Console.WindowWidth) + $"\r{report.message} {(int)progress}%");
            }
        }

        XivRace GetRace(string modPath)
        {
            var xivRace = XivRace.All_Races;

            if (modPath.Contains("ui/") || modPath.Contains(".avfx"))
                xivRace = XivRace.All_Races;
            else if (modPath.Contains("monster"))
                xivRace = XivRace.Monster;
            else if (modPath.Contains("bgcommon"))
                xivRace = XivRace.All_Races;
            else if (modPath.Contains(".tex") || modPath.Contains(".mdl") || modPath.Contains(".atex"))
            {
                if (modPath.Contains("accessory") || modPath.Contains("weapon") || modPath.Contains("/common/"))
                    xivRace = XivRace.All_Races;
                else
                {
                    if (modPath.Contains("demihuman"))
                        xivRace = XivRace.DemiHuman;
                    else if (modPath.Contains("/v"))
                    {
                        var raceCode = modPath.Substring(modPath.IndexOf("_c") + 2, 4);
                        xivRace = XivRaces.GetXivRace(raceCode);
                    }
                    else
                    {
                        var raceCode = modPath.Substring(modPath.IndexOf("/c") + 2, 4);
                        xivRace = XivRaces.GetXivRace(raceCode);
                    }
                }
            }
            return xivRace;
        }

        string GetNumber(string modPath)
        {
            var number = "-";

            if (modPath.Contains("/human/") && modPath.Contains("/body/"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("/b") + 2, 4);
                number = int.Parse(subString).ToString();
            }

            if (modPath.Contains("/face/"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("/f") + 2, 4);
                number = int.Parse(subString).ToString();
            }

            if (modPath.Contains("decal_face"))
            {
                var length = modPath.LastIndexOf(".") - (modPath.LastIndexOf("_") + 1);
                var subString = modPath.Substring(modPath.LastIndexOf("_") + 1, length);

                number = int.Parse(subString).ToString();
            }

            if (modPath.Contains("decal_equip"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("_") + 1, 3);

                try
                {
                    number = int.Parse(subString).ToString();
                }
                catch
                {
                    if (modPath.Contains("stigma"))
                        number = "stigma";
                    else
                        number = "Error";
                }
            }

            if (modPath.Contains("/hair/"))
            {
                var t = modPath.Substring(modPath.LastIndexOf("/h") + 2, 4);
                number = int.Parse(t).ToString();
            }

            if (modPath.Contains("/tail/"))
            {
                var t = modPath.Substring(modPath.LastIndexOf("l/t") + 3, 4);
                number = int.Parse(t).ToString();
            }

            return number;
        }

        string GetType(string modPath)
        {
            var exRaw = Path.GetExtension(modPath);
            if(string.IsNullOrEmpty(exRaw))
                return "Unknown";
            var ext = exRaw.Substring(1);
            if(ext == "mdl")
                return "Model";
            else if ( ext == "meta")
                return "Metadata";
            else if (ext == "mtrl")
                return "Material";
            else if(ext == "tex")
                return "Texture - " + GuessTextureUsage(modPath).ToString();
            else
                return ext.ToUpper();
        }

        string GetMap(string modPath)
        {
            var xivTexType = XivTexType.Other;

            if (modPath.Contains(".mdl"))
                return "3D";

            if (modPath.Contains(".mtrl"))
                return "ColorSet";

            if (modPath.Contains("ui/"))
            {
                var subString = modPath.Substring(modPath.IndexOf("/") + 1);
                return subString.Substring(0, subString.IndexOf("/"));
            }

            if (modPath.Contains("_s.tex") || modPath.Contains("skin_m"))
                xivTexType = XivTexType.Specular;
            else if (modPath.Contains("_d.tex"))
                xivTexType = XivTexType.Diffuse;
            else if (modPath.Contains("_n.tex"))
                xivTexType = XivTexType.Normal;
            else if (modPath.Contains("_m.tex"))
                xivTexType = XivTexType.Multi;
            else if (modPath.Contains(".atex"))
            {
                var atex = Path.GetFileNameWithoutExtension(modPath);
                return atex.Substring(0, 4);
            }
            else if (modPath.Contains("decal"))
                xivTexType = XivTexType.Mask;

            return xivTexType.ToString();
        }

        public static XivTexType GuessTextureUsage(string path) {
            Regex _normRegex = new Regex("(_n(\\.|_))|(norm)");
            Regex _diffuseRegex = new Regex("(_d(\\.|_))|(diff)");
            Regex _specRegex = new Regex("(_s(\\.|_))|(spec)");
            Regex _multiRegex = new Regex("(_m(\\.|_))|(mul)|(mask)");
            Regex _reflectionRegex = new Regex("(catchlight|refl)");
            Regex _iconRegex = new Regex("^ui/icon/");
            Regex _mapRegex = new Regex("^ui/map/");
            Regex _loadingImageRegex = new Regex("^ui/loadingimage/");
            Regex _uldRegex = new Regex("^ui/uld/");

            if(_normRegex.IsMatch(path))
                return XivTexType.Normal;
            else if(_diffuseRegex.IsMatch(path))
                return XivTexType.Diffuse;
            else if (_specRegex.IsMatch(path))
                return XivTexType.Specular;
            else if (_multiRegex.IsMatch(path))
                return XivTexType.Multi;
            else if (_reflectionRegex.IsMatch(path))
                return XivTexType.Reflection;
            else if(_iconRegex.IsMatch(path))
                return XivTexType.Icon;
            else if (_mapRegex.IsMatch(path))
                return XivTexType.Map;
            else if (_loadingImageRegex.IsMatch(path))
                return XivTexType.UI;
            else if (_uldRegex.IsMatch(path))
                return XivTexType.UI;
            else
                return XivTexType.Other;
        }

        static readonly Dictionary<string, string> FaceTypes = new Dictionary<string, string>
        {
            {"fac", "Face"},
            {"iri", "Iris"},
            {"etc", "Etc"},
            {"acc", "Accessory"}
        };

        static readonly Dictionary<string, string> HairTypes = new Dictionary<string, string>
        {
            {"acc", "Accessory"},
            {"hir", "Hair"},
        };

        static readonly Dictionary<string, string> slotAbr = new Dictionary<string, string>
        {
            {"met", "Head"},
            {"glv", "Hands"},
            {"dwn", "Legs"},
            {"sho", "Feet"},
            {"top", "Body"},
            {"ear", "Ears"},
            {"nek", "Neck"},
            {"rir", "Ring Right"},
            {"ril", "Ring Left"},
            {"wrs", "Wrists"},
        };
        #endregion

        public void CreateModpack(DirectoryInfo outputFile)
        {
            string name = "";
            if (!outputFile.Exists)
            {
                PrintMessage("Name of the modpack?");
                name = Console.ReadLine();
            }
            else
                name = Path.GetFileNameWithoutExtension(outputFile.FullName);
            PrintMessage("Version of the modpack (in x.x.x format)?");
            Version version = Version.Parse(Console.ReadLine());
            PrintMessage("Author of the modpack?");
            string author = Console.ReadLine();
            PrintMessage("Full path to where you want to save the modpack");
            DirectoryInfo modpackDir = new DirectoryInfo("/tmp/placeholder.ttmp");
            if (!outputFile.Exists)
                modpackDir = new DirectoryInfo(Console.ReadLine());
            else
                modpackDir = new DirectoryInfo(Path.GetDirectoryName(outputFile.FullName));
            if (!modpackDir.Exists)
                PrintMessage($"Can't find {modpackDir}. Does it exist?", 2);
            var ttmp = new TTMP(modpackDir, "FFXIV_Modding_Tool");
            SimpleModPackData modpackData = new SimpleModPackData
            {
                Name = name,
                Author = author,
                Version = version,
                SimpleModDataList = new List<SimpleModData>()
            };
            var dat = new Dat(new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "sqpack", "ffxiv")));
            var localModData = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(Path.Combine(_gameDirectory.FullName, "XivMods.json")));
            foreach (Mod mod in localModData.Mods)
            {
                if (mod.source == "_INTERNAL_" || !mod.enabled)
                    continue;
                var compressedSize = mod.data.modSize;
                try
                {
                    var getCompressedFileSize = dat.GetCompressedFileSize(mod.data.modOffset, IOUtil.GetDataFileFromPath(mod.fullPath));
                    getCompressedFileSize.Wait();
                    compressedSize = getCompressedFileSize.Result;
                } catch
                {
                    // If the calculation fails, the TexTools people just use the original size
                }
                SimpleModData modData = new SimpleModData
                {
                    Name = mod.name,
                    Category = mod.category,
                    FullPath = mod.fullPath,
                    ModOffset = mod.data.modOffset,
                    ModSize = compressedSize,
                    DatFile = mod.datFile
                };
                modpackData.SimpleModDataList.Add(modData);
            }
            Progress<(int current, int total, string message)> progressIndicator = new Progress<(int current, int total, string message)>(ReportProgress);
            string modpackPath = "";
            if (!outputFile.Exists)
                modpackPath = Path.Combine(modpackDir.FullName, $"{name}.ttmp2");
            else
                modpackPath = outputFile.FullName;
            bool overwriteModpack = false;
            Validators validation = new Validators();
            if (File.Exists(modpackPath))
                overwriteModpack = validation.PromptContinuation($"{modpackPath} already exists, do you want to overwrite it? y/N");
            PrintMessage("Creating modpack...");
            var modpackCreation = ttmp.CreateSimpleModPack(modpackData, _gameDirectory, progressIndicator, overwriteModpack);
            modpackCreation.Wait();
        }

        #region Index File Handling
        Dictionary<string, XivDataFile> IndexFiles()
        {
            Dictionary<string, XivDataFile> indexFiles = new Dictionary<string, XivDataFile>();
            string indexExtension = ".win32.index";
            string index2Extension = ".win32.index2";
            List<XivDataFile> dataFiles = new List<XivDataFile>
            {
                XivDataFile._01_Bgcommon,
                XivDataFile._04_Chara,
                XivDataFile._06_Ui
            };
            foreach (XivDataFile dataFile in dataFiles)
            {
                indexFiles.Add($"{dataFile.GetDataFileName()}{indexExtension}", dataFile);
                indexFiles.Add($"{dataFile.GetDataFileName()}{index2Extension}", dataFile);
            }
            return indexFiles;
        }

        public void BackupIndexes()
        {
            if (!_backupDirectory.Exists)
            {
                PrintMessage($"{_backupDirectory.FullName} does not exist, please specify an existing directory", 2);
                return;
            }
            if (IndexLocked())
            {
                PrintMessage("Can't make backups while the game is running", 2);
                return;
            }
            string modFile = Path.Combine(_gameDirectory.FullName, "XivMods.json");
            if (File.Exists(modFile))
            {
                var modData = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(modFile));
                if (modData.Mods.Count > 0)
                {
                    bool allDisabled = true;
                    foreach (Mod mod in modData.Mods)
                    {
                        if (mod.enabled)
                        {
                            allDisabled = false;
                            break;
                        }
                    }
                    if (!allDisabled)
                    {
                        PrintMessage("Can't make backups while the game is still modded.\nPlease disable all mods or reset your index files first", 2);
                        return;
                    }
                }
            }
            PrintMessage("Backing up index files...");
            try
            {
                foreach (string indexFile in IndexFiles().Keys)
                {
                    string indexPath = Path.Combine(_indexDirectory.FullName, indexFile);
                    string backupPath = Path.Combine(_backupDirectory.FullName, indexFile);
                    File.Copy(indexPath, backupPath, true);
                }
            }
            catch (Exception ex)
            {
                PrintMessage($"Something went wrong when backing up the index files\n{ex.Message}", 2);
                return;
            }
            PrintMessage("Successfully backed up the index files!", 1);
        }

        public void ResetMods()
        {
            bool allFilesAvailable = true;
            bool indexesUpToDate = true;
            var problemChecker = new ProblemChecker(_indexDirectory);
            if (!_backupDirectory.Exists)
            {
                PrintMessage($"{_backupDirectory.FullName} does not exist, please specify an existing directory", 2);
                return;
            }
            foreach (KeyValuePair<string, XivDataFile> indexFile in IndexFiles())
            {
                string backupPath = Path.Combine(_backupDirectory.FullName, indexFile.Key);
                if (!File.Exists(backupPath))
                {
                    PrintMessage($"{indexFile.Key} not found, aborting...", 3);
                    allFilesAvailable = false;
                    break;
                }
                var outdatedBackupsCheck = problemChecker.CheckForOutdatedBackups(indexFile.Value, _backupDirectory);
                outdatedBackupsCheck.Wait();
                if (!outdatedBackupsCheck.Result)
                {
                    PrintMessage($"{indexFile.Key} is out of date, aborting...", 3);
                    indexesUpToDate = false;
                    break;
                }
            }
            if (!allFilesAvailable || !indexesUpToDate)
            {
                PrintMessage($"{_backupDirectory.FullName} has missing or outdated index files. You can either\n1. Download them from the TT discord\n2. Run this command again using \"backup\" instead of \"reset\" using a clean install of the game", 2);
                return;
            }
            if (IndexLocked())
            {
                PrintMessage("Can't reset the game while the game is running", 2);
                return;
            }
            try
            {
                var reset = Task.Run(() =>
                {
                    var modding = new Modding(_indexDirectory);
                    var dat = new Dat(_indexDirectory);
                    var modListDirectory = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
                    var backupFiles = Directory.GetFiles(_backupDirectory.FullName);
                    foreach (var backupFile in backupFiles)
                    {
                        if (backupFile.Contains(".win32.index"))
                            File.Copy(backupFile, $"{_indexDirectory}/{Path.GetFileName(backupFile)}", true);
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
                    File.Delete(modListDirectory.FullName);
                    modding.CreateModlist();
                    if (File.Exists(modActiveConfFile))
                        File.WriteAllText(modActiveConfFile, string.Empty);
                });
                reset.Wait();
                PrintMessage("Reset complete!", 1);
            }
            catch (Exception ex)
            {
                PrintMessage($"Something went wrong during the reset process\n{ex.Message}", 2);
            }
        }
        #endregion

        #region Problem Checking
        public void ProblemChecker()
        {
            var problemChecker = new ProblemChecker(_indexDirectory);
            List<string> problemsResolved = new List<string>();
            List<string> problemsUnresolved = new List<string>();
            PrintMessage("Initializing problem check");
            PrintMessage("Checking index dat values...");
            List<XivDataFile> _indexDatRepairList = CheckIndexDatCounts(problemChecker);
            if (_indexDatRepairList.Count > 0)
            {
                if (!IndexLocked())
                {
                    foreach (var xivDataFile in _indexDatRepairList)
                        problemChecker.RepairIndexDatCounts(xivDataFile);
                    PrintMessage("Rechecking index dat values...");
                    List<XivDataFile> unfixedIndexes = CheckIndexDatCounts(problemChecker);
                    if (unfixedIndexes.Count > 0)
                        problemsUnresolved.Add("Issues with dat files were found, a reset is recommended");
                    else
                        problemsResolved.Add("Index files needed repairs");
                }
                else
                    problemsUnresolved.Add("Can't repair index files while the game is running");
            }

            PrintMessage("Checking index backups...");
            problemsUnresolved.AddRange(CheckBackups(problemChecker));

            PrintMessage("Checking dat file...");
            problemsUnresolved.AddRange(CheckDat());

            PrintMessage("Checking modlist...");
            problemsUnresolved.AddRange(CheckMods());

            PrintMessage("Checking LoD settings...");
            Dictionary<string, bool> lodIssues = CheckLoD();
            problemsResolved.AddRange((from issue in lodIssues where issue.Value select issue.Key).ToList());
            problemsUnresolved.AddRange((from issue in lodIssues where !issue.Value select issue.Key).ToList());

            if (problemsResolved.Count == 0 && problemsUnresolved.Count == 0)
                PrintMessage("No problems found", 1);
            if (problemsResolved.Count > 0)
                PrintMessage($"\nThe following problems were found and resolved:\n{string.Join("\n", problemsResolved.ToArray())}", 1);
            if (problemsUnresolved.Count > 0)
            {
                Console.Write("\n");
                PrintMessage($"The following problems could not be resolved:\n{string.Join("\n", problemsUnresolved.ToArray())}", 2);
            }
        }

        List<XivDataFile> CheckIndexDatCounts(ProblemChecker problemChecker)
        {
            var filesToCheck = new XivDataFile[] { XivDataFile._0A_Exd, XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
            List<XivDataFile> _indexDatIssueList = new List<XivDataFile>();
            foreach (var file in filesToCheck)
            {
                int atFile = Array.IndexOf(filesToCheck, file) + 1;
                Console.Write($"\r{(int)(0.5f + ((100f * atFile) / filesToCheck.Length))}%");
                try
                {
                    var datCountsCheck = problemChecker.CheckIndexDatCounts(file);
                    datCountsCheck.Wait();
                    if (datCountsCheck.Result)
                    {
                        _indexDatIssueList.Add(file);
                        continue;
                    }
                    var largeDatCheck = problemChecker.CheckForLargeDats(file);
                    largeDatCheck.Wait();
                    if (largeDatCheck.Result)
                        _indexDatIssueList.Add(file);
                }
                catch (Exception ex)
                {
                    PrintMessage($"There was an issue checking index dat counts\n{ex.Message}", 3);
                    return new List<XivDataFile>();
                }
            }
            Console.Write("\n");
            return _indexDatIssueList;
        }

        List<string> CheckBackups(ProblemChecker problemChecker)
        {
            var filesToCheck = new XivDataFile[] { XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
            List<string> problemsFound = new List<string>();
            foreach (var file in filesToCheck)
            {
                int atFile = Array.IndexOf(filesToCheck, file) + 1;
                Console.Write($"\r{(int)(0.5f + ((100f * atFile) / filesToCheck.Length))}%");
                string fileName = file.GetDataFileName();
                try
                {
                    var backupFile = Path.Combine(_backupDirectory.FullName, $"{fileName}.win32.index");
                    if (!File.Exists(backupFile))
                    {
                        problemsFound.Add($"Index backups for {fileName} not found");
                        continue;
                    }
                    var outdatedBackupsCheck = problemChecker.CheckForOutdatedBackups(file, _backupDirectory);
                    outdatedBackupsCheck.Wait();
                    if (!outdatedBackupsCheck.Result)
                        problemsFound.Add($"Index backups for {fileName} are out of date");
                }
                catch (Exception ex)
                {
                    PrintMessage($"There was an issue checking the backed up index files\n{ex.Message}", 3);
                    return new List<string>();
                }
            }
            Console.Write("\n");
            return problemsFound;
        }

        List<string> CheckDat()
        {
            List<string> problemsFound = new List<string>();
            string dataFileName = $"{XivDataFile._06_Ui.GetDataFileName()}.win32.dat1";
            var fileInfo = new FileInfo(Path.Combine(_indexDirectory.FullName, dataFileName));
            if (fileInfo.Exists)
            {
                if (fileInfo.Length < 10000000)
                    problemsFound.Add($"{dataFileName} is missing data");
            }
            else
                problemsFound.Add($"Game directory is missing {dataFileName}");
            PrintMessage("100%");
            return problemsFound;
        }

        List<string> CheckMods()
        {
            var modlistPath = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
            var modlistJson = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(modlistPath.FullName));
            var dat = new Dat(_indexDirectory);
            List<string> problemsFound = new List<string>();
            if (modlistJson.Mods.Count > 0)
            {
                foreach (var mod in modlistJson.Mods)
                {
                    int atMod = modlistJson.Mods.IndexOf(mod) + 1;
                    Console.Write($"\r{(int)(0.5f + ((100f * atMod) / modlistJson.Mods.Count))}%");
                    if (mod.name.Equals(string.Empty))
                        continue;
                    var fileName = Path.GetFileName(mod.fullPath);
                    if (mod.data.originalOffset == 0)
                        problemsFound.Add($"{fileName} has an original offset of 0. You will need to reset to remove this mod");
                    else if (mod.data.modOffset == 0)
                        problemsFound.Add($"{fileName} has a mod offset of 0, disable it and reimport");

                    var fileType = 0;
                    try
                    {
                        fileType = dat.GetFileType(mod.data.modOffset, XivDataFiles.GetXivDataFile(mod.datFile));
                    }
                    catch (Exception ex)
                    {
                        PrintMessage($"{ex.Message}", 2);
                    }

                    if (fileType != 2 && fileType != 3 && fileType != 4)
                        problemsFound.Add($"{fileName} has an unknown file type ({fileType}), offset is most likely corrupt");
                }
                Console.Write("\n");
            }
            else
                PrintMessage("No entries found in the modlist, skipping", 3);
            return problemsFound;
        }

        Dictionary<string, bool> CheckLoD()
        {
            Dictionary<string, bool> problemsFound = new Dictionary<string, bool>();
            if (_configDirectory == null)
            {
                PrintMessage("No config directory specified, skipping", 3);
                return new Dictionary<string, bool>();
            }
            Console.Write("\r0%");
            var problem = false;
            var DX11 = false;
            string ffxivCfg = Path.Combine(_configDirectory.FullName, "FFXIV.cfg");
            string ffxivbootCfg = Path.Combine(_configDirectory.FullName, "FFXIV_BOOT.cfg");

            if (_configDirectory.Exists)
            {
                Console.Write("\r25%");
                if (File.Exists(ffxivbootCfg))
                {
                    var lines = File.ReadAllLines(ffxivbootCfg);
                    foreach (var line in lines)
                    {
                        if (line.Contains("DX11Enabled"))
                        {
                            var val = line.Substring(line.Length - 1, 1);
                            if (val.Equals("1"))
                                DX11 = true;
                            break;
                        }
                    }
                }
                else
                    problemsFound.Add($"Could not find {ffxivbootCfg}", false);
                Console.Write("\r50%");
                if (File.Exists(ffxivCfg))
                {
                    var lines = File.ReadAllLines(ffxivCfg);
                    var lineNum = 0;
                    var tmpLine = 0;
                    foreach (var line in lines)
                    {
                        if (line.Contains("LodType"))
                        {
                            var val = line.Substring(line.Length - 1, 1);
                            if (DX11 && line.Contains("DX11"))
                            {
                                if (val.Equals("1"))
                                {
                                    tmpLine = lineNum;
                                    problem = true;
                                    break;
                                }
                            }
                            else if (!DX11 && !line.Contains("DX11"))
                            {
                                if (val.Equals("1"))
                                {
                                    tmpLine = lineNum;
                                    problem = true;
                                    break;
                                }
                            }
                        }
                        lineNum++;
                    }
                    Console.Write("\r75%");
                    if (problem)
                    {
                        var line = lines[tmpLine];
                        line = line.Substring(0, line.Length - 1) + 0;
                        lines[tmpLine] = line;
                        File.WriteAllLines(ffxivCfg, lines);
                        problemsFound.Add("LoD turned off, as some mods have issues with it turned on", true);
                    }
                }
                else
                    problemsFound.Add($"Could not find {ffxivCfg}", false);
            }
            else
                problemsFound.Add($"{_configDirectory.FullName} does not exist", false);
            Console.Write("\r100%\n");
            return problemsFound;
        }
        #endregion

        public void ToggleModStates(bool enable)
        {
            string modstate = "";
            if (enable)
                modstate = "on";
            else
                modstate = "off";
            var modding = new Modding(_indexDirectory);
            PrintMessage($"Turning {modstate} all mods...");
            try
            {
                var toggle = modding.ToggleAllMods(enable);
                toggle.Wait();
                PrintMessage($"Successfully turned {modstate} all mods", 1);
            }
            catch (Exception e)
            {
                PrintMessage($"There was an issue turning {modstate} mods:\n{e}", 2);
            }
        }

        public void SetModActiveStates()
        {
            Modding modding = new Modding(_indexDirectory);
            string modlistFile = Path.Combine(_gameDirectory.FullName, "XivMods.json");
            List<ModActiveStatus> modActiveStates = new List<ModActiveStatus>();
            if (!File.Exists(modActiveConfFile) || string.IsNullOrEmpty(File.ReadAllText(modActiveConfFile)))
            {
                PrintMessage("Can't enable/disable mods when no mods are installed", 2);
                return;
            }
            if (File.Exists(modActiveConfFile) && !string.IsNullOrEmpty(File.ReadAllText(modActiveConfFile)))
                modActiveStates = JsonConvert.DeserializeObject<List<ModActiveStatus>>(File.ReadAllText(modActiveConfFile));
            int enabled = 0;
            int disabled = 0;
            try
            {
                PrintMessage("Toggling mods...");
                foreach (ModActiveStatus modState in modActiveStates)
                {
                    var toggle = modding.ToggleModStatus(modState.file, modState.enabled);
                    toggle.Wait();
                    if (modState.enabled)
                        enabled++;
                    else
                        disabled++;
                    int atMod = modActiveStates.IndexOf(modState) + 1;
                    Console.Write($"\r{(int)(0.5f + ((100f * atMod) / modActiveStates.Count))}%...  ");
                }
            }
            catch (Exception ex)
            {
                PrintMessage($"Something went wrong during the toggle process\n{ex.Message}", 3);
                return;
            }
            PrintMessage($"\nSuccessfully enabled {enabled} and disabled {disabled} out of {modActiveStates.Count} mods!", 1);
        }

        private static DirectoryInfo GetConfigurationPath()
        {
            var _ConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //Workaround for Mac as dotnetcore doesn't seem to return a valid ApplicationData folder.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && string.IsNullOrEmpty(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))){
                _ConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config";
            }
            return new DirectoryInfo(Path.Combine(_ConfigPath, "FFXIV_Modding_Tool"));
        }

        static void Main(string[] args)
        {
            _projectconfDirectory = GetConfigurationPath();
            Config config = new Config();
            modActiveConfFile = Path.Combine(_projectconfDirectory.FullName, "modlist.cfg");
            //Can be removed on 1.0 release. Defined to move old files with typo
            string _oldmodActiveConfFile = Path.Combine(_projectconfDirectory.FullName, "modlist.cgf");
            if (File.Exists(_oldmodActiveConfFile) && !File.Exists(modActiveConfFile))
                File.Move(_oldmodActiveConfFile, modActiveConfFile);
            //End of temporary file rename section
            Arguments arguments = new Arguments();
            arguments.ArgumentHandler(args);
        }
    }
}
