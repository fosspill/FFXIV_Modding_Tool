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
using xivModdingFramework.General.Enums;
using xivModdingFramework.Mods;
using xivModdingFramework.Mods.DataContainers;
using xivModdingFramework.Mods.Enums;
using xivModdingFramework.Mods.FileTypes;
using xivModdingFramework.SqPack.FileTypes;
using xivModdingFramework.Textures.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Items.Interfaces;
using CommandLine;

namespace FFXIV_TexTools_CLI
{
// Verbs does not support nesting :( 
    [Verb("modpackimport", HelpText = "Import a modpack")]
    public class importoptions
    {
        [Option('g', "gamedirectory", Required = true, HelpText = "Full path including \"Final Fantasy XIV - A Realm Reborn\"")]
        public string Directory { get; set; }
        [Option('m', "modpackdirectory", Required = true, HelpText = "Path to modpackdirectory")]
        public string ModPackDirectory { get; set; }
        [Option('t', "ttmp", Required = true, HelpText = "Path to .ttmp(2) file")]
        public string TTMP { get; set; }
    }
    [Verb("export", HelpText = "Export modpack / texture / model")]
    public class exportoptions
    { //normal options here
    }
    [Verb("reset", HelpText = "Reset game to clean state")]
    public class resetoptions
    { //normal options here
    }

    public class MainClass
    {
        public DirectoryInfo _gameDirectory;
        public DirectoryInfo _gameModDirectory;

