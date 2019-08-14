using System;
using System.IO;

namespace FFXIV_Modding_Tool.Validation
{
    public class Validators
    {
        public Validators()
        {
        }

        public bool ValidateDirectory(DirectoryInfo directory, string directoryType)
        {
            if (!directory.Exists)
                return false;
            else
            {
                switch (directoryType)
                {
                    case "BackupDirectory":
                        return true;
                    case "GameDirectory":
                        if (directory.GetFiles("*.index").Length == 0)
                            return false;
                        return true;
                    case "ConfigDirectory":
                        if (directory.GetFiles("FFXIV*.cfg").Length == 0)
                            return false;
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
