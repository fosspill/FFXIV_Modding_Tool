﻿// Copyright © 2019 Ole Erik Brennhagen - All Rights Reserved
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
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Mods.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Mods.FileTypes;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Textures.Enums;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using CommandLine;

namespace FFXIV_TexTools_CLI
{
// Verbs does not support nesting :( 
    [Verb("modpackimport", HelpText = "Import a modpack")]
    public class importoptions
    {
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path to game install, including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string gameDirectory { get; set; }
        [Option('t', "ttmp", Required = true, HelpText = "Path to .ttmp(2) file")]
        public string ttmpPath { get; set; }
    }
    [Verb("export", HelpText = "Export modpack / texture / model")]
    public class exportoptions
    { //normal options here
    }
    [Verb("backup", HelpText = "Backup clean index files for use in reseting the game to a clean state")]
    public class backupoptions
    {
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path to game install, including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string gameDirectory { get; set; }
        [Option('b', "backupdirectory", Required = true, HelpText = "Full path to directory you want to use for backups")]
        public string backupDirectory { get; set; }
    }
    [Verb("reset", HelpText = "Reset game to clean state")]
    public class resetoptions
    {
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path to game install, including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string gameDirectory { get; set; }
        [Option('b', "backupdirectory", Required = true, HelpText = "Full path to directory with your index backups")]
        public string backupDirectory { get; set; }
    }
    [Verb("problemcheck", HelpText = "Check if there are any problems with the game, mod or backup files")]
    public class problemoptions
    {
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path to game install, including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string gameDirectory { get; set; }
        [Option('c', "configdirectory", Required = true, HelpText = "Full path to directory where FFXIV.cfg and character data is saved, including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string configDirectory { get; set; }
        [Option('b', "backupdirectory", Required = true, HelpText = "Full path to directory with your index backups")]
        public string backupDirectory { get; set; }
    }
    [Verb("gameversion", HelpText = "Display current game version")]
    public class versionoptions
    { 
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string gameDirectory { get; set; }
    }

    public class MainClass
    {
        public DirectoryInfo _gameDirectory;
        public DirectoryInfo _indexDirectory;

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
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;

        }

        void CheckGameVersion()
        {
            Version ffxivVersion = null;
            var versionFile = Path.Combine(_gameDirectory.FullName, "ffxivgame.ver");
            if (File.Exists(versionFile))
            {
                var versionData = File.ReadAllLines(versionFile);
                ffxivVersion = new Version(versionData[0].Substring(0, versionData[0].LastIndexOf(".")));
            }
            else
            {
                PrintMessage("Incorrect directory", 2);
                return;
            }
            PrintMessage(ffxivVersion.ToString());
        }

        public bool IndexLocked()
        {
            var index = new Index(_indexDirectory);
            bool indexLocked = index.IsIndexLocked(XivDataFile._0A_Exd);
            return indexLocked;
        }

        #region Importing Functions
        void ImportModpackHandler(DirectoryInfo ttmpPath)
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
                var ttmp = new TTMP(ttmpPath, "TexTools");

                try
                {
                    if (ttmpPath.Extension == ".ttmp2")
                    {
                        var ttmpData = ttmp.GetModPackJsonData(ttmpPath);
                        GetModpackData(ttmpPath, ttmpData.ModPackJson);
                    }
                    else
                        GetModpackData(ttmpPath, null);
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
                    GetModpackData(ttmpPath, null);
                }
                else
                {
                    PrintMessage($"There was an error importing the modpack at {ttmpPath.FullName}\nMessage: {ex.Message}", 2);
                    return;
                }
            }

            return;
        }

