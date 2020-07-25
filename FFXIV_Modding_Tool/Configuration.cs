using System.Globalization;
using System.IO;
using Salaros.Configuration;

namespace FFXIV_Modding_Tool.Configuration
{
    public class Config
    {
        public static string configFile = Path.Combine(MainClass._projectconfDirectory.FullName, "config.cfg");
        MainClass main = new MainClass();

        public void CreateDefaultConfig()
        {
            if (!Directory.Exists(MainClass._projectconfDirectory.FullName))
                Directory.CreateDirectory(MainClass._projectconfDirectory.FullName);
            var configFileFromString = new ConfigParser(@"[Directories]
# All paths can be written with or without escaping

# Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
# Example locations:
#   MacOS: /Users/<USER_NAME>/Library/Application Support/FINAL FANTASY XIV ONLINE/Bottles/published_Final_Fantasy/drive_c/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn
#   Linux: /path/to/WINEBOTTLE/drive_c/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn
#   Windows: C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn
GameDirectory

# Full path to directory with your index backups, this can be any directory where you wish to store your backups
BackupDirectory

# Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
# Example locations:
#   MacOS: /Users/<USER_NAME>/My Documents/My Games/FINAL FANTASY XIV - A Realm Reborn
#   Linux: /path/to/WINEBOTTLE/drive_c/users/<USER_NAME>/My Documents/My Games/FINAL FANTASY XIV - A Realm Reborn
#   Windows: C:\users\<USER_NAME>\My Documents\My Games\FINAL FANTASY XIV - A Realm Reborn
ConfigDirectory",
                new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.QuoteDelimitedValues,
                    Culture = new CultureInfo("en-US")
                });
            configFileFromString.Save(configFile);
            main.PrintMessage($"Config file saved to {configFile}", 1);
        }

        public string ReadConfig(string target)
        {
            var configFileFromPath = new ConfigParser(configFile);
            string targetDirectory = configFileFromPath.GetValue("Directories", target);
            if (!string.IsNullOrEmpty(targetDirectory))
                //Workaround for issue #166
                //If Directory is quoted on both ends: ignore both quotes.
                List<char> quotelist = new List<char>();
                quotelist.AddRange("\'\"");
                if(quotelist.Contains(targetDirectory[0]) && quotelist.Contains(targetDirectory[targetDirectory.Length -1])){
                    targetDirectory = targetDirectory.Substring(1, targetDirectory.Length -2);
                }
                return targetDirectory;
            else
                return "";
        }
    }
}
