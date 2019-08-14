using System;
using System.IO;

namespace FFXIV_Modding_Tool
{
    public class Validation
    {
        public Validation()
        {
        }

        public bool ValidGameDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists || directory.GetFiles("*.index").Length == 0)
                return false;
            return true;
        }

        public bool ValidBackupDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists)
                return false;
            return true;
        }

        public bool ValidGameConfigDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists || directory.GetFiles("FFXIV*.cfg").Length == 0)
                return false;
            return true;
        }
    }
}