        void GetModpackData(DirectoryInfo ttmpPath, ModPackJson ttmpData)
        {
            var modding = new Modding(_indexDirectory);
            List<SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            TTMP _textoolsModpack = new TTMP(ttmpPath, "TexTools");
            PrintMessage($"Extracting data from {ttmpPath.Name}...");
            if (ttmpData != null)
            {
                foreach (var modsJson in ttmpData.SimpleModsList)
                {
                    var race = GetRace(modsJson.FullPath);
                    var number = GetNumber(modsJson.FullPath);
                    var type = GetType(modsJson.FullPath);
                    var map = GetMap(modsJson.FullPath);

                    var active = false;
                    var isActive = modding.IsModEnabled(modsJson.FullPath, false);

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
            }
            else
            {
                var originalModPackData = new List<OriginalModPackJson>();
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

                foreach (var modsJson in originalModPackData)
                {
                    var race = GetRace(modsJson.FullPath);
                    var number = GetNumber(modsJson.FullPath);
                    var type = GetType(modsJson.FullPath);
                    var map = GetMap(modsJson.FullPath);

                    var active = false;
                    var isActive = modding.IsModEnabled(modsJson.FullPath, false);

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
                            ModPackEntry = new ModPack { name = Path.GetFileNameWithoutExtension(ttmpPath.FullName), author = "N/A", version = "1.0.0" }
                        }
                    });
                }
            }
            ttmpDataList.Sort();
            PrintMessage("Data extraction successfull. Importing modpack...");
            ImportModpack(ttmpDataList, _textoolsModpack, ttmpPath);
        }

        void ImportModpack(List<SimpleModPackEntries> ttmpDataList, TTMP _textoolsModpack, DirectoryInfo ttmpPath)
        {
            var importList = (from SimpleModPackEntries selectedItem in ttmpDataList select selectedItem.JsonEntry).ToList();
            var modlistPath = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
            int totalModsImported = 0;
            var progressIndicator = new Progress<double>(ReportProgress);

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

        void ReportProgress(double value)
        {
            float progress = (float)value * 100;
            Console.Write($"\r{(int)progress}%...  ");
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

        void BackupIndexes(DirectoryInfo backupDirectory)
        {
            if (!backupDirectory.Exists)
            {
                PrintMessage($"{backupDirectory.FullName} does not exist, please specify an existing directory", 2);
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
                    PrintMessage("Can't make backups while the game is still modded.\nPlease clean your index files using reset first.", 2);
                    return;
                }
            }
            PrintMessage("Backing up index files...");
            try
            {
                foreach (string indexFile in IndexFiles().Keys)
                {
                    string indexPath = Path.Combine(_indexDirectory.FullName, indexFile);
                    string backupPath = Path.Combine(backupDirectory.FullName, indexFile);
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

        void ResetMods(DirectoryInfo backupDirectory)
        {
            bool allFilesAvailable = true;
            bool indexesUpToDate = true;
            var problemChecker = new ProblemChecker(_indexDirectory);
            if (!backupDirectory.Exists)
            {
                PrintMessage($"{backupDirectory.FullName} does not exist, please specify an existing directory", 2);
                return;
            }
            foreach (KeyValuePair<string, XivDataFile> indexFile in IndexFiles())
            {
                string backupPath = Path.Combine(backupDirectory.FullName, indexFile.Key);
                if (!File.Exists(backupPath))
                {
                    PrintMessage($"{indexFile.Key} not found, aborting...", 3);
                    allFilesAvailable = false;
                    break;
                }
                if (!problemChecker.CheckForOutdatedBackups(indexFile.Value, backupDirectory))
                {
                    PrintMessage($"{indexFile.Key} is out of date, aborting...", 3);
                    indexesUpToDate = false;
                    break;
                }
            }
            if (!allFilesAvailable || !indexesUpToDate)
            {
                PrintMessage($"{backupDirectory.FullName} has missing or outdated index files. You can either\n1. Download them from the TT discord\n2. Run this command again using \"backup\" instead of \"reset\" using a clean install of the game", 2);
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

                    var backupFiles = Directory.GetFiles(backupDirectory.FullName);
                    foreach (var backupFile in backupFiles)
                    {
                        if (backupFile.Contains(".win32.index"))
                            File.Copy(backupFile, $"{_indexDirectory}/{Path.GetFileName(backupFile)}", true);
                    }

                // Delete modded dat files
                foreach (var xivDataFile in (XivDataFile[])Enum.GetValues(typeof(XivDataFile)))
                    {
                        var datFiles = dat.GetModdedDatList(xivDataFile);

                        foreach (var datFile in datFiles)
                            File.Delete(datFile);

                        if (datFiles.Count > 0)
                            problemChecker.RepairIndexDatCounts(xivDataFile);
                    }

                // Delete mod list
                File.Delete(modListDirectory.FullName);
                    modding.CreateModlist();
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
        void ProblemChecker(DirectoryInfo configDirectory, DirectoryInfo backupDirectory)
        {
            var problemChecker = new ProblemChecker(_indexDirectory);
            PrintMessage("Initializing problem check...");
            PrintMessage("Checking index dat values...");
            List<XivDataFile> _indexDatRepairList = CheckIndexDatCounts(problemChecker);
            if (_indexDatRepairList.Count > 0)
            {
                PrintMessage("Problems found, attempting to repair...", 3);
                if (!IndexLocked())
                {
                    foreach (var xivDataFile in _indexDatRepairList)
                        problemChecker.RepairIndexDatCounts(xivDataFile);
                    PrintMessage("Repairs complete!", 1);
                    CheckIndexDatCounts(problemChecker);
                }
                else
                {
                    PrintMessage("Cannot run repairs while the game is running.", 2);
                }
            }

            PrintMessage("Checking index backups...");
            CheckBackups(backupDirectory, problemChecker);

            PrintMessage("Checking dat file...");
            CheckDat();

            PrintMessage("Checking modlist...");
            CheckMods();

            PrintMessage("Checking LoD settings...");
            CheckLoD(configDirectory);
        }

        List<XivDataFile> CheckIndexDatCounts(ProblemChecker problemChecker)
        {
            var filesToCheck = new XivDataFile[] { XivDataFile._0A_Exd, XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
            List<XivDataFile> _indexDatRepairList = new List<XivDataFile>();
            foreach (var file in filesToCheck)
            {
                PrintMessage($"{file.GetDataFileName()} index files");
                try
                {
                    if (problemChecker.CheckIndexDatCounts(file))
                    {
                        _indexDatRepairList.Add(file);
                        PrintMessage("Needs repair", 3);
                    }
                    else
                        PrintMessage("No issues found", 1);

                    if (problemChecker.CheckForLargeDats(file))
                    {
                        _indexDatRepairList.Add(file);
                        PrintMessage("Extra dat files found, a reset is recommended", 3);
                    }
                    else
                        PrintMessage("No extra dat files", 1);
                }
                catch (Exception ex)
                {
                    PrintMessage($"There was an issue checking index dat counts\n{ex.Message}", 2);
                }
            }
            return _indexDatRepairList;
        }

        void CheckBackups(DirectoryInfo backupDirectory, ProblemChecker problemChecker)
        {
            var filesToCheck = new XivDataFile[] { XivDataFile._01_Bgcommon, XivDataFile._04_Chara, XivDataFile._06_Ui };
            foreach (var file in filesToCheck)
            {
                PrintMessage($"{file.GetDataFileName()} index files");
                try
                {
                    var backupFile = new DirectoryInfo(Path.Combine(backupDirectory.FullName, $"{file.GetDataFileName()}.win32.index"));
                    if (!File.Exists(backupFile.FullName))
                    {
                        PrintMessage("No backup found", 3);
                        continue;
                    }
                    if (!problemChecker.CheckForOutdatedBackups(file, backupDirectory))
                        PrintMessage("Out of date", 3);
                    else
                        PrintMessage("Up to date", 1);
                }
                catch (Exception ex)
                {
                    PrintMessage($"There was an issue checking the backed up index files\n{ex.Message}", 2);
                }
            }
        }

        void CheckDat()
        {
            var fileInfo = new FileInfo(Path.Combine(_indexDirectory.FullName, $"{XivDataFile._06_Ui.GetDataFileName()}.win32.dat1"));
            PrintMessage($"{XivDataFile._06_Ui.GetDataFileName()}.win32.dat1");
            if (fileInfo.Exists)
            {
                if (fileInfo.Length < 10000000)
                    PrintMessage("File is missing data", 3);
                else
                    PrintMessage("No issues found", 1);
            }
            else
                PrintMessage("File could not be found", 2);
        }

        void CheckMods()
        {
            var modlistPath = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "XivMods.json"));
            var modlistJson = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(modlistPath.FullName));
            var dat = new Dat(_indexDirectory);

            if (modlistJson.modCount > 0)
            {
                foreach (var mod in modlistJson.Mods)
                {
                    if (mod.name.Equals(string.Empty)) 
                        continue;
                    var fileName = Path.GetFileName(mod.fullPath);
                    PrintMessage(fileName);
                    if (mod.data.originalOffset == 0)
                        PrintMessage("Original offset was 0, you will be unable to revert to original, consider starting over", 3);
                    else if (mod.data.modOffset == 0)
                        PrintMessage("Mod offset was 0, disable it and reimport. (This option is not available yet, consider starting over)", 3);
                    else
                        PrintMessage("Correct offsets", 1);

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
                        PrintMessage($"Found unknown file type ({fileType}), offset is most likely corrupt", 3);
                    else
                        PrintMessage("Correct file type", 1);
                }
            }
            else
                PrintMessage("No entries found in the modlist", 3);
        }

        void CheckLoD(DirectoryInfo configDirectory)
        {
            var problem = false;
            var DX11 = false;
            string ffxivCfg = Path.Combine(configDirectory.FullName, "FFXIV.cfg");
            string ffxivbootCfg = Path.Combine(configDirectory.FullName, "FFXIV_BOOT.cfg");

            if (configDirectory.Exists)
            {
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
                    PrintMessage($"Could not find {ffxivbootCfg}", 3);

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
                                    PrintMessage($"{line.Substring(0, line.IndexOf("\t"))} ON\nCertain mods have issues with LoD turned on, turning it off...", 3);
                                    tmpLine = lineNum;
                                    problem = true;
                                    break;
                                }
                                PrintMessage($"{line.Substring(0, line.IndexOf("\t"))} OFF", 1);

                            }
                            else if (!DX11 && !line.Contains("DX11"))
                            {
                                if (val.Equals("1"))
                                {
                                    PrintMessage($"{line.Substring(0, line.IndexOf("\t"))} ON", 3);
                                    tmpLine = lineNum;
                                    problem = true;
                                    break;
                                }
                                PrintMessage($"{line.Substring(0, line.IndexOf("\t"))} OFF", 1);
                            }

                        }

                        lineNum++;
                    }

                    if (problem)
                    {
                        var line = lines[tmpLine];
                        line = line.Substring(0, line.Length - 1) + 0;
                        lines[tmpLine] = line;
                        File.WriteAllLines(ffxivCfg, lines);
                        PrintMessage("LoD turned off, rerunning check...");
                        CheckLoD(configDirectory);
                    }
                }
                else
                    PrintMessage($"Could not find {ffxivCfg}", 3);
            }
        }
        #endregion

        static void Main(string[] args)
        {
            MainClass instance = new MainClass();
            Parser.Default.ParseArguments<importoptions, exportoptions, backupoptions, resetoptions, problemoptions, versionoptions>(args)
            .WithParsed<importoptions>(opts => {
                instance._gameDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game"));
                instance._indexDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game", "sqpack", "ffxiv"));
                instance.ImportModpackHandler(new DirectoryInfo(opts.ttmpPath));
            })
            .WithParsed<versionoptions>(opts => {
                instance._gameDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game"));
                instance.CheckGameVersion();
            })
            .WithParsed<backupoptions>(opts => {
                instance._gameDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game"));
                instance._indexDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game", "sqpack", "ffxiv"));
                instance.BackupIndexes(new DirectoryInfo(opts.backupDirectory));
            })
            .WithParsed<resetoptions>(opts => {
                instance._gameDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game"));
                instance._indexDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game", "sqpack", "ffxiv"));
                instance.ResetMods(new DirectoryInfo(opts.backupDirectory));
            })
            .WithParsed<problemoptions>(opts => {
                instance._gameDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game"));
                instance._indexDirectory = new DirectoryInfo(Path.Combine(opts.gameDirectory, "game", "sqpack", "ffxiv"));
                instance.ProblemChecker(new DirectoryInfo(opts.configDirectory), new DirectoryInfo(opts.backupDirectory));
            })
                /*.WithParsed<exportoptions>(opts => ...)
                .WithNotParsed(errs => ...)*/;
        }
    }
}