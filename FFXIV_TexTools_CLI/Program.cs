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
using xivModdingFramework.General.Enums;
using xivModdingFramework.Helpers;
using xivModdingFramework.Mods;
using xivModdingFramework.SqPack.FileTypes;
using CommandLine;

namespace FFXIV_TexTools_CLI
{
// Verbs does not support nesting :( 
    [Verb("import modpack", HelpText = "Import modpack / texture / model")]
    public class importoptions
    {
    }
    [Verb("export", HelpText = "Export modpack / texture / model")]
    public class exportoptions
    { //normal options here
    }
    [Verb("reset", HelpText = "Reset game to clean state")]
    public class resetoptions
    { //normal options here
    }
    //public class Options
    //{
    //    [Option('o', "operation", Required = true, HelpText = "Path to .ttmp(2) file")]
    //    public string Operation { get; set; }
    //    [Option('g', "gamedirectory", Required = true, HelpText = "Full path including \"Final Fantasy XIV - A Realm Reborn\"")]
    //    public string Directory { get; set; }
    //    [Option('m', "modpackdirectory", Required = true, HelpText = "Path to modpackdirectory")]
    //    public string ModPackDirectory { get; set; }
    //    [Option('t', "ttmp", Required = false, HelpText = "Path to .ttmp(2) file")]
    //    public string TTMP { get; set; }

    //}

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

        private void CheckGameVersion()
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


        static void Main(string[] args)
        {
            MainClass instance = new MainClass();
# var options = new Options();
            Parser.Default.ParseArguments<importoptions, exportoptions, resetoptions>(args)
                .WithParsed<importoptions>(opts => ...)
                .WithParsed<exportoptions>(opts => ...)
                .WithParsed<resetoptions>(opts => ...)
                .WithNotParsed(errs => ...)
}
            Parser.Default.ParseArguments<Options>(args)
                       .WithParsed<Options>(o =>
                       {
                           instance._gameDirectory = new DirectoryInfo(o.Directory);
                       });
            instance.CheckGameVersion();

        }


    }
}
