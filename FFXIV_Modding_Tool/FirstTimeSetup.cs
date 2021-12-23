using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using xivModdingFramework.Mods;
using System.Collections.Generic;
using FFXIV_Modding_Tool.Configuration;
using FFXIV_Modding_Tool.Validation;

namespace FFXIV_Modding_Tool.FirstTimeSetup
{
    public class SetupCommand
    {
        public SetupCommand(){}
        Validators validation = new Validators();
        MainClass main = new MainClass();
        Config config = new Config();
        
        static string _home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        //Lists of common install and profile locations to assist with the first-time setup.
        //Linux
        static List<string> _InstallLocations_Linux = new List<string>()  {
                        Path.Combine(_home, "Games", "final-fantasy-xiv-a-realm-reborn", "drive_c", "Program Files (x86)", "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn"),
                        Path.Combine(_home, "Games", "final-fantasy-xiv-online", "drive_c", "Program Files (x86)", "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn"),
                        Path.Combine(_home, ".steam", "steam", "steamapps", "common", "Final Fantasy XIV Online")};
        static List<string> _UserDataLocations_Linux = new List<string>()  {
                        Path.Combine(_home, "Games", "final-fantasy-xiv-a-realm-reborn", "drive_c", "users", $"{Environment.UserName}", "My Documents", "My Games", "FINAL FANTASY XIV - A Realm Reborn"),
                        Path.Combine(_home, "Games", "final-fantasy-xiv-online", "drive_c", "users", $"{Environment.UserName}", "My Documents", "My Games", "FINAL FANTASY XIV - A Realm Reborn"),
                        Path.Combine(_home, ".steam", "steam", "steamapps", "compatdata", "39210", "pfx", "drive_c", "users", "steamuser", "My Documents", "My Games", "FINAL FANTASY XIV - A Realm Reborn")};
        //Mac
        static List<string> _InstallLocations_Mac = new List<string>()  {
            Path.Combine(_home, "Library", "Application Support", "FINAL FANTASY XIV ONLINE", "Bottles", "published_Final_Fantasy", "drive_c", "Program Files (x86)", "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn")
        };
        static List<string> _UserDataLocations_Mac = new List<string>()  {
            Path.Combine(_home, "My Documents", "My Games", "FINAL FANTASY XIV - A Realm Reborn")
        };

        //Windows
        static List<string> _InstallLocations_Windows = new List<string>()  {
            Path.Combine("C:\\", "Program Files (x86)", "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn")
        };
        static List<string> _UserDataLocations_Windows = new List<string>()  {
            Path.Combine(_home, "My Documents", "My Games", "FINAL FANTASY XIV - A Realm Reborn")
        };
        
        //Combining all lists to make itteration easy
        Dictionary<string, List<string>> _InstallLocations = new Dictionary<string, List<string>>() {["Linux"] = _InstallLocations_Linux, ["Mac"] = _InstallLocations_Mac, ["Windows"] = _InstallLocations_Windows};
        Dictionary<string, List<string>> _UserDataLocations = new Dictionary<string, List<string>>(){["Linux"] = _UserDataLocations_Linux, ["Mac"] = _UserDataLocations_Mac, ["Windows"] = _UserDataLocations_Windows};
        
        //Lists to store Valid locations
        List<string> _ValidInstallLocations = new List<string>() {};
        List<string> _ValidUserDataLocations = new List<string>() {};
        
        private string _OperatingSystemAsString(){
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
                return "Linux";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                return "Mac";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){
                return "Windows";
            } else {
                return "";
            }
        }
        
        private bool _ValidDirectory(string path, string type){
            if (!string.IsNullOrEmpty(path)){
                return validation.ValidateDirectory(new DirectoryInfo(path), type);
            } else {
                return false;
            }
        }
        
