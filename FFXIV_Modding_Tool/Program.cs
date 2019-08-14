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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Mods.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Mods.FileTypes;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Textures.Enums;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Commandline;
using ICSharpCode.SharpZipLib.Zip;
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

        public class ModActiveStatus
        {
            public string modpack { get; set; }
            public string name { get; set; }
            public string map { get; set; }
            public string part { get; set; }
            public string race { get; set; }
            public string file { get; set; }
            public bool enabled { get; set; }

            public ModActiveStatus() { }

            public ModActiveStatus(SimpleModPackEntries entry)
            {
                modpack = entry.JsonEntry.ModPackEntry.name;
                name = entry.Name;
                map = entry.Map;
                part = entry.Part;
                race = entry.Race;
                file = entry.JsonEntry.FullPath;
                enabled = true;
            }
        }
        public ModActiveStatus modpackActiveStatus;

        /* Print slightly nicer messages. Can add logging here as well if needed.
         1 = Success message, 2 = Error message, 3 = Warning message 
        */
        public void PrintMessage(string message, int importance = 0)
        {
            switch (importance)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 2:
                    Console.Write("ERROR: ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 3:
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void CheckVersions()
        {
            string ffxivVersion = "Not detected";
            if (_gameDirectory != null)
            {
                var versionFile = Path.Combine(_gameDirectory.FullName, "ffxivgame.ver");
                if (File.Exists(versionFile))
                {
                    var versionData = File.ReadAllLines(versionFile);
                    ffxivVersion = new Version(versionData[0].Substring(0, versionData[0].LastIndexOf("."))).ToString();
                }
            }
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            PrintMessage($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title} {version}\nGame version {ffxivVersion}");
        }

        public Dictionary<string, string> GetModpackInfo(DirectoryInfo ttmpPath)
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
            return modpackInfo;
        }

        public bool IndexLocked()
        {
            var index = new Index(_indexDirectory);
            bool indexLocked = index.IsIndexLocked(XivDataFile._0A_Exd);
            return indexLocked;
        }

        #region Importing Functions
        public void ImportModpackHandler(DirectoryInfo ttmpPath, bool useWizard, bool importAll, bool skipProblemCheck)
        {
            var importError = false;
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
                    PrintMessage($"Exception was thrown:\n{ex.Message}\nRetrying import...", 3);
                    ModpackDataHandler(ttmpPath, null, useWizard, importAll);
                }
                else
                {
                    PrintMessage($"There was an error importing the modpack at {ttmpPath.FullName}\nMessage: {ex.Message}", 2);
                    return;
                }
            }
            if (!skipProblemCheck)
                ProblemChecker();
            return;
        }

        void ModpackDataHandler(DirectoryInfo ttmpPath, ModPackJson ttmpData, bool useWizard, bool importAll)
        {
            var modding = new Modding(_indexDirectory);
            string ttmpName = null;
            List<SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            TTMP _textoolsModpack = new TTMP(ttmpPath, "FFXIV_Modding_Tool");
            PrintMessage($"Extracting data from {ttmpPath.Name}...");
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
            ttmpDataList.Sort();
            PrintMessage("Data extraction successfull.");
            int originalModCount = ttmpDataList.Count;
            string modActiveConfFile = Path.Combine(_projectconfDirectory.FullName, "modlist.cgf");
            List<ModActiveStatus> modActiveStates = UpdateActiveModsConfFile(modActiveConfFile, ttmpDataList);
            PrintMessage($"Importing {ttmpDataList.Count}/{originalModCount} mods from modpack...");
            ImportModpack(ttmpDataList, _textoolsModpack, ttmpPath);
            File.WriteAllText(modActiveConfFile, JsonConvert.SerializeObject(modActiveStates, Formatting.Indented));
            PrintMessage($"Updated {modActiveConfFile} to reflect changes.", 1);
        }

        List<SimpleModPackEntries> TTMP2DataList(List<ModsJson> modsJsons, ModPackJson ttmpData, bool useWizard, bool importAll)
        {
            List <SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            foreach (var modsJson in modsJsons)
            {
                var race = GetRace(modsJson.FullPath);
                var number = GetNumber(modsJson.FullPath);
                var type = GetType(modsJson.FullPath);
                var map = GetMap(modsJson.FullPath);

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
                    Part = type,
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
                PrintMessage($"\nName: {ttmpData.Name}\nVersion: {ttmpData.Version}\nAuthor: {ttmpData.Author}\n");
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
                var race = GetRace(modsJson.FullPath);
                var number = GetNumber(modsJson.FullPath);
                var type = GetType(modsJson.FullPath);
                var map = GetMap(modsJson.FullPath);

                var active = false;
                var isActive = XivModStatus.Disabled;

                if (isActive == XivModStatus.Enabled)
                    active = true;

                ttmpDataList.Add(new SimpleModPackEntries
                {
                    Name = modsJson.Name,
                    Category = modsJson.Category,
                    Race = race.ToString(),
                    Part = type,
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
                PrintMessage($"\nName: {ttmpName}\nVersion: N/A\nAuthor: N/A\n");
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
            if (modCount > 250)
                PrintMessage($"This modpack contains {modCount} mods, using the wizard could be a tedious process", 3);
            bool userPicked = false;
            bool answer = false;
            while (!userPicked)
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
            }
            return answer;
        }

        List<ModsJson> WizardDataHandler(ModPackJson ttmpData)
        {
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
                            Console.Write("Choose one or multiple (eg: 1 2 3, 0-3, *): ");
                            List<int> wantedIndexes = WizardUserInputValidation(Console.ReadLine(), maxChoices);
                            List<string> pickedChoices = new List<string>();
                            foreach (int index in wantedIndexes)
                                pickedChoices.Add($"{index} - {option.OptionList[index].Name}");
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
                            int wantedIndex = WizardUserInputValidation(Console.ReadLine(), maxChoices)[0];
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
                    PrintMessage($"{i}-{i + items.Count} ({ttmpDataList.Count} total)");
                bool userDone = false;
                while (!userDone)
                {
                    PrintMessage("Mods:");
                    List<string> mods = new List<string>();
                    foreach (var item in items)
                        mods.Add($"    {items.IndexOf(item)} - {item.Name}, {item.Map}, {item.Race}");
                    PrintMessage(string.Join("\n", mods));
                    Console.Write("Choose mods you wish to import (eg: 1 2 3, 0-3, *): ");
                    List<int> wantedMods = WizardUserInputValidation(Console.ReadLine(), items.Count);
                    List<string> pickedMods = new List<string>();
                    foreach (int index in wantedMods)
                        pickedMods.Add(mods[index]);
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

        List<int> WizardUserInputValidation(string input, int totalChoices)
        {
            List<int> desiredIndexes = new List<int>();
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
            return desiredIndexes;
        }

        public List<ModActiveStatus> UpdateActiveModsConfFile(string modActiveConfFile, List<SimpleModPackEntries> ttmpDataList)
        {
            List<ModActiveStatus> modActiveStates = new List<ModActiveStatus>();
            if (File.Exists(modActiveConfFile) && !string.IsNullOrEmpty(File.ReadAllText(modActiveConfFile)))
                modActiveStates = JsonConvert.DeserializeObject<List<ModActiveStatus>>(File.ReadAllText(modActiveConfFile));
            bool alreadyExists = false;
            int modIndex = 0;
            foreach (SimpleModPackEntries entry in ttmpDataList)
            {
                foreach (ModActiveStatus modState in modActiveStates)
                {
                    if (entry.JsonEntry.FullPath == modState.file)
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

        void ImportModpack(List<SimpleModPackEntries> ttmpDataList, TTMP _textoolsModpack, DirectoryInfo ttmpPath)
        {
            var importList = (from SimpleModPackEntries selectedItem in ttmpDataList select selectedItem.JsonEntry).ToList();
            var modlistPath = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
            int totalModsImported = 0;
            var progressIndicator = new Progress<(int current, int total, string message)>(ReportProgress);

            try
            {
                var importResults = _textoolsModpack.ImportModPackAsync(ttmpPath, importList,
                _indexDirectory, modlistPath, progressIndicator);
                importResults.Wait();
                if (!string.IsNullOrEmpty(importResults.Result.Errors))
                    PrintMessage($"There were errors importing some mods:\n{importResults.Result.Errors}", 2);
                else
                {
                    totalModsImported = ttmpDataList.Count();
                    PrintMessage($"\n{totalModsImported} mod(s) successfully imported.", 1);
                }
            }
            catch (Exception ex)
            {
                PrintMessage($"There was an error attempting to import mods:\n{ex.Message}", 2);
            }
        }

        void ReportProgress((int current, int total, string message) report)
        {
            var progress = ((double)report.current / (double)report.total) * 100;
            Console.Write($"\r{(int)progress}%");
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
            var type = "-";

            if (modPath.Contains(".tex") || modPath.Contains(".mdl") || modPath.Contains(".atex"))
            {
                if (modPath.Contains("demihuman"))
                    type = slotAbr[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];

                if (modPath.Contains("/face/"))
                {
                    if (modPath.Contains(".tex"))
                        type = FaceTypes[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];
                }

                if (modPath.Contains("/hair/"))
                {
                    if (modPath.Contains(".tex"))
                        type = HairTypes[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];
                }

                if (modPath.Contains("/vfx/"))
                    type = "VFX";
            }
            else if (modPath.Contains(".avfx"))
                type = "AVFX";
            
            return type;
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
                if (modData.modCount > 0)
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
                    string modActiveConfFile = Path.Combine(_projectconfDirectory.FullName, "modlist.cgf");

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
            {
                PrintMessage($"\nThe following problems were found and resolved:", 1);
                PrintMessage(string.Join("\n", problemsResolved.ToArray()));
            }
            if (problemsUnresolved.Count > 0)
            {
                Console.Write("\n");
                PrintMessage("The following problems could not be resolved:", 2);
                PrintMessage(string.Join("\n", problemsUnresolved.ToArray()));
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
                    PrintMessage($"There was an issue checking index dat counts\n{ex.Message}", 2);
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
                    PrintMessage($"There was an issue checking the backed up index files\n{ex.Message}", 2);
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
            if (modlistJson.modCount > 0)
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

        public void SetModActiveStates()
        {
            Modding modding = new Modding(_indexDirectory);
            string modActiveConfFile = Path.Combine(_projectconfDirectory.FullName, "modlist.cgf");
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
                    modding.ToggleModStatus(modState.file, modState.enabled);
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

        static void Main(string[] args)
        {
            _projectconfDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title));
            Config config = new Config();
            Arguments arguments = new Arguments();
            arguments.ArgumentHandler(args);
        }
    }
}
