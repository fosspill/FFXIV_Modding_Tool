using System.Globalization;
using System.IO;
using Salaros.Configuration;

namespace FFXIV_Modding_Tool.Configuration
{
    public class Config
    {
        public DirectoryInfo _projectconfDirectory;

        public Config(DirectoryInfo projectconfDirectory)
        {
            _projectconfDirectory = projectconfDirectory;
        }

        public void ReadConfig()
        {
            MainClass main = new MainClass();
            if (!Directory.Exists(_projectconfDirectory.FullName))
                Directory.CreateDirectory(_projectconfDirectory.FullName);
            string configFile = Path.Combine(_projectconfDirectory.FullName, "config.cfg");
            var configFileFromPath = new ConfigParser(configFile);
            var configFileFromString = new ConfigParser(@"[Directories]
# All paths can be written with or without escaping

# Full path to game install, including 'FINAL FANTASY XIV - A Realm Reborn'
GameDirectory

# Full path to directory with your index backups, this can be any directory where you wish to store your backups
BackupDirectory

# Full path to directory where FFXIV.cfg and character data is saved, including 'FINAL FANTASY XIV - A Realm Reborn'
# This is usually located in 'My Documents/My Games', in your wine prefix if not on Windows
ConfigDirectory",
                new ConfigParserSettings
                {
                    MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys | MultiLineValues.QuoteDelimitedValues,
                    Culture = new CultureInfo("en-US")
                });
            if (!File.Exists(configFile) || string.IsNullOrEmpty(File.ReadAllText(configFile)))
                configFileFromString.Save(configFile);
            string gameDirectory = configFileFromPath.GetValue("Directories", "GameDirectory");
            string backupDirectory = configFileFromPath.GetValue("Directories", "BackupDirectory");
            string configDirectory = configFileFromPath.GetValue("Directories", "ConfigDirectory");
            if (!string.IsNullOrEmpty(gameDirectory))
            {
                MainClass._gameDirectory = new DirectoryInfo(Path.Combine(gameDirectory, "game"));
                MainClass._indexDirectory = new DirectoryInfo(Path.Combine(gameDirectory, "game", "sqpack", "ffxiv"));
            }
            else
                main.PrintMessage("No game install directory saved", 3);
            if (!string.IsNullOrEmpty(backupDirectory))
                MainClass._backupDirectory = new DirectoryInfo(backupDirectory);
            else
                main.PrintMessage("No index backup directory saved", 3);
            if (!string.IsNullOrEmpty(configDirectory))
                MainClass._configDirectory = new DirectoryInfo(configDirectory);
            else
                main.PrintMessage("No game config directory saved", 3);
        }
    }
}
