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
using System.Reflection;
using System.Runtime.InteropServices;
using xivModdingFramework.General.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Textures.Enums;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Commandline;
using FFXIV_Modding_Tool.ModControl;
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

        /* Print slightly nicer messages. Can add logging here as well if needed.
         1 = Success message, 2 = Error message, 3 = Warning message, 4 = Bad message 
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
                    Environment.Exit(1);
                    break;
                case 3:
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    goto default;
                case 4:
                    Console.ForegroundColor = ConsoleColor.Red;
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

        public bool IndexLocked()
        {
            var index = new xivModdingFramework.SqPack.FileTypes.Index(_indexDirectory);
            bool indexLocked = index.IsIndexLocked(XivDataFile._0A_Exd);
            return indexLocked;
        }

        public void ReportProgress((int current, int total, string message) report)
        {
            var progress = ((double)report.current / (double)report.total) * 100;
            Console.Write($"\r{(int)progress}%");
        }

        public XivRace GetRace(string modPath)
        {
            var xivRace = XivRace.All_Races;
            List<string> modPathList = modPath.Split('/').ToList();

            if (modPath.StartsWith("ui/") || modPath.EndsWith(".avfx"))
                xivRace = XivRace.All_Races;
            else if (modPathList.Contains("monster"))
                xivRace = XivRace.Monster;
            else if (modPathList.Contains("bgcommon"))
                xivRace = XivRace.All_Races;
            else if (modPath.EndsWith(".tex") || modPath.EndsWith(".mdl") || modPath.EndsWith(".atex"))
            {
                if (modPathList.Contains("accessory") || modPathList.Contains("weapon") || modPathList.Contains("common"))
                    xivRace = XivRace.All_Races;
                else
                {
                    if (modPathList.Contains("demihuman"))
                        xivRace = XivRace.DemiHuman;
                    else if (modPathList.Last().StartsWith("v"))
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

        public string GetNumber(string modPath)
        {
            var number = "-";
            List<string> modPathList = modPath.Split('/').ToList();

            if (modPathList.Contains("human") && modPathList.Contains("body"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("/b") + 2, 4);
                number = int.Parse(subString).ToString();
            }

            if (modPathList.Contains("face"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("/f") + 2, 4);
                number = int.Parse(subString).ToString();
            }

            if (modPathList.Contains("decal_face"))
            {
                var length = modPath.LastIndexOf(".") - (modPath.LastIndexOf("_") + 1);
                var subString = modPath.Substring(modPath.LastIndexOf("_") + 1, length);

                number = int.Parse(subString).ToString();
            }

            if (modPathList.Contains("decal_equip"))
            {
                var subString = modPath.Substring(modPath.LastIndexOf("_") + 1, 3);

                try
                {
                    number = int.Parse(subString).ToString();
                }
                catch
                {
                    if (modPath.EndsWith("_stigma.tex"))
                        number = "stigma";
                    else
                        number = "Error";
                }
            }

            if (modPathList.Contains("hair"))
            {
                var t = modPath.Substring(modPath.LastIndexOf("/h") + 2, 4);
                number = int.Parse(t).ToString();
            }

            if (modPathList.Contains("tail"))
            {
                var t = modPath.Substring(modPath.LastIndexOf("l/t") + 3, 4);
                number = int.Parse(t).ToString();
            }

            return number;
        }

        public string GetType(string modPath)
        {
            var type = "-";
            List<string> modPathList = modPath.Split('/').ToList();

            if (modPath.EndsWith(".tex") || modPath.EndsWith(".mdl") || modPath.EndsWith(".atex"))
            {
                if (modPathList.Contains("demihuman"))
                    type = slotAbr[modPath.Substring(modPath.LastIndexOf("/") + 16, 3)];

                if (modPathList.Contains("face"))
                {
                    if (modPath.EndsWith(".tex"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(modPath);
                        try
                        {
                            type = FaceTypes[fileName.Substring(fileName.IndexOf("_") + 1, 3)];
                        } 
                        catch
                        {
                            type = "Unknown";
                        }
                    }
                }

                if (modPathList.Contains("hair"))
                {
                    if (modPath.EndsWith(".tex"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(modPath);
                        try 
                        {
                            type = HairTypes[fileName.Substring(fileName.IndexOf("_") + 1, 3)];
                        } 
                        catch
                        {
                            type = "Unknown";
                        }
                    }
                }

                if (modPathList.Contains("vfx"))
                    type = "VFX";
            }
            else if (modPath.EndsWith(".avfx"))
                type = "AVFX";
            
            return type;
        }

        public string GetPart(string modPath)
        {
            string part = "-";
            string[] parts = new[] { "a", "b", "c", "d", "e", "f" };
            List<string> modPathList = modPath.Split('/').ToList();

            if (modPathList.Contains("equipment"))
            {
                if(modPathList.Contains("texture"))
                {
                    part = modPath.Substring(modPath.LastIndexOf("_") - 1, 1);
                    foreach(string letter in parts)
                        if (part == letter) return part;
                    return "a";
                }

                if(modPathList.Contains("material"))
                    return modPath.Substring(modPath.LastIndexOf("_") + 1, 1);
            }
            return part;
        }

        public string GetMap(string modPath)
        {
            var xivTexType = XivTexType.Other;

            if (modPath.EndsWith(".mdl"))
                return "3D";

            if (modPath.EndsWith(".mtrl"))
                return "ColorSet";

            if (modPath.StartsWith("ui/"))
            {
                var subString = modPath.Substring(modPath.IndexOf("/") + 1);
                return subString.Substring(0, subString.IndexOf("/"));
            }

            if (modPath.EndsWith("_s.tex") || modPath.Equals("chara/common/texture/skin_m.tex"))
                xivTexType = XivTexType.Specular;
            else if (modPath.EndsWith("_d.tex"))
                xivTexType = XivTexType.Diffuse;
            else if (modPath.EndsWith("_n.tex"))
                xivTexType = XivTexType.Normal;
            else if (modPath.EndsWith("_m.tex"))
                xivTexType = XivTexType.Multi;
            else if (modPath.EndsWith(".atex"))
            {
                var atex = Path.GetFileNameWithoutExtension(modPath);
                return atex.Substring(0, 4);
            }
            else if (modPath.StartsWith("chara/common/texture/decal_") && modPath.EndsWith(".tex"))
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

        #region Index File Handling
        public Dictionary<string, XivDataFile> IndexFiles()
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
            if (File.Exists(Mods._modlistPath.FullName))
            {
                var modData = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(Mods._modlistPath.FullName));
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
            var modlistJson = JsonConvert.DeserializeObject<ModList>(File.ReadAllText(Mods._modlistPath.FullName));
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
            Arguments arguments = new Arguments();
            arguments.ArgumentHandler(args);
        }
    }
}