        /* Print slightly nicer messages. Can add logging here as well if needed.
         1 = Success message, 2 = Error message 
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
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n");

        }

        void CheckGameVersion()
        {

            Version ffxivVersion = null;
            var versionFile = Path.Combine(_gameDirectory.ToString(), "game", "ffxivgame.ver");
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

        #region Importing Functions
        void ImportModpackHandler(DirectoryInfo ttmpPath, DirectoryInfo modpackDirectory)
        {
            var importError = false;
            var index = new Index(new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "game", "sqpack", "ffxiv")));

            bool indexLockStatus = index.IsIndexLocked(XivDataFile._0A_Exd);

            if (indexLockStatus)
            {
                PrintMessage("Unable to import while the game is running.", 2);
                return;
            }

            try
            {
                var ttmp = new TTMP(modpackDirectory, "TexTools");
                var ttmpData = ttmp.GetModPackJsonData(ttmpPath);
                try
                {
                    GetModpackData(ttmpPath, modpackDirectory, ttmpData.ModPackJson);
                }
                catch
                {
                    importError = true;
                }

            }
            catch (Exception ex)
            {
                if (!importError)
                    GetModpackData(ttmpPath, modpackDirectory, null);
                else
                {
                    PrintMessage($"There was an error importing the modpack at {ttmpPath.FullName}\nMessage: {ex.Message}", 2);
                    return;
                }
            }

            return;
        }

        void GetModpackData(DirectoryInfo ttmpPath, DirectoryInfo modpackDirectory, ModPackJson ttmpData)
        {
            var modding = new Modding(_gameDirectory);
            List<SimpleModPackEntries> ttmpDataList = new List<SimpleModPackEntries>();
            TTMP _textoolsModpack = new TTMP(ttmpPath, "TexTools");
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
                var originalModPackData = _textoolsModpack.GetOriginalModPackJsonData(modpackDirectory);

                foreach (var modsJson in originalModPackData)
                {
                    var race = GetRace(modsJson.FullPath);
                    var number = GetNumber(modsJson.FullPath);
                    var type = GetType(modsJson.FullPath);
                    var map = GetMap(modsJson.FullPath);

                    var active = false;
                    var isActive = modding.IsModEnabled(modsJson.FullPath, false);

                    if (isActive == XivModStatus.Enabled)
                    {
                        active = true;
                    }

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
                            ModPackEntry = new ModPack { name = Path.GetFileNameWithoutExtension(modpackDirectory.FullName), author = "N/A", version = "1.0.0" }
                        }
                    });
                }
            }
            ttmpDataList.Sort();
            ImportModpack(ttmpDataList, _textoolsModpack, modpackDirectory);
        }

        async void ImportModpack(List<SimpleModPackEntries> ttmpDataList, TTMP _textoolsModpack, DirectoryInfo modpackDirectory)
        {
            var importList = (from SimpleModPackEntries selectedItem in ttmpDataList select selectedItem.JsonEntry).ToList();
            var modListDirectory = new DirectoryInfo(Path.Combine(_gameDirectory.FullName, "game", "XivMods.json"));
            int totalModsImported = 0;
            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                var importResults = await _textoolsModpack.ImportModPackAsync(modpackDirectory, importList,
                    _gameDirectory, modListDirectory, progressIndicator);

                if (!string.IsNullOrEmpty(importResults.Errors))
                    PrintMessage($"There were errors importing some mods:\n{importResults.Errors}", 2);
            }
            catch (Exception ex)
            {
                PrintMessage($"There was an error attempting to import mods:\n{ex.Message}", 2);
            }
            PrintMessage($"{totalModsImported} mod(s) successfully imported.", 1);
        }

        void ReportProgress(double value)
        {
            PrintMessage($"{value}%");
        }

        XivRace GetRace(string modPath)
        {
            var xivRace = XivRace.All_Races;

            if (modPath.Contains("ui/") || modPath.Contains(".avfx"))
            {
                xivRace = XivRace.All_Races;
            }
            else if (modPath.Contains("monster"))
            {
                xivRace = XivRace.Monster;
            }
            else if (modPath.Contains("bgcommon"))
            {
                xivRace = XivRace.All_Races;
            }
            else if (modPath.Contains(".tex") || modPath.Contains(".mdl") || modPath.Contains(".atex"))
            {
                if (modPath.Contains("accessory") || modPath.Contains("weapon") || modPath.Contains("/common/"))
                {
                    xivRace = XivRace.All_Races;
                }
                else
                {
                    if (modPath.Contains("demihuman"))
                    {
                        xivRace = XivRace.DemiHuman;
                    }
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
                    {
                        number = "stigma";
                    }
                    else
                    {
                        number = "Error";
                    }
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
                {
                    type = slotAbr[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];
                }

                if (modPath.Contains("/face/"))
                {
                    if (modPath.Contains(".tex"))
                    {
                        type = FaceTypes[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];
                    }
                }

                if (modPath.Contains("/hair/"))
                {
                    if (modPath.Contains(".tex"))
                    {
                        type = HairTypes[modPath.Substring(modPath.LastIndexOf("_") - 3, 3)];
                    }
                }

                if (modPath.Contains("/vfx/"))
                {
                    type = "VFX";
                }

            }
            else if (modPath.Contains(".avfx"))
            {
                type = "AVFX";
            }

            return type;
        }

        string GetMap(string modPath)
        {
            var xivTexType = XivTexType.Other;

            if (modPath.Contains(".mdl"))
            {
                return "3D";
            }

            if (modPath.Contains(".mtrl"))
            {
                return "ColorSet";
            }

            if (modPath.Contains("ui/"))
            {
                var subString = modPath.Substring(modPath.IndexOf("/") + 1);
                return subString.Substring(0, subString.IndexOf("/"));
            }

            if (modPath.Contains("_s.tex") || modPath.Contains("skin_m"))
            {
                xivTexType = XivTexType.Specular;
            }
            else if (modPath.Contains("_d.tex"))
            {
                xivTexType = XivTexType.Diffuse;
            }
            else if (modPath.Contains("_n.tex"))
            {
                xivTexType = XivTexType.Normal;
            }
            else if (modPath.Contains("_m.tex"))
            {
                xivTexType = XivTexType.Multi;
            }
            else if (modPath.Contains(".atex"))
            {
                var atex = Path.GetFileNameWithoutExtension(modPath);
                return atex.Substring(0, 4);
            }
            else if (modPath.Contains("decal"))
            {
                xivTexType = XivTexType.Mask;
            }

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

        static void Main(string[] args)
        {
            MainClass instance = new MainClass();
//var options = new Options();
            Parser.Default.ParseArguments<importoptions, exportoptions, resetoptions>(args)
            .WithParsed<importoptions>(opts => { instance._gameDirectory = new DirectoryInfo(opts.Directory); })
                /*.WithParsed<exportoptions>(opts => ...)
                .WithParsed<resetoptions>(opts => ...)
                .WithNotParsed(errs => ...)*/;
            instance.CheckGameVersion();

        }
    }
}