        private string AskForInstallationDirectory(){
        main.PrintMessage("----------\nFirst we'll try to define your Game Directory!", 1);
            main.PrintMessage(@"    Example locations:
        MacOS: /Users/<USER_NAME>/Library/Application Support/FINAL FANTASY XIV ONLINE/Bottles/published_Final_Fantasy/drive_c/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn
        Linux: /path/to/WINEBOTTLE/drive_c/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn
        Windows: C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn
        ");

            if (_ValidInstallLocations.Count > 0){
                main.PrintMessage($"Found {_ValidInstallLocations.Count} possible Game folder(s):");
                foreach (string path in _ValidInstallLocations){
                    main.PrintMessage($"{path}");
                }
            } else {main.PrintMessage("Found no valid game installs in common locations, you must define the Game Directory path on your own."); }
            string _GameDirectoryFromConsole = "";
            while (!_ValidDirectory(Path.Combine(_GameDirectoryFromConsole, "game", "sqpack", "ffxiv"), "GameDirectory")){
                Console.Write("\nEnter your Game Directory: ");
                _GameDirectoryFromConsole=@"" + Console.ReadLine().Replace("\"", "").Replace("~", _home);
                if(!_ValidDirectory(Path.Combine(_GameDirectoryFromConsole, "game", "sqpack", "ffxiv"), "GameDirectory")){
                    main.PrintMessage("Invalid directory, please confirm that it matches the examples provided.", 3);
                }
            }
            return _GameDirectoryFromConsole;
        }
        
        private string AskForConfigurationDirectory(){
        main.PrintMessage("----------\nNow we'll have to find your Configuration directory!", 1);
            main.PrintMessage(@"    Example locations:
        MacOS: /Users/<USER_NAME>/My Documents/My Games/FINAL FANTASY XIV - A Realm Reborn
        Linux: /path/to/WINEBOTTLE/drive_c/users/<USER_NAME>/My Documents/My Games/FINAL FANTASY XIV - A Realm Reborn
        Windows: C:\users\<USER_NAME>\My Documents\My Games\FINAL FANTASY XIV - A Realm Reborn
        ");      
            
            if (_ValidUserDataLocations.Count > 0){
                main.PrintMessage($"Found {_ValidUserDataLocations.Count} possible Configuration folder(s):");
                foreach (string path in _ValidUserDataLocations){
                    main.PrintMessage($"{path}");
                }
            } else { main.PrintMessage("Found no valid Configuration folders in common locations, you must define the Configuration Directory path on your own."); }
            string _ConfigDirectoryFromConsole = "";
            while (!_ValidDirectory(_ConfigDirectoryFromConsole, "ConfigDirectory")){
                Console.Write("\nEnter your Config Directory: ");
                _ConfigDirectoryFromConsole=@"" + Console.ReadLine().Replace("\"", "").Replace("~", _home);
                if(!_ValidDirectory(_ConfigDirectoryFromConsole, "ConfigDirectory")){
                    main.PrintMessage("Invalid directory, please confirm that it matches the examples provided.", 3);
                }
            }
            return _ConfigDirectoryFromConsole;
        }
        
        private string AskForBackupDirectory(){
            main.PrintMessage("----------\nTime to set up your index backup directory.", 1);
            main.PrintMessage(@"    Example locations:
        MacOS: /Users/<USER_NAME>/My Documents/FFXIV Index Backups
        Linux: /home/<USER_NAME>/FFXIV Index Backups
        Windows: C:\users\<USER_NAME>\My Documents\FFXIV Index Backups
        
        This folder can be anywhere but must already exist.
        ");      
            
            string _BackupDirectoryFromConsole = "";
            while (!_ValidDirectory(_BackupDirectoryFromConsole, "BackupDirectory")){
                Console.Write("\nEnter your desired Backup Directory: ");
                _BackupDirectoryFromConsole=@"" + Console.ReadLine().Replace("\"", "").Replace("~", _home);
                if(!_ValidDirectory(_BackupDirectoryFromConsole, "BackupDirectory")){
                    main.PrintMessage("Invalid directory. Make sure it exists and is accessable.", 3);
                }
            }
            return _BackupDirectoryFromConsole;
        }
    
        public void ExecuteSetup(){
            main.PrintMessage($"Starting configuration wizard for first-time setup...\nThis will overwrite the configuration file at {Config.configFile}\nYou'll be guided, step-by-step, to ensure that your configuration file is valid.\n\nWe'll start off by scanning common installation directories.", 1);
            if(!validation.PromptContinuation("Ready?", true)){ return; }
            if (!string.IsNullOrEmpty(_OperatingSystemAsString())){
                foreach (string path in _InstallLocations[_OperatingSystemAsString()]){
                    if (_ValidDirectory(Path.Combine(path, "game", "sqpack", "ffxiv"), "GameDirectory")){
                        _ValidInstallLocations.Add(path);
                    }
                }
                foreach (string path in _UserDataLocations[_OperatingSystemAsString()]){
                    if (_ValidDirectory(Path.Combine(path), "ConfigDirectory")){
                        _ValidUserDataLocations.Add(path);
                    }
                }
            }
            string _GameDirectoryFromConsole = AskForInstallationDirectory();
            string _ConfigDirectoryFromConsole = AskForConfigurationDirectory();
            string _BackupDirectoryFromConsole = AskForBackupDirectory();
            
            main.PrintMessage("----------\nFinal confirmation.", 1);
            main.PrintMessage($"Game Directory = {_GameDirectoryFromConsole}", 1);
            main.PrintMessage($"Config Directory = {_ConfigDirectoryFromConsole}", 1);
            main.PrintMessage($"Backup Directory = {_BackupDirectoryFromConsole}", 1);
            if(!validation.PromptContinuation("\nDoes the configuration look correct?", false)){
                main.PrintMessage("Cancelled.", 3);
                return;
            } else {
                config.SaveConfig("GameDirectory", _GameDirectoryFromConsole);
                config.SaveConfig("ConfigDirectory", _ConfigDirectoryFromConsole);
                config.SaveConfig("BackupDirectory", _BackupDirectoryFromConsole);
                main.PrintMessage($"Configuration saved to {Config.configFile}", 1);
                }
            }
    }
}
