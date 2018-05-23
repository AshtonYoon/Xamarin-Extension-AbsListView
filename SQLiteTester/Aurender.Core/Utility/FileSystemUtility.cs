using System;

namespace Aurender.Core.Utility
{
    public static class FileSystemUtility
    {
        public static Func<String> GetDataFolderPath { get; set; }
        public static Func<String, String> PathConverter { get; set; }
    }
}
